# ACMu アーキテクチャ決定書

> 本書は契約(ACMu.Core / ACMu.PluginApi)と対で凍結する。変更には設計セッションでの裁定が必要。
> Sonnet への実装タスクには本書 + 契約ファイル + csharp_mono4_constraints.md + unity54_constraints.md を必ず添付する。

## 1. モジュール分割図

```
他Mod(プラグイン) → ACMu.PluginApi → ACMu.Core
                          ↑
ACMu.Host ──→ ACMu.Compat → ACMu.PluginApi
ACMu.Host ──→ ACMu.Weapons → ACMu.PluginApi, ACMu.Core
ACMu.Host ──→ ACMu.Net     → ACMu.Core
ACMu.Host ──→ ACMu.World   → ACMu.Core
ACMu.Host ──→ ACMu.Adapter → ACMu.Core
ACMu.Adapter → [Besiege本体クラス(BlockBehaviour/StatMaster/NetworkCompression等)]
ACMu.Weapons / ACMu.PluginApi → [Modding.Modules (BCM)]
ACMu.Host → [Modding.ModEntryPoint]
```

### 依存規律(違反 = レビュー即差し戻し)

1. `ACMu.Core` はどこにも依存しない(UnityEngine を除く)
2. 本体デコンパイル領域(`BlockBehaviour`, `StatMaster`, `NetworkCompression`, `Machine` 等)への参照は `ACMu.Adapter` のみ
3. 実装層(Adapter / Net / World / Weapons / Compat)同士の横参照は禁止。連携は Core 契約経由のみ
4. 配線(実装の生成と IAcmuServices への束ね)は `ACMu.Host` のみが知る
5. 公式 Modding API(`Modding.*`)は安定とみなし、Host / Weapons / PluginApi から参照可

### 各モジュールの責務

| モジュール | 責務 | 対応する契約 |
|---|---|---|
| ACMu.Core | 契約・純粋型のみ。実装コードを置かない | 全契約の定義元 |
| ACMu.PluginApi | 他Modへの公開面 | IAcmuPluginHost, IWeaponRegistry |
| ACMu.Adapter | 本体クラス依存の隔離実装 | ILog, IGameSessionInfo, IGameEventSource, IBlockAccessor, IConfigStore |
| ACMu.Net | 通信境界の実装(ModNetworking 多重化) | INetworkTransport, IPacketWriter/Reader |
| ACMu.World | 拡張EX(座標変換・同期) | IWorldFrame |
| ACMu.Weapons | 武装パイプライン・弾体管理・武装ホストBehaviour | IProjectileService, IWeaponHost, IWeaponRegistry実装 |
| ACMu.Compat | 旧ACM識別子の互換モジュール群(PluginApi の利用者として実装) | — |
| ACMu.Host | ModEntryPoint, ACMUcore GameObject, ライフサイクル進行, 配線 | ILifecycleParticipant の駆動側 |

## 2. 決定表

| 論点 | 決定 | 根拠(1行) | 却下した代替案(1行) | 仮定 |
|---|---|---|---|---|
| 介入様式 | コンポーネント合成+データ補正のみ | 司令塔§2の在庫に準拠 | パッチ/差し替え前提の設計 | — |
| モジュール分割 | Core/PluginApi/Adapter/Net/World/Weapons/Compat/Host の8層 | 変更理由の分離=エージェントの並行作業単位 | 機能別フラット分割(横断変更が頻発) | 物理は当面1DLL、namespaceで規律維持 |
| 本体依存の隔離 | デコンパイル由来クラス参照はAdapterのみ | 本体更新の破壊半径を1層に限定 | 各所からの直接参照 | 公式Modding APIは安定とみなし各層から可 |
| プラグイン発見 | IWeaponRegistryへの明示登録(他ModがACMu.dll参照) | Reflection禁止環境で走査不可能 | 属性スキャン/Assembly走査 | 他ModはMods.IsModLoadedでガード |
| 公開面の取得 | "ACMUcore" GameObject上のIAcmuPluginHostコンポーネント | Find+GetComponentで無Reflection到達 | staticシングルトン | Findは接続時1回のみ |
| 武装実装方式 | 合成: Behaviourは薄いホスト、ロジックはWeaponComponentBase | BCMの単一継承制約(UC-1の難所)の解消+単体テスト可 | BlockModuleBehaviour多段継承 | BCM基底はModding.Modules.BlockModule |
| 武装拡張点 | 5段フック(Validate/BeforeFire/AfterFire/Impact/Explosion)+可変Context | UC-1の遅延・追加生成・誘導差替・ダメージ補正を全て被覆 | 汎用イベントバス | フック内例外は捕捉し当該武装のみ無効化 |
| 弾体管理 | 中央IProjectileService+ホスト発番ProjectileHandle | 不具合#1(スポーン同期)の正を単一管理点に一本化 | ブロック個別のプール(現行と同じ破綻) | クライアントSpawnはInvalid返却 |
| 誘導 | IGuidanceStrategy差し替え、計算はホスト側のみ | ドローンdrop後も遊動拡張点を温存 | 弾クラス継承による分岐 | 出力はMaxAccelerationでクランプ |
| 座標権威 | ホスト権威 | 既存アーキ踏襲、状態分岐の防止 | クライアント権威(自弾のみ) | 補間・再送は通信設計セッションで決定 |
| ワイヤ座標表現 | Vector3d(double)を直に直列化、シーンは当面絶対float | 拡張EXの本質。本体の16bit圧縮はボーダーレスで破綻 | 本体NetworkCompression流用 | 原点シフトは契約のみ予約、初期実装は恒等 |
| 同期粒度 | イベント(Spawn/Despawn)=Reliable、状態=Unreliable周期(sendRate準拠) | 不具合#1はイベント欠落起因の疑いが濃い | 全状態毎フレームReliable(帯域破綻) | 周期値は後でチューニング |
| メッセージ多重化 | ModNetworkingメッセージ型は最少数、byteチャネルIDで多重化 | 255型上限と定義順互換問題の回避 | 機能ごとに型追加(現行方式) | ペイロード上限8KB |
| **通信基盤の外部公開** | **正式API化: IAcmuPluginHost.Services.Network を安定APIティアに含める** | ACM以外に拡張通信を持つModが無く、基盤としての価値が高い | publicフィールドを晒すだけ(互換保証なしで利用側が壊れる) | チャネル32〜254はAllocateChannelで払い出し、ownerName必須で衝突を診断可能にする |
| API安定性ティア | 安定=PluginApi全部+Core.Net/Weapons/World/Maths、内部=Adapter/Host/Compat | 他Modが依存してよい面を明文化 | 全部公開・全部保証 | ApiVersionはメジャーのみ(破壊的変更時に+1) |
| 非同期 | コルーチンのみ。Threadは純計算に限定 | .NET3.5制約(Task/await不在) | Task風の自作 | — |
| 旧ACM互換 | Compat層が旧識別子(ドローン除く)をPluginApi上に再実装 | 互換境界C-01/C-02準拠+自APIのドッグフーディング | コア直実装(プラグインAPIが検証されない) | drop判定機能は実装しない |
| 設定 | IConfigStore、joystickConfig形式維持 | 互換境界§3 | 新形式へ強制移行 | — |
| エラー方針 | 契約境界でtry-catch、機能単位フェイルソフト | Mod全体クラッシュ防止 | fail-fast | — |
| エージェント運用 | Sonnet主体(実装+セルフレビュー)、Opusはマイルストーン境界のみ、実機検証は人間 | コスト制約(Fable/Opusほぼ不可) | Fable常駐レビュー | CLAUDE.mdのチェックリストでセルフレビューを機械化 |

## 3. 司令塔§7への回答

1. **カスタムモジュール実装方式**: BCMの`AddBlockModule`登録は維持しつつ、TBehaviourはACMuが提供する汎用ホスト(WeaponComponentBaseを生成・委譲する薄いBlockModuleBehaviour)に固定する。プラグインはXMLモジュール定義+WeaponComponentBase継承+Register呼び出しの3点のみ書く。旧版互換モジュールはCompat層がこのAPI上で再実装し、新版モジュールも同一APIで実装する。

2. **実装方針と進め方**: 縦切り優先。ROADMAP.md の M0→M6 の順で、各マイルストーンごとに「ゲーム内で動く」状態を保つ。契約は凍結扱いとし、変更が必要になったら実装を止めて設計判断を仰ぐ。

3. **AIエージェント割当**: 実装=Sonnet(Claude Code、CLAUDE.md常駐)。レビュー=Sonnetセルフレビュー(CLAUDE.md内チェックリスト)+マイルストーン境界でのみOpusに差分レビューを依頼。実機テスト=人間(各マイルストーンの受け入れ基準を手動確認)。

4. **通信処理**: 確定済み=境界(INetworkTransport)・ホスト権威・チャネル多重・Reliable/Unreliableの使い分け・外部Modへの正式公開。未確定=再送・補間・スナップショット形式(M4の前に通信設計セッションを1回行う。Sonnetに委ねる場合は「決定表の制約内で2案の比較表(帯域・実装量・失敗モード)を作成→人間が裁定」の手順)。

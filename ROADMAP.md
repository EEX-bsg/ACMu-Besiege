# ROADMAP — 実装順序と Sonnet 用タスク票

> 原則: 各マイルストーン終了時点で「ゲーム内で何かが動く」。縦切り優先、横並べ禁止。
> 各票はそのまま Claude Code への最初の指示として貼れる粒度にしてある。
> 着手順は M0 → M1 → M2 → M3 → M4 → M5 → M6。M2 完了時点で初の目に見える成果が出る。

---

## M0: ビルド基盤(最初の1日)

**目的**: 「ビルドしてBesiegeにロードされる空Mod」を作る。以後の全作業の足場。

**タスク**:
1. `ACMu.csproj` を作成: `TargetFramework=net35`, `LangVersion=6`,
   参照 = `UnityEngine.dll` / `Assembly-CSharp.dll`(Modding含む) / BCMのdll。
   参照パスは `$(BesiegeDir)` プロパティで外出しし、`Directory.Build.props.user`(gitignore対象)で各自指定
2. net35 ビルドのため NuGet `Microsoft.NETFramework.ReferenceAssemblies.net35` を PrivateAssets で追加
3. `build.sh`: `dotnet build -c Release` → 成果物dllとmod.jsonを `$BESIEGE_DIR/Besiege_Data/Mods/ACMu/` へコピー
4. `mod.json` の雛形作成(**形式はユーザーが既存Modから提供する。届くまで TODO コメントで仮置き**)
5. `src/ACMu.Host/AcmuMod.cs`: `Modding.ModEntryPoint` 継承、`OnLoad()` で `ModConsole` に1行出すだけ
6. `.gitignore`(bin/obj/Directory.Build.props.user)

**受け入れ基準**:
- [ ] `./build.sh` が警告ゼロで完走(機械確認)
- [ ] Besiege起動時にコンソールへ "ACMu loaded" が出る(実機・人間)

**触ってよい範囲**: ルート直下 + `src/ACMu.Host/` のみ。

---

## M1: Host骨格 + Adapter(動く土台)

**目的**: ACMUcore GameObject とサービス配線を作り、契約の最初の実装(Adapter)を通す。

**タスク**:
1. `ACMu.Host/AcmuCoreBootstrap.cs`: OnLoad で `new GameObject(WellKnownNames.CoreObjectName)`,
   `DontDestroyOnLoad`, 各サービスコンポーネントを AddComponent して配線
2. `ACMu.Host/LifecycleCoordinator.cs`: `ILifecycleParticipant` を InitOrder 順に保持し、
   `OnModLoad` を即時、`SimulationToggled` を受けて Start/Stop を呼ぶ。各呼び出しは try-catch + ILog
3. `ACMu.Adapter/ConsoleLog.cs`: ILog 実装(ModConsole委譲、Errorはスタックトレース付き)
4. `ACMu.Adapter/GameSessionInfoAdapter.cs`: IGameSessionInfo 実装(StatMaster / NetworkScene.ServerSettings の射影)
5. `ACMu.Adapter/GameEventSourceAdapter.cs`: IGameEventSource 実装(Modding.Events の再公開。ハンドラ毎 try-catch)
6. `ACMu.Adapter/BlockAccessorAdapter.cs`: IBlockAccessor 実装(BlockBehaviour ラップ。Mapper系は KeyList から解決)
7. `ACMu.Adapter/ModIoConfigStore.cs`: IConfigStore 実装(Modding.ModIO の XML 読み書き、失敗時 new T())
8. `ACMu.Host/AcmuServicesComponent.cs`: IAcmuServices + IAcmuPluginHost 実装(ApiVersion=1)。
   未実装サービス(Network/World/Projectiles)は M3/M2 まで Null実装(何もしないがnullを返さない)を置く

**受け入れ基準**:
- [ ] シミュ開始/終了でログに Start/Stop が1回ずつ出る(実機)
- [ ] MPロビーでホスト/クライアント判定ログが正しい(実機)
- [ ] サービスのどれかが例外を投げてもMod全体は生きる(機械: テスト用に故意に投げて確認後、削除)

**触ってよい範囲**: `src/ACMu.Host/`, `src/ACMu.Adapter/`

---

## M2: 武装の縦切り1本(シングルプレイで撃てる) ★最初の見せ場

**目的**: プラグインAPI経由のテスト砲が、シングルプレイで弾を撃ち、当たって消える。

**タスク**:
1. `ACMu.Weapons/ProjectileService.cs`: IProjectileService 実装。
   ローカル限定(MP同期はM4)。プール(Queue<GameObject>)、ハンドル発番(int連番)、
   寿命管理コルーチン、着弾検知(弾に付ける `ProjectileBody : MonoBehaviour` の OnCollisionEnter)
2. `ACMu.Weapons/WeaponHostBehaviour.cs`:
   `class WeaponHostBehaviour<TModule> : Modding.Modules.BlockModuleBehaviour<TModule>, IWeaponHost`。
   薄いホスト: 武装ファクトリの解決(typeof(TModule)キーの static Dictionary)、WeaponComponentBase 生成・AttachTo、
   ライフサイクル転送(OnSimulateStart→NotifySimulationStart 等)、発射キー監視→発射パイプライン実行
3. `ACMu.Weapons/FirePipeline.cs`: RequestFire → クールダウン判定 → FireContext生成(Shot=BaseSpec.Clone()) →
   NotifyValidateFire → NotifyBeforeFire → (DelaySeconds>0ならコルーチン遅延) → Projectiles.Spawn → NotifyAfterFire。
   着弾時: ImpactContext生成 → NotifyImpact → NotifyExplosion → 既定爆発(抑止可) → Despawn
4. `ACMu.Weapons/WeaponRegistryImpl.cs`: IWeaponRegistry 実装。
   Register<TModule> で `CustomModules.AddBlockModule<TModule, WeaponHostBehaviour<TModule>>(name, inMP)` +
   ファクトリ辞書登録。二重登録は例外
5. `ACMu.Compat/TestCannon/`: テスト武装(新識別子 "AcmuTestCannon"、互換対象外)。
   XMLモジュール(発射キー+弾速スライダー) + `TestCannonWeapon : WeaponComponentBase`
   (OnBeforeFireで弾速をSpecから反映するだけの最小実装)。弾プレハブは原始Sphere+Rigidbodyを実行時生成
6. ブロックXML(Blocks/ フォルダ)の雛形作成(**メッシュ等は仮。Cube流用**)

**受け入れ基準**:
- [ ] テスト砲ブロックを設置→シミュ開始→キーで球が飛ぶ(実機)
- [ ] 着弾/寿命で弾が消え、プールに戻る(連射してもGameObject数が増え続けない)(実機)
- [ ] スライダーで弾速が変わる(実機)
- [ ] 他Mod視点のサンプル: TestCannon の登録コードが PluginApi だけで書けている(機械: using検査)

**触ってよい範囲**: `src/ACMu.Weapons/`, `src/ACMu.Compat/TestCannon/`, Host の配線1行

---

## M3: 通信境界(INetworkTransport 実装)

**目的**: チャネル多重トランスポートを動かす。**他Modにも公開する基盤**なので堅牢第一。

**タスク**:
1. `ACMu.Net/PacketWriterImpl.cs` / `PacketReaderImpl.cs`: MemoryStream + BinaryWriter/BinaryReader(許可型)。
   Vector3d=double×3、Quaternion=float×4(圧縮は通信設計セッション後)。文字列はUTF8+長さ前置
2. `ACMu.Net/ModNetTransport.cs`: INetworkTransport 実装。
   ModNetworking メッセージ型は **2つだけ** 登録(Reliable用/Unreliable用…ModNetworkingに不達設定が無ければ1つ)。
   ペイロード = [channelId(1B)][本体]。受信で channelId を剥がし Subscribe 済みハンドラへ配送(ハンドラ毎try-catch)。
   AllocateChannel は 32 から連番払い出し+ownerName記録。IsReady=ModNetworking.IsNetworkingReady
3. 8KB超過Sendの例外、IsReady=false時の破棄+ログ、不正channelId受信の破棄+ログ
4. `ACMu.Host` の Null実装を本物に差し替え
5. 自己診断: ACMu内部チャネル0で接続時にバージョンhelloを交換し、相手のApiVersion不一致を警告ログ

**受け入れ基準**:
- [ ] 2クライアント(ホスト+1)でhello交換ログが双方に出る(実機)
- [ ] エコーテストチャネル(デバッグ用、後で削除)で文字列が往復する(実機)
- [ ] Writer→Readerの全型ラウンドトリップ単体テスト(機械: ゲーム外で実行可能なテストとして書く)

**触ってよい範囲**: `src/ACMu.Net/`, Host の配線

**注意**: 再送・補間・スナップショット形式はここでは作らない(通信設計セッション待ち)。

---

## M4: 武装のMP化(スポーン同期の本丸)

**前提**: 通信設計セッション(人間+AI)を1回挟み、スナップショット形式を決めてから着手。

**目的**: 既知不具合#1(スポーン/デスポーン同期の弱さ)を新設計で潰す。

**タスク概要**(詳細は設計セッション後に確定):
1. ホスト: Spawn/Despawn を Reliable で全員へ、座標スナップショットを sendRate 周期 Unreliable で送信
2. クライアント: 受信でプロキシ弾(物理なし、見た目のみ)を生成・移動。Spawned/Despawnedイベントを同一ハンドル列で発火
3. 整合性掃除: 周期的な生存ハンドル一覧(Reliable)で、クライアント側の孤児弾を NetworkCorrection で消す
4. 発射要求: クライアントの発射キーはホストへ要求送信、ホストが権威実行

**受け入れ基準**:
- [ ] ラグ模擬環境(可能なら)で連射しても、弾がクライアントに残留しない(実機)
- [ ] ホストとクライアントで同時刻の弾数が一致に収束する(実機+ログ比較)

---

## M5: Compat 第1弾(旧識別子の武装互換)

**目的**: 旧 "AdShootingProp" 相当を PluginApi 上に再実装し、既存セーブのマシンが動く。

**タスク概要**: compatibility-boundary.md C-01/C-02 の識別子・XMLキーを忠実に再現。
ドローン機能は実装しない(司令塔§3でdrop)。XMLキー一覧は detailed-design.md §10 を正とする。

**受け入れ基準**: 既存ACMで作られた武装付きマシンのセーブが読み込め、発射できる(実機)。

---

## M6: World 拡張EX

**目的**: Vector3d ワイヤ座標 + IWorldFrame 実装で 4km 境界を越える。

**タスク概要**: 本体の境界制限の無効化箇所の特定(Adapter経由のデータ書き換えで)、
ブロック座標同期の double 化、(必要なら)原点シフトの実装解禁。
着手前にユーザーから既存ACMの「黒魔術」の仕組み聞き取りを行うこと(司令塔§5が未記入のため)。

**受け入れ基準**: MPで4km境界を越えて相互に座標が正しく見える(実機)。

---

## マイルストーン横断の運用

- 各M完了時: 人間が受け入れ基準の実機項目を確認 → 余裕があればOpusに差分レビュー1回 → 次へ
- M内で契約の不備が見つかったら: 実装を止めて報告(CLAUDE.md ルール1)
- M2 と M3 は依存が薄いので、並行セッションで走らせてもよい

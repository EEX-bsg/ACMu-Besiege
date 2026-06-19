# CLAUDE.md — ACMu 開発ルール(Claude Code 常駐コンテキスト)

ACMu は Besiege の Mod「ACM」の権利クリーンな非公式フォーク。武装プラグインAPIと
ボーダーレスワールド(拡張EX)がコア価値。あなた(Sonnet)は実装担当。設計判断はしない。

**応答言語**: ユーザーへの返答は常に日本語で行う。コード・コミットメッセージ・識別子は対象外。
作業前に必ずリモートリポジトリと差分を無くす事！！fetch&merge!!

## 0. 絶対ルール(違反したらその変更は破棄)

1. **契約凍結(条件付き解除あり)**: `src/acmu/ACMu.Core/` と `src/acmu/ACMu.PluginApi/` は原則読み取り専用。
   ただし「凍結を守るために設計が明らかに複雑・無意味になる」場合は Core/PluginApi 自体を変える方が正。
   その場合は**勝手に変えず**、オーナーに次を説明して**同意を得られた時のみ**変更する:
   (a) 何を どのように 変えるか (b) その影響範囲 (c) 既存の利用箇所・契約不変条件に他の問題が出ないか。
   同意なしの Core/PluginApi 変更は禁止。判断に迷う/同意前の段階では作業を止めて上記を提示する。
2. **依存規律**(ARCHITECTURE.md §1): 本体デコンパイル由来クラス
   (`BlockBehaviour` `SaveableDataHolder` `StatMaster` `Machine` `NetworkCompression` `MapperType` 等)を
   `using`/参照してよいのは `src/acmu/ACMu.Adapter/` のみ。実装層同士の横参照禁止(Core契約経由のみ)。
3. **言語制約**: C# 6 まで・.NET 3.5 ランタイム。詳細は `docs/csharp_mono4_constraints.md` を毎タスク冒頭で読む。
   特に: `async/await` `Task` `ValueTuple` `switch式` `ローカル関数` `?\?=` `using宣言` は存在しないものとして書く。
4. **禁止API**: `System.Reflection` 全部 / `System.IO`(Stream系・Path等の許可型を除く) /
   `System.Xml` / `System.Net` / Harmony的発想全部。ファイルI/Oは `Modding.ModIO` のみ。
5. **Unity 5.4**: `docs/unity54_constraints.md` 参照。コルーチンが唯一の非同期手段。
   `Thread` は Unity API を一切呼ばない純計算のみ可。
6. **旧ACMのコードを見ない・写さない**: クリーンルーム方針。参照してよいのは
   docs/ 配下の解析ドキュメントと、本リポジトリのコードのみ。
7. オリジナルの `Ad〜` という命名を新規コードに使わない(Compat の互換識別子文字列を除く)。
   クラス名・フィールド名・メソッド名はすべて ACMu 固有の名前を付ける。
   旧ACM互換ブロック(XMLタグ `AdShootingProp` など)に強く紐づくクラスは `Old〜` または `Legacy〜` で表現する。
   例: `AdShootingWeapon` → `OldCannonWeapon`、`AdShootingHostBehaviour` → `OldCannonHostBehaviour`

## 1. リポジトリ構成

```
src/acmu/                プロジェクトルート(acmu.csproj はここ)
  Mod.cs                 Modロード起動点(Besiege が OnLoad() を呼ぶ)
  ACMu.Core/             契約のみ(凍結)
  ACMu.PluginApi/        公開面(凍結)
  ACMu.Adapter/          本体依存の隔離実装
  ACMu.Net/              INetworkTransport 実装
  ACMu.World/            IWorldFrame 実装・拡張EX
  ACMu.Weapons/          武装パイプライン・IProjectileService・武装ホスト
  ACMu.Compat/           旧ACM互換モジュール(PluginApi の利用者として書く)
  ACMu.Host/             ACMUcore・配線
src/ACMu.sln             Visual Studio ソリューション
docs/                    制約・設計ドキュメント(読み取り専用)
ACMu/                    ビルド成果物置き場(Mod.xml 等のリソース)
build.sh                 ビルド+Modsフォルダ配置(GitBash用)
```

## 2. コーディング規約

- 1クラス1ファイル。ファイル名=型名。namespace はフォルダと一致(`ACMu.Weapons` 等)
- Godクラス禁止: 1クラスの責務は1行で言えること。public メソッド10超 or 400行超で分割を検討
- 契約のXMLコメントに書かれた不変条件・呼び出しタイミング・スレッド制約は実装側の義務。実装前に必ず読む
- 契約境界(フック呼び出し・イベント発火・受信ハンドラ)は try-catch で囲み、
  `ILog.Error` に記録して続行(フェイルソフト)。Mod全体を巻き込んで死なない
- `Update`/`FixedUpdate` 内でのアロケーション(new、LINQ、文字列結合、クロージャ)を避ける。
  ループ外でキャッシュ・プール・再利用。これは「慢性的なラグ改善」という非機能要件そのもの
- イベント購読したら対応する解除箇所を必ず書く(シミュ停止・破棄時のリーク防止)
- ログは入れすぎない。初期化・エラー・ネットワーク異常のみ。毎フレームログ禁止。ただし削除する前提のデバックログは毎フレームでなければ許可、作業が一段落したら削除。
- **WHYコメント方針(思考履歴)**: タスク実施中、**判断点があった箇所**にコメントを残す。
  閾値は「非自明かどうか」ではなく「選択肢があったかどうか」。
  以下のカテゴリが対象:
  1. 複数の実装方法があってこちらを選んだ理由
  2. 制約(互換・Besiege/Unity5.4/Mono/.NET3.5・ACMu構造)のせいで自然な書き方を諦めた箇所
  3. 将来「なぜこうなってる?」と疑問を持たれそうな非線形な構造
  4. 試して上手くいかなかった代替案・過去の失敗履歴
  コードが「何をするか(WHAT)」はコメントしない。「なぜそうなっているか(WHY)」のみ。
  単純な getter・自明な forwarding は対象外。事後の一括追加はしない。

## 3. ビルドと検証

- ビルド: `./build.sh`(中身: `dotnet msbuild src/acmu/acmu.csproj /p:Configuration=Release` → PostBuildEvent で `ACMu/acmu.dll` へコピー → `$BESIEGE_DIR/Besiege_Data/Mods/ACMu/` へコピー)
- `BESIEGE_DIR` 環境変数が無ければコピーをスキップしビルドのみ
- **タスク完了の定義**: (1) ビルドが警告ゼロで通る (2) 変更ファイルが依存規律に違反していない
  (3) 下記セルフレビューを通過 (4) 受け入れ基準のうちゲーム外で確認できる項目を満たす
- ゲーム内動作確認は人間が行う。「実機確認が必要な項目」をタスク末尾に箇条書きで残すこと

## 4. セルフレビュー チェックリスト(毎タスク、コミット前に全項目を明示的に確認)

- [ ] C#7以降の構文を使っていない(タプル/ローカル関数/switch式/`?\?=`/throw式/型パターンswitch)
- [ ] `Task` / `async` / `await` / `System.Reflection` / 禁止 `System.IO` 型が無い
- [ ] Adapter 以外で本体デコンパイル由来クラスを参照していない
- [ ] 実装層から別の実装層の具象型を参照していない
- [ ] 契約のXMLコメント(不変条件)に反していない(該当契約を再読して確認)
- [ ] Update/FixedUpdate 内の毎フレームアロケーションが無い
- [ ] 購読/生成したものに対応する解除/破棄がある
- [ ] 新規 public API を勝手に増やしていない(契約に無い public は実装層内では internal 推奨)

## 5. ワークフロー

1. タスクは ROADMAP.md のマイルストーン単位で受ける。1セッション=1タスクが原則
2. 着手前に: 該当する契約ファイル・docs の制約2本・ARCHITECTURE.md の関連行を読む
3. 実装 → ビルド → セルフレビュー → 変更概要+実機確認項目を報告
4. 設計疑問・契約の不足・仕様の曖昧さに当たったら、**勝手に決めずに**選択肢を2つ提示して停止
5. コミットメッセージ: `M<番号>: <変更内容1行>`(日本語可)

## 6. よく使う対応表(本体API → ACMu契約)

| やりたいこと | 使うもの |
|---|---|
| ログ | `IAcmuServices.Log`(実装: Adapter の ModConsole ラッパー) |
| MP セッション権威判定(ACMu設定の送受信など) | `IGameSessionInfo.IsHost / IsClient`(StatMaster を直接見ない) |
| **自マシンのシミュ実行権威(武装・物理・ローカル弾体)** | **`IGameSessionInfo.IsSimulating`** — MP クライアントのローカルシミュでも true になる。`IsHost` で分岐すると ローカルシミュ中クライアントの武装が動かない(→ `docs/mp-local-simulation.md`) |
| シミュ開始/終了に反応 | `IGameEventSource.SimulationToggled`(Events を直接購読しない) |
| ブロックの値読み取り | `IBlockAccessor`(BlockBehaviour を直接触らない) |
| 送受信 | `INetworkTransport`(ModNetworking を直接呼ばない) |
| 座標の同期表現 | `Vector3d` + `IWorldFrame`(float の生 Vector3 をワイヤに乗せない) |
| 設定保存 | `IConfigStore`(ModIO を直接呼ばない) |

例外: これらの契約を「実装する」モジュール(Adapter/Net/World)の内部だけは下位APIを直接呼ぶ。

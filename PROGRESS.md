# ACMu 実装進捗 — セッション引き継ぎメモ

最終更新: 2026-06-14 (M1.5 完了・M2 未着手)

---

## 完了済みマイルストーン

### M0 ✅ (タグ: M0)
- `Directory.Build.props` + `Directory.Build.props.user` (gitignore)
- `.gitignore` (bin/, obj/, docs/, ACMu/*.dll 等)
- `build.sh`
- `ACMu/Mod.xml` (Besiegeマニフェスト — `mod.json`ではなくXML形式)

ビルド: 警告0エラー0確認済み

**注**: M0当初に作成したSDK-styleの `ACMu.csproj` (ルート) と `src/ACMu.Host/AcmuMod.cs` は
誤った設計であった。真のプロジェクトファイルは `src/acmu/acmu.csproj` (旧VS形式)。
エントリーポイントは `src/acmu/Mod.cs` (`Mod : ModEntryPoint`)。

### M1 ✅ (タグ: M1)

**Adapter層** (`src/ACMu.Adapter/`):
| ファイル | 実装内容 | 特記事項 |
|---|---|---|
| `ConsoleLog.cs` | ILog + ILifecycleParticipant | ModConsole委譲、try-catchで飲み込み |
| `GameSessionInfoAdapter.cs` | IGameSessionInfo + ILifecycleParticipant | StatMaster static参照 |
| `GameEventSourceAdapter.cs` | IGameEventSource + ILifecycleParticipant | Events.* 購読、GetInvocationList per-try-catch |
| `BlockAccessorAdapter.cs` | IBlockAccessor | BlockBehaviourラップ、IEnumerable foreach |
| `ModIoConfigStore.cs` | IConfigStore + ILifecycleParticipant | Modding.ModIO (完全修飾必須) + BinaryWriter/JsonUtility |

**Host層** (`src/ACMu.Host/`):
| ファイル | 実装内容 |
|---|---|
| `AcmuCoreBootstrap.cs` | 静的Initialize(): ACMUcore GameObject生成+配線 |
| `LifecycleCoordinator.cs` | ILifecycleParticipant InitOrder順管理、SimulationToggled購読 |
| `AcmuServicesComponent.cs` | IAcmuServices + IAcmuPluginHost (ApiVersion=1) |
| `Null/NullNetworkTransport.cs` | INetworkTransport Null実装 (M3で差替え) |
| `Null/NullWorldFrame.cs` | IWorldFrame Null実装 (M6で差替え) |
| `Null/NullProjectileService.cs` | IProjectileService Null実装 (M2で差替え) |
| `Null/NullWeaponRegistry.cs` | IWeaponRegistry Null実装 (M2で差替え) |

### M1.5 ✅ — Mod 起動確立・M2 Core seam 追加 (タグなし / commit f7aadbb)

M0 が残した設計ミスの修正 + M2 のための Core 拡張。git tag は打っていないがビルド確認済み。

**Mod 起動修正**:
| 変更 | 内容 |
|---|---|
| `src/acmu/Mod.cs` | `OnLoad()` で `AcmuCoreBootstrap.Initialize()` を呼ぶ(元は空) |
| `src/acmu/ACMu.Host/AcmuMod.cs` | 削除。M0 が作った重複エントリーポイント。`Mod.cs` だけで十分 |
| `src/acmu/acmu.csproj` | `<PlatformTarget>x86</PlatformTarget>` 追加(DynamicText.dll arch 警告を除去) |
| `build.sh` | `dotnet msbuild` → VS MSBuild(vswhere 自動検出)に変更。dotnet は Unity Full v3.5 プロファイル非対応 |

**M2 Core seam 追加** (凍結対象だが調査ドキュメント M2_INVESTIGATION.md §3 でユーザー承認済み):
| ファイル | 変更 |
|---|---|
| `ACMu.Core/Game/IBlockAccessorFactory.cs` | 新規。`FromGameObject(GameObject)` → `IBlockAccessor` のファクトリ契約 |
| `ACMu.Core/IAcmuServices.cs` | `IBlockAccessorFactory Blocks { get; }` プロパティを追加 |
| `ACMu.Adapter/BlockAccessorFactoryAdapter.cs` | 新規。`Block.From(go).InternalObject` → `BlockAccessorAdapter` を `Dictionary` キャッシュつきで返す |
| `ACMu.Host/AcmuServicesComponent.cs` | `_blocks` フィールド + `Blocks` プロパティ追加、`Initialize()` 引数に追加 |
| `ACMu.Host/AcmuCoreBootstrap.cs` | `BlockAccessorFactoryAdapter` を AddComponent して配線 |

**seam の存在理由(次セッションの AI が読む想定)**:
Weapons 層の `WeaponHostBehaviour` は `IWeaponHost.Block`(= `IBlockAccessor`)を組み立てる必要がある。
しかし `BlockBehaviour`(decompile 型)は Adapter 外で触れてはならない(依存規律 rule 2)。
この seam により Weapons は `services.Blocks.FromGameObject(this.gameObject)` を呼ぶだけで済む。
decompile 型の取り扱いは Adapter の `BlockAccessorFactoryAdapter` が完全に隠蔽する。

---

## 既知の落とし穴・注意点

### ModIO名前空間の衝突
`ModIO` という独立したnamespaceが存在するため、`using Modding;` 下で `ModIO.ExistsFile()` を書くと
CS0234エラーになる。**完全修飾 `Modding.ModIO.ExistsFile()` を使うこと。**

### BlockBehaviour.Sliders / Toggles / Keys の型
実際の戻り値は `IEnumerable<T>` であり `List<T>` ではない。インデックスアクセス不可。foreach必須。

### ビルドツール: VS MSBuildを使うこと
`dotnet msbuild` は `TargetFrameworkProfile=Unity Full v3.5` (非標準プロファイル) を解決できず
**MSB3644 エラーで失敗する**。代わりに Visual Studio の MSBuild を使う:
```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" src/acmu/acmu.csproj /p:Configuration=Release /nologo /v:minimal
```
`build.sh` は vswhere 経由で自動検出する。PowerShell での手動ビルドは上記コマンド。

### git コミット (PowerShell)
here-string構文 `@'...'@` を使う (bash heredocは使えない):
```powershell
git -c user.name="EEX-bsg" -c user.email="exendra314@gmail.com" commit -m @'
コミットメッセージ
'@
```

### Mod.cs がエントリーポイント / AcmuMod.cs は不要
`src/acmu/Mod.cs` が唯一のエントリーポイント。Besiege はアセンブリ内の全 `ModEntryPoint` 実装を呼ぶ。
`AcmuMod.cs`（二重エントリーポイント）は削除済み。`Mod.cs` が `AcmuCoreBootstrap.Initialize()` を呼ぶ。

---

## 次のタスク: M2 (未着手 / 着手前調査は完了)

> **必読**: M2 着手前の Besiege 実API整合性調査を `M2_INVESTIGATION.md` に完了済み。
> Core 改訂は1点のみ(`IBlockAccessorFactory` 追加 + `IAcmuServices.Blocks` プロパティ)。
> PluginApi 変更不要。武装登録設計(`CustomModules.AddBlockModule` / `BlockModuleBehaviour<T>`)は
> 実在し実装可能(前回「契約変更必要」報告は誤りだった。リフレクションが UnityEngine 依存型を脱落させていた)。
> DLL 構造調査は **Mono.Cecil** を使うこと(通常リフレクションは MonoBehaviour 派生型を取りこぼす)。
> 実装手順・decompile型回避パターン・ライフサイクル対応表は `M2_INVESTIGATION.md` §3〜§5 に記載。

### M2: 武装の縦切り1本

**目的**: TestCannon でシングルプレイ発射→着弾→弾消滅の1サイクル完走。

**実装ファイル** (`src/ACMu.Weapons/`):
1. `ProjectileService.cs` — ローカルプール実装 (Queue<GameObject>), ハンドル連番, コルーチン寿命, ProjectileBody
2. `WeaponHostBehaviour.cs` — `BlockModuleBehaviour<TModule>` 継承, IWeaponHost, WeaponComponentBase 管理
3. `FirePipeline.cs` — RequestFire → クールダウン → NotifyBeforeFire → Spawn → コルーチン遅延 → NotifyAfterFire
4. `WeaponRegistryImpl.cs` — CustomModules.AddBlockModule<TModule, WeaponHostBehaviour<TModule>> 呼び出し

**実装ファイル** (`src/ACMu.Compat/TestCannon/`):
5. `TestCannonModule.cs` — BlockModule([XmlRoot="AcmuTestCannon"]), スライダー+キー定義
6. `TestCannonWeapon.cs` — WeaponComponentBase継承, OnBeforeFireで弾速反映

**Host配線変更** (`src/ACMu.Host/`):
- `AcmuCoreBootstrap.cs`: `ProjectileService` と `WeaponRegistryImpl` を AddComponent、`NullProjectileService` / `NullWeaponRegistry` を差替え ← **これだけ残り。Blocks 配線は M1.5 で完了済み**
- `AcmuServicesComponent.cs`: 変更不要(M1.5 で `Blocks` 追加済み。`_projectiles` / `_weapons` は Bootstrap 経由で渡す)

**M2着手前に読むべき契約**:
- `src/acmu/ACMu.Core/Weapons/WeaponComponentBase.cs`
- `src/acmu/ACMu.Core/Weapons/IWeaponHost.cs`
- `src/acmu/ACMu.Core/Weapons/ProjectileHandle.cs`
- `src/acmu/ACMu.Core/Weapons/ProjectileSpawnRequest.cs`
- `src/acmu/ACMu.Core/Weapons/FireContext.cs`
- `src/acmu/ACMu.Core/Weapons/WeaponSpec.cs`
- `src/acmu/ACMu.PluginApi/WeaponRegistration.cs`

**M2注意点**:
- `WeaponHostBehaviour<TModule>` は Generic MonoBehaviour で Unity 5.4 / .NET 3.5 では
  `AddComponent<WeaponHostBehaviour<TModule>>()` は使えない可能性がある。
  `CustomModules.AddBlockModule` の内部で型引数が確定するため、ここでジェネリクスが解決される。
- コルーチン: `yield return new WaitForSeconds(delay)` のみ可。IEnumerator戻り値のメソッドで定義。
- ProjectileBody (OnCollisionEnter) はコンポーネントとして弾に attach。MonoBehaviour要。

---

## アーキテクチャ上の判断メモ

| 判断 | 理由 |
|---|---|
| AcmuCoreBootstrap (Host) → ConsoleLog等 (Adapter) を直接参照 | Host は Composition Root ("配線")。Contract経由では AddComponent できない |
| Null実装でexplicit event accessor `{ add{} remove{} }` を使用 | CS0067警告回避 + 意味的正確性(購読しても呼ばれない) |
| NetworkSendRate = 20f (固定値) | NetworkScene.ServerSettings の型が不明確。安全なデフォルト値を返す |
| LocalPlayerId = 0 (固定値) | reflection禁止環境でローカルネットIDの取得APIが特定できなかった |

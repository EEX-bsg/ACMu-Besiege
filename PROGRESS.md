# ACMu 実装進捗 — セッション引き継ぎメモ

最終更新: 2026-06-14 (M2 完了)

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

### M2 ✅ — 武装の縦切り1本 (警告0エラー0確認済み)

**ACMu.Weapons 層** (新規):
| ファイル | 実装内容 |
|---|---|
| `ProjectileBody.cs` | MonoBehaviour。OnCollisionEnterでProjectileService.HandleImpactへ通知。_hit フラグで二重着弾防止 |
| `ProjectileService.cs` | IProjectileService + MonoBehaviour + ILifecycleParticipant(InitOrder=300)。Queue<GameObject>プール、_epoch でライフタイムコルーチン無効化、StopAllCoroutinesでシミュ停止時クリーン |
| `WeaponHostRegistry.cs` | internal static Dictionary<Type, WeaponRegistration>。WeaponRegistryImpl と WeaponHostBehaviour を繋ぐ内部ハブ |
| `WeaponHostBehaviour.cs` | BlockModuleBehaviour<TModule> : IWeaponHost。SafeAwake でseam解決、OnSimulateStart でアクセサ確定+武装生成、SimulateUpdateAlways で権威チェック後 NotifyUpdate |
| `FirePipeline.cs` | internal sealed。Time.time ベースのクールダウン、DelayedFire コルーチン、ImpactOccurred/Despawned を内部で購読・Dispose でunsubscribe |
| `WeaponRegistryImpl.cs` | IWeaponRegistry。AddBlockModule<TModule, WeaponHostBehaviour<TModule>> + WeaponHostRegistry 登録。二重登録は例外 |

**ACMu.Compat/TestCannon 層** (新規):
| ファイル | 実装内容 |
|---|---|
| `TestCannonModule.cs` | [Serializable] BlockModule。FireKey / SpeedSlider の MKeyReference / MSliderReference を定義 |
| `TestCannonWeapon.cs` | WeaponComponentBase。OnAttached でBaseSpec設定、OnUpdate でIsKeyHeldチェック、OnBeforeFire でスライダー値をShotに反映 |

**Host 配線変更**:
| ファイル | 変更 |
|---|---|
| `AcmuCoreBootstrap.cs` | ProjectileService(AddComponent+InitializeService) + WeaponRegistryImpl(new) を追加。RegisterTestCannon でSphere prefab作成+登録 |
| `AcmuServicesComponent.Initialize()` | シグネチャに IProjectileService/IWeaponRegistry を追加。Null実装を置換 |
| `acmu.csproj` | ACMu.Weapons / ACMu.Compat の Compile Include を追加 |

**データ**:
| ファイル | 内容 |
|---|---|
| `ACMu/Blocks/TestCannon.xml` | ブロック定義。ModuleMapperTypes (Key/Slider) + Modules (AcmuTestCannon) + Collider + BasePoint |
| `ACMu/Mod.xml` | `<Block path="Blocks/TestCannon.xml" />` 追加 |

**設計上の判断**:
- `WeaponHostBehaviour<TModule>` は Generic MonoBehaviour。Unity 5.4 で動作するのは型が具体確定済みの場合のみ(BCMがAddBlockModule内部で解決)
- 発射キー監視は `TestCannonWeapon.OnUpdate` に配置。Generic Host は TModule の形状を知らないため、Key名定数を同層のConst経由で渡す
- `ProjectileService.ImpactOccurred` は `internal` event。FirePipeline(同層)のみ購読できる。IProjectileService 公開面は汚染しない
- Sphere prefab は `CreatePrimitive` でランタイム生成。メッシュアセット不要。SphereCollider が衝突検知も担う

---

## 次のタスク: M3 (未着手)

> M2 完了。次は INetworkTransport 実装(M3)。M2 の実機確認項目を先に人間が検証すること。

---

## アーキテクチャ上の判断メモ

| 判断 | 理由 |
|---|---|
| AcmuCoreBootstrap (Host) → ConsoleLog等 (Adapter) を直接参照 | Host は Composition Root ("配線")。Contract経由では AddComponent できない |
| Null実装でexplicit event accessor `{ add{} remove{} }` を使用 | CS0067警告回避 + 意味的正確性(購読しても呼ばれない) |
| NetworkSendRate = 20f (固定値) | NetworkScene.ServerSettings の型が不明確。安全なデフォルト値を返す |
| LocalPlayerId = 0 (固定値) | reflection禁止環境でローカルネットIDの取得APIが特定できなかった |

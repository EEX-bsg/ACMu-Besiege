# ACMu 実装進捗 — セッション引き継ぎメモ

最終更新: 2026-06-17 (M5 実機確認 途中 / 二重爆発バグ修正)

---

## 完了済みマイルストーン

### M0 ✅
- `Directory.Build.props`, `.gitignore`, `build.sh`, `ACMu/Mod.xml`

### M1 ✅
Adapter / Host 基盤 (`ILog` `IGameSessionInfo` `IGameEventSource` `IBlockAccessor` `IConfigStore` + `AcmuCoreBootstrap` `LifecycleCoordinator` `AcmuServicesComponent`)

### M2 ✅
武装縦切り1本。`ProjectileService`, `WeaponHostBehaviour`, `FirePipeline`, `TestCannon` ブロック動作確認済み。

### M3 ✅
`INetworkTransport` 実装 (`ModNetTransport` / `PacketWriter` / `PacketReader`)。

### M4 ✅ (実機確認済み 2026-06-15)
弾体 MP 同期。`ProxyProjectile`, `ProjectileSyncTransport`, クライアント発射要求フロー。

---

### M5 ✅ — AdShootingProp 互換実装 (警告0エラー0確認済み)

`ACMu.Compat/Shooting/` を全面実装。旧ACM (`Ad〜` 命名) は `OldCannon〜` 命名で再実装(クリーンルーム)。

#### 追加ファイル一覧

| ファイル | 内容 |
|---|---|
| `XmlVector3.cs` | `[XmlAttribute x/y/z]` 補助型 |
| `XmlTransform.cs` | Position/Rotation/Scale XML型。`ToPosition()` / `ToRotation()` 変換付き |
| `MeshTransformRef.cs` | `<Mesh name="…"><Position/><Rotation/><Scale/>` XML型 |
| `AssetBundleNameRef.cs` | `<Foo name="…" />` 汎用 name 属性型 |
| `ShootingState.cs` | `<ShootingState>` サブ要素: Mass/Drag/AngularDrag/IgnoreGravity/EntityDamage/BlockDamage/CollisionTypeS/BounceCombineType/BounceStr/FrictionCombineType/FrictionStr/Mesh/Texture/Colliders |
| `ColliderDefs.cs` | `ColliderDefBase` + `CapsuleColliderDef` / `BoxColliderDef` / `SphereColliderDef` + `ProjectileColliderList` (弾頭コライダー) |
| `OldCannonModule.cs` | `[XmlRoot("AdShootingProp")]` BlockModule。全フィールド定義 |
| `OldCannonWeapon.cs` | WeaponComponentBase。以下の機能を実装: |
| | • hold-to-shoot / power / rate-of-fire スライダー |
| | • バーストショット (useBurstShot / RateOfBurst / BurstShotNum) |
| | • 弾薬管理 (DefaultAmmo / OnValidateFire) |
| | • ランダム拡散 (RandomDiffusion、Seed 由来決定論乱数) |
| | • スポーン遅延 (useDelay / DelayTime) |
| | • ランダムインターバルジッター (RandomInterval) |
| | • リコイル (RecoilMultiplier) |
| | • 発射フラッシュエフェクト (ShotFlashPosition 対応) |
| | • 発射音 / 着弾音 (Sounds / HitSounds) |
| | • 爆発処理 (OnExplosion) |
| `OldCannonHostBehaviour.cs` | WeaponHostBehaviour<OldCannonModule>。MuzzlePosition/Rotation、FlashMuzzlePosition/Rotation。AttachProjectileEffects: ProjectilePhysicsSetup / ProjectileMeshRestorer / ProjectileFuseTimer / トレイル/弾体エフェクト。OnFuseExplosion: 爆発力+エフェクト+Despawn |
| `ProjectilePhysicsSetup.cs` | MonoBehaviour (ACMu.Compat.Shooting)。ShootingState → Rigidbody / カスタムコライダー / PhysicMaterial に反映。OnEnable/OnDisable でプール復元 |
| `ProjectileMeshRestorer.cs` | MonoBehaviour (ACMu.Weapons)。カスタムメッシュ/マテリアルをプール安全に適用。transform オフセット時は子 GO を使用 |
| `ProjectileFuseTimer.cs` | MonoBehaviour (ACMu.Compat.Shooting)。タイムフューズ: `Activate(handle, fuseTime, callback)` → 経過後 callback を1回呼ぶ。OnDisable でキャンセル |
| `DamageRegistry.cs` | static delegate ブリッジ。`DamageApplierAdapter.ApplyDamage` を Bootstrap から注入 |
| `EffectRegistry.cs` | static delegate ブリッジ。Spawn/Return/Fade/LoadMesh/LoadMaterial/PlaySounds を Bootstrap から注入 |

#### 変更ファイル

| ファイル | 変更 |
|---|---|
| `ACMu.Adapter/EffectBundleAdapter.cs` | エフェクトプール管理 + メッシュ/テクスチャ/サウンドのアセット読み込み。ModResource 優先→AssetBundle フォールバック |
| `ACMu.Adapter/DamageApplierAdapter.cs` | ダメージ適用ブリッジ |
| `ACMu.Host/AcmuCoreBootstrap.cs` | EffectBundleAdapter 追加、EffectRegistry.SetFunctions/SetSoundFn、DamageRegistry.SetApplyFn 配線 |
| `ACMu/Blocks/ECannon.xml` | AdShootingProp ブロック定義。全新機能を記述。アセット未登録のためエフェクト/メッシュは空欄 |

#### 設計判断

- **ProjectilePhysicsSetup を ACMu.Compat に置く**: `ColliderDefBase` が Compat 層のため。ACMu.Compat→ACMu.Weapons は許容。逆方向は禁止
- **タイムフューズ爆発**: `FirePipeline.OnDespawned` は `NotifyImpact/Explosion` を呼ばない(Timeout のみトリガー)。フューズは `OnFuseExplosion` で爆発処理を自前で行い、最後に `Despawn(Manual)` を呼ぶ
- **デリゲートキャッシュ**: `_fuseDelegate = OnFuseExplosion` を `OnSimulateStart` で1回確保。発射ごとのデリゲート new を防ぐ
- **拡散の決定論性**: `new System.Random(context.Seed)` で全ピア同一。MP で拡散方向が揃う
- **`ModResource` 優先**: メッシュ/テクスチャはまず `ModResource.GetMesh/GetTexture` を試みてから AssetBundle にフォールバック。これが元 ACM のリソース管理手法と一致

---

## 未実装(将来フェーズ)

| 機能 | 理由 |
|---|---|
| ミサイル誘導 (useBeacon / GuidRatio / GuidType) | M6 スコープ |
| エフェクト / メッシュ (ECannon.xml) | ゲーム側に Mod.xml リソース登録が必要。テスト環境で手動設定後に有効化 |
| `AdBlockProp` (BlockPhysicsModule) | **契約変更が必要**。ConfigurableJoint/SpringMotion/RotateMotion/サブオブジェクト差し替え等を持つ汎用ブロック物理モジュールで、Weapons系のIWeaponHost/IWeaponRegistryとは無関係。現在のCore/PluginApiには武装(発射)以外のBlockModuleをホストする契約が一切無い(IBlockAccessorは値の読み取りのみ)。ROADMAP.mdにもM0~M6の範囲外で、対応するマイルストーンが存在しない。新規Core契約(例: 汎用BlockModuleホスト)の設計が必要なため実装着手せず停止。次セッションで設計セッションとして起こすことを推奨 |

---

## ShootingModule 追加機能(2026-06-16、M5後の追加セッション)

ユーザー指定の優先順で全6項目を実装。ビルド警告0・エラー0で確認済み(実機確認は未)。

| 機能 | 実装場所 | 注記 |
|---|---|---|
| useMagazine(装弾数/リロード) | `MagazineState.cs`(新規)、`OldCannonModule`/`OldCannonWeapon` | `docs/ACM/magazine-system.md` で確証済み。初期化: `AmmoLeft=min(DefaultAmmo,Capacity)`,`AmmoStock=残り`。補充: `min(AmmoStock, Capacity-AmmoLeft)`。未対応: `MagazineCapacitySlider.ValueChanged` での整数スナップUI(UIの見た目のみ、読み取り時は `RoundToInt` で機能的に同等) |
| useBooster + useThrustDelayTimer | `ProjectileBoosterBehaviour.cs`(新規,旧ProjectileBoosterTimer置換)、`OldCannonModule`/`OldCannonHostBehaviour` | `docs/ACM/missile-behavior.md` §5,§7で確証済み。`useBooster=true`でスラスターシステム全体有効: パージ(`AddRelativeForce(PurgeVector × PurgePower × 100f)`)+横方向安定ドラッグ(WingDragBehavour2相当)+連続前方推力(`AddForce(forward × PowerSlider.Value × 100f)` / FixedUpdate毎)。`useThrustDelayTimer`は点火遅延時間のみ制御(false=即時点火)。`boosterPower = PowerSlider.Value`(原ACM互換)。DragPrefab2実体Prefabは原ACMアセット依存のため生成不可。横方向ドラッグのみコードで再現 |
| 衝突有効化遅延 | `ProjectilePhysicsSetup.cs` | 発射後0.02秒間コライダー無効化(固定値、XMLキーなし) |
| 接着効果(Attaches) | `OldCannonWeapon.OnImpact`/`OldCannonHostBehaviour.AttachProjectile` | 契約変更不要と判明: `IProjectileService.TryGetGameObject` + 既存の寿命タイムアウト(`LifetimeCoroutine`)で十分。Despawn時に親子関係を復元するハンドラを追加 |
| 凍結効果 | `FreezeRegistry.cs`(新規,Compat)、`FreezeApplierAdapter.cs`(新規,Adapter)、`OldCannonModule.UseFreezingAttack` | `docs/decompiled/{BlockBehaviour,IceTag,BlockPrefab}.cs` から確認したネイティブ凍結アルゴリズム(`gotChildBlocks`→`CreateSimLists`→子の`canFreeze`→`iceTag.Freeze()`、自身も同様)をAdapterで再現。XMLキー名は **`useFreezingAttack`**(`docs/XML/ACMモジュール.xml` 287行で確証済み)。`ShootingState` ではなく `AdShootingProp` 直下のトップレベルフィールドだったため `OldCannonModule` 側に実装し直した |
| 跳弾効果 | (削除済み) | 原ACMには専用フラグが存在しないと判明(`docs/XML/ACMモジュール.xml` 176-179行: 跳弾は `BounceStr`/`BounceCombineType` の PhysicMaterial bounciness のみで実現)。一旦 `ShootingState.Deflectable`+`Vector3.Reflect` として実装したが、誤認防止のためユーザー判断で削除。`BounceStr`/`BounceCombineType` は元から実装済みなので機能的な欠落はない |

### 停止した項目

`AdBlockProp` 新規モジュール着手(ユーザー指定の「時間が余れば」項目)は、上表の通り契約変更が必要なため着手せず停止。

---

## 実機確認が必要な項目 (M5)

- [x] ECannon ブロックが Besiege で選択・配置できる
- [x] C キーで発射できる (hold-to-shoot)
- [x] 弾体が重力に従い飛翔し、3 秒後にタイムフューズ爆発する (`useTimefuse=true`)
- [x] 20 発で弾切れになる (`DefaultAmmo=20`)
- [x] リコイルでブロックが後退する (`RecoilMultiplier=0.8`)
- [x] 複数発の弾道が微妙にバラける (`RandomDiffusion=0.007`)
- [x] `useBurstShot=true` に変更したとき 3 発バーストになる
- [x] `useDelay=true` に変更したとき弾体スポーンが遅延する
- [ ] カプセルコライダー(弾頭)が壁に当たって衝突判定が取れる (未確認・おそらくOK)
- [ ] MP: 弾体スポーン・フューズ爆発がホスト・クライアント双方で見える (未確認)

### 実機確認が必要な項目(追加機能)

- [x] `useMagazine=true` で、装弾数が尽きると自動/手動リロードが発生し、リロード中は発射できない
- [x] `useBooster=true` で発射直後にパージ推力がかかり加速・直進する(横方向安定=フィン効果も確認済み)
- [x] `useThrustDelayTimer=true` でパージ後に点火遅延が発生し、点火後に推力開始する
- [ ] ECannon.xml: PowerSlider が連続推力の大きさを兼ねていることを確認 (未確認)
- [ ] 発射直後の0.02秒間、弾頭が発射元ブロックに自爆判定しない(衝突有効化遅延) (未確認)
- [ ] `ShootingState.Attaches=true` で、命中対象に弾が刺さって止まり、シミュ終了時にクラッシュしない (未確認)
- [ ] `useFreezingAttack=true` で、命中したブロック(と子ブロック)が凍結する (未確認)

### 修正済みバグ (2026-06-17)

- **二重爆発バグ**: `ProjectilesExplode=true` + `ProjectilesDespawnImmediately=false` の弾体が衝突後も GO が残り、タイムフューズでも爆発していた。`OldCannonWeapon.OnImpact` で `_projectilesExplode=true` のときも `Despawn(Impact)` するよう修正。`ProjectileFuseTimer.OnDisable` でフューズがキャンセルされ二重爆発が解消。

---

## 既知の落とし穴・注意点

### ビルドツール: VS MSBuildを使うこと
```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" src/acmu/acmu.csproj /p:Configuration=Release /nologo /v:minimal
```

### git コミット (PowerShell)
```powershell
git -c user.name="ユーザー名" -c user.email="mail@example.com" commit -m @'
コミットメッセージ
'@
```

### ModIO名前空間の衝突
`Modding.ModIO.ExistsFile()` — 完全修飾必須。

### `CollisionTypeS` の大文字 S
`<CollisionTypeS>ContinuousDynamic</CollisionTypeS>` — 末尾が大文字 S。旧 ACM XML は `<CollisionTypes>` だったが、ShootingState フィールド名に合わせて修正済み。

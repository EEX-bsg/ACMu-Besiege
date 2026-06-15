# ACMu 実装進捗 — セッション引き継ぎメモ

最終更新: 2026-06-16 (M5 完了)

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
| `Attaches = true` | 着弾時 Despawn 抑止 API が IProjectileService に存在しない。契約変更が必要 |
| ミサイル誘導 (useBeacon / GuidRatio / GuidType) | M6 スコープ |
| useBooster / useThrustDelayTimer | M6 スコープ |
| useMagazine / マガジン弾倉システム | M6 スコープ |
| エフェクト / メッシュ (ECannon.xml) | ゲーム側に Mod.xml リソース登録が必要。テスト環境で手動設定後に有効化 |

---

## 実機確認が必要な項目 (M5)

- [ ] ECannon ブロックが Besiege で選択・配置できる
- [ ] C キーで発射できる (hold-to-shoot)
- [ ] 弾体が重力に従い飛翔し、3 秒後にタイムフューズ爆発する (`useTimefuse=true`)
- [ ] 20 発で弾切れになる (`DefaultAmmo=20`)
- [ ] リコイルでブロックが後退する (`RecoilMultiplier=0.8`)
- [ ] 複数発の弾道が微妙にバラける (`RandomDiffusion=0.007`)
- [ ] `useBurstShot=true` に変更したとき 3 発バーストになる
- [ ] `useDelay=true` に変更したとき弾体スポーンが 0.1 秒遅延する
- [ ] カプセルコライダー(弾頭)が壁に当たって衝突判定が取れる
- [ ] MP: 弾体スポーン・フューズ爆発がホスト・クライアント双方で見える

---

## 既知の落とし穴・注意点

### ビルドツール: VS MSBuildを使うこと
```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" src/acmu/acmu.csproj /p:Configuration=Release /nologo /v:minimal
```

### git コミット (PowerShell)
```powershell
git -c user.name="EEX-bsg" -c user.email="exendra314@gmail.com" commit -m @'
コミットメッセージ
'@
```

### ModIO名前空間の衝突
`Modding.ModIO.ExistsFile()` — 完全修飾必須。

### `CollisionTypeS` の大文字 S
`<CollisionTypeS>ContinuousDynamic</CollisionTypeS>` — 末尾が大文字 S。旧 ACM XML は `<CollisionTypes>` だったが、ShootingState フィールド名に合わせて修正済み。

using System;
using System.Collections.Generic;
using ACMu.Core.Weapons;
using ACMu.Weapons;
using UnityEngine;

namespace ACMu.Compat.Shooting
{
    public class OldCannonWeapon : WeaponComponentBase
    {
        internal const string SharedProjectileKey = "compat-adshooting";

        // ブロックに対応スライダーが無い場合のみ使うフォールバック既定値
        private const int   FallbackMagazineCapacity = 10;
        private const float FallbackReloadTime       = 2f;

        // OldCannonHostBehaviour が OnSimulateStart 内でセットし、AttachTo 完了後にクリアする。
        // Unity はシングルスレッドのためこの static パターンは安全。
        internal static OldCannonModule LoadingModule;

        // OnAttached でキャッシュ
        private bool   _projectilesExplode;
        private bool   _projectilesDespawnImmediately;
        private float  _explodePower;
        private float  _explodeUpPower;
        private string _bundleName;
        private string _explodeEffectName;
        private string _shotFlashEffectName;
        private int    _poolSize;
        private float  _blockDamage;
        private float  _entityDamage;
        private bool   _attaches;
        private bool   _freezing;

        // OnSimulationStart でキャッシュ
        private float  _recoilMultiplier;
        private float  _randomDiffusion;
        private float  _randomInterval;
        private bool   _useDelay;
        private float  _delayTime;
        private bool   _useBurstShot;
        private float  _rateOfBurst;
        private int    _burstShotNum;
        private int    _defaultAmmo;

        // マガジン/リロード(useMagazine)
        private bool _useMagazine;
        private bool _useReloadKey;
        private bool _forceAutoReload;
        private bool _useAutoReloadToggle;

        // サウンドリスト: OnSimulationStart で一度構築、ホットパスで参照のみ
        private readonly List<string> _soundNames     = new List<string>();
        private readonly List<string> _hitSoundNames  = new List<string>();
        private readonly List<string> _reloadSoundNames = new List<string>();

        // バースト/弾薬管理の実行時状態
        private float _savedInterval;
        private int   _burstRemaining;
        private int   _ammoRemaining;
        // バースト完了後、次のバーストを開始できる Time.time。通常レート分の間隔を空ける
        private float _burstCooldownUntil;

        // マガジン実行時状態(useMagazine時のみ使用)
        private int   _ammoLeft;
        private int   _ammoStock;
        private bool  _isReloading;
        private float _reloadTimeRemaining;

        protected override void OnAttached()
        {
            Host.BaseSpec.ProjectileKey             = SharedProjectileKey;
            Host.BaseSpec.MuzzleVelocity            = 250f;
            Host.BaseSpec.FireIntervalSeconds       = 0.37f;
            Host.BaseSpec.ProjectileLifetimeSeconds = 20f;
            Host.BaseSpec.Damage                    = 0f;
            Host.BaseSpec.ExplosionRadius           = 0f;

            OldCannonModule m = LoadingModule;
            if (m == null) return;

            _projectilesExplode           = m.ProjectilesExplode;
            _projectilesDespawnImmediately = m.ProjectilesDespawnImmediately;
            _explodePower                 = m.ExplodePower;
            _explodeUpPower      = m.ExplodeUpPower;
            _bundleName          = m.AssetBundleName != null ? m.AssetBundleName.Name : "";
            _explodeEffectName   = m.ExplodeEffect;
            _shotFlashEffectName = m.ShotFlashEffect;
            _poolSize            = m.PoolSize;

            _blockDamage  = m.Shooting != null ? m.Shooting.BlockDamage  : 0f;
            _entityDamage = m.Shooting != null ? m.Shooting.EntityDamage : 0f;
            _attaches     = m.Shooting != null && m.Shooting.Attaches;
            _freezing     = m.UseFreezingAttack;

            Host.BaseSpec.Damage          = _entityDamage;
            Host.BaseSpec.ExplosionRadius = m.ProjectilesExplode ? m.ExplodeRadius : 0f;
            Host.BaseSpec.PoolSize        = m.PoolSize;
            if (m.ProjectilesDespawnImmediately)
                Host.BaseSpec.ProjectileLifetimeSeconds = 0.05f;
        }

        protected override void OnSimulationStart()
        {
            OldCannonModule m = LoadingModule;
            if (m == null) return;

            _recoilMultiplier = m.RecoilMultiplier;
            _randomDiffusion  = m.RandomDiffusion;
            _randomInterval   = m.RandomInterval;
            _useDelay         = m.UseDelay;
            _delayTime        = m.DelayTime;
            _useBurstShot     = m.UseBurstShot;
            _rateOfBurst      = m.RateOfBurst;
            _burstShotNum     = m.BurstShotNum > 0 ? m.BurstShotNum : 3;
            _defaultAmmo      = m.DefaultAmmo;

            _savedInterval  = Host.BaseSpec.FireIntervalSeconds;
            _burstRemaining = 0;
            _ammoRemaining  = _defaultAmmo;
            _burstCooldownUntil = 0f;

            _soundNames.Clear();
            if (m.Sounds != null)
                foreach (var s in m.Sounds)
                    if (s != null && !string.IsNullOrEmpty(s.Name)) _soundNames.Add(s.Name);

            _hitSoundNames.Clear();
            if (m.HitSounds != null)
                foreach (var s in m.HitSounds)
                    if (s != null && !string.IsNullOrEmpty(s.Name)) _hitSoundNames.Add(s.Name);

            _useMagazine = m.UseMagazine;
            MagazineState mag = m.MagazineInfo;
            _useReloadKey        = mag != null && mag.UseReloadKey;
            _forceAutoReload     = mag != null && mag.ForceAutoReload;
            _useAutoReloadToggle = mag != null && mag.UseAutoReloadToggle;

            _reloadSoundNames.Clear();
            if (mag != null && mag.ReloadSounds != null)
                foreach (var s in mag.ReloadSounds)
                    if (s != null && !string.IsNullOrEmpty(s.Name)) _reloadSoundNames.Add(s.Name);

            _isReloading = false;
            _reloadTimeRemaining = 0f;

            if (_useMagazine)
            {
                int capacity = GetMagazineCapacity();
                _ammoLeft  = Mathf.Min(_defaultAmmo, capacity);
                _ammoStock = Mathf.Max(_defaultAmmo - capacity, 0);
            }
            else
            {
                _ammoLeft  = 0;
                _ammoStock = 0;
            }
        }

        protected override void OnSimulationStop()
        {
            // バースト中にシミュが止まったとき、インターバルを戻す
            if (_useBurstShot && _savedInterval > 0f)
                Host.BaseSpec.FireIntervalSeconds = _savedInterval;
            _burstRemaining = 0;
            _burstCooldownUntil = 0f;

            _isReloading = false;
            _reloadTimeRemaining = 0f;
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (!Host.IsAuthority) return;

            if (_useMagazine)
                UpdateMagazine(deltaTime);

            bool holdToShoot;
            if (!Host.Block.TryGetToggle(OldCannonModule.HoldToShootToggleName, out holdToShoot))
                holdToShoot = true;

            bool keyHeld    = Host.Block.IsKeyHeld(OldCannonModule.FireKeyName);
            bool keyPressed = Host.Block.IsKeyPressed(OldCannonModule.FireKeyName);
            bool trigger    = holdToShoot ? keyHeld : keyPressed;

            if (_useBurstShot)
            {
                // 新バースト開始: トリガー、前のバーストが完了済み、通常レート分のクールダウンも経過済み
                if (trigger && _burstRemaining == 0 && Time.time >= _burstCooldownUntil)
                {
                    _burstRemaining = _burstShotNum;
                    Host.BaseSpec.FireIntervalSeconds = _rateOfBurst > 0f ? 1f / _rateOfBurst : 0.1f;
                }
                if (_burstRemaining > 0)
                    Host.RequestFire();
            }
            else
            {
                if (trigger)
                    Host.RequestFire();
            }
        }

        protected override FireDecision OnValidateFire(FireContext context)
        {
            if (_defaultAmmo <= 0 || GameRulesRegistry.IsInfiniteAmmo())
                return FireDecision.Proceed;

            if (_useMagazine)
            {
                if (_isReloading || _ammoLeft <= 0) return FireDecision.Suppress;
                return FireDecision.Proceed;
            }

            if (_ammoRemaining <= 0) return FireDecision.Suppress;
            return FireDecision.Proceed;
        }

        protected override void OnBeforeFire(FireContext context)
        {
            // スライダーから弾速 / 発射レート上書き
            float power;
            if (Host.Block.TryGetSlider(OldCannonModule.PowerSliderName, out power))
                context.Shot.MuzzleVelocity = power;

            float rateOfFire;
            if (!_useBurstShot && Host.Block.TryGetSlider(OldCannonModule.RateOfFireSliderName, out rateOfFire))
                Host.BaseSpec.FireIntervalSeconds = rateOfFire;

            // バースト残弾カウント処理: バースト完了時、通常レート分のクールダウンを設定
            if (_useBurstShot && _burstRemaining > 0)
            {
                _burstRemaining--;
                if (_burstRemaining == 0)
                {
                    float normalInterval;
                    if (!Host.Block.TryGetSlider(OldCannonModule.RateOfFireSliderName, out normalInterval))
                        normalInterval = _savedInterval;
                    Host.BaseSpec.FireIntervalSeconds = normalInterval;
                    _burstCooldownUntil = Time.time + normalInterval;
                }
            }

            // 弾薬消費(GODMODE 弾薬無限時は消費しない)
            if (_defaultAmmo > 0 && !GameRulesRegistry.IsInfiniteAmmo())
            {
                if (_useMagazine)
                {
                    if (_ammoLeft > 0) _ammoLeft--;
                }
                else if (_ammoRemaining > 0)
                {
                    _ammoRemaining--;
                }
            }

            // ランダム拡散: Seed 由来の乱数で全ピア決定論的
            if (_randomDiffusion > 0f)
            {
                var rng = new System.Random(context.Seed);
                float dx = (float)(rng.NextDouble() * 2.0 - 1.0) * _randomDiffusion;
                float dy = (float)(rng.NextDouble() * 2.0 - 1.0) * _randomDiffusion;
                context.Direction = (context.Direction
                    + context.MuzzleRotation * new Vector3(dx, dy, 0f)).normalized;
            }

            // スポーン遅延
            if (_useDelay && _delayTime > 0f)
                context.DelaySeconds = _delayTime;

            // ランダムインターバルジッター(発射タイミングのバラつき)
            if (_randomInterval > 0f)
                context.DelaySeconds += UnityEngine.Random.Range(0f, _randomInterval);
        }

        // useMagazine時の毎フレーム処理: リロード進行、または自動/手動リロード開始判定
        private void UpdateMagazine(float deltaTime)
        {
            if (_isReloading)
            {
                _reloadTimeRemaining -= deltaTime;
                if (_reloadTimeRemaining <= 0f)
                    FinishReload();
                return;
            }

            bool reloadKeyPressed = _useReloadKey && Host.Block.IsKeyPressed(OldCannonModule.ReloadKeyName);
            int  capacity = GetMagazineCapacity();

            if (reloadKeyPressed && _ammoStock > 0 && _ammoLeft < capacity)
            {
                StartReload();
                return;
            }

            bool autoToggleOn;
            if (!Host.Block.TryGetToggle(OldCannonModule.AutoReloadToggleName, out autoToggleOn))
                autoToggleOn = false;

            bool wantsAutoReload = _forceAutoReload || (_useAutoReloadToggle && autoToggleOn);
            if (wantsAutoReload && _ammoLeft <= 0 && _ammoStock > 0)
                StartReload();
        }

        private void StartReload()
        {
            _isReloading = true;

            float reloadTime;
            if (!Host.Block.TryGetSlider(OldCannonModule.ReloadTimeSliderName, out reloadTime))
                reloadTime = FallbackReloadTime;
            _reloadTimeRemaining = Mathf.Max(reloadTime, 0f);

            EffectRegistry.PlaySounds(_reloadSoundNames, Host.MuzzlePosition);
        }

        private void FinishReload()
        {
            _isReloading = false;
            _reloadTimeRemaining = 0f;

            int capacity = GetMagazineCapacity();
            int space = capacity - _ammoLeft;
            if (space <= 0) return;
            int moved = Mathf.Min(_ammoStock, space);
            _ammoLeft  += moved;
            _ammoStock -= moved;
        }

        private int GetMagazineCapacity()
        {
            float capacity;
            if (!Host.Block.TryGetSlider(OldCannonModule.MagazineCapacitySliderName, out capacity))
                return FallbackMagazineCapacity;
            return Mathf.Max(Mathf.RoundToInt(capacity), 1);
        }

        protected override void OnAfterFire(FireContext context, ProjectileHandle projectile)
        {
            // 発射フラッシュエフェクト / 発射音: useDelay 等で DelaySeconds が掛かっている場合、
            // 弾が実際に発生するこのタイミングまで遅らせて同期させる(専用位置があればそちらを使う)
            var host = Host as OldCannonHostBehaviour;
            Vector3    flashPos = host != null ? host.FlashMuzzlePosition : Host.MuzzlePosition;
            Quaternion flashRot = host != null ? host.FlashMuzzleRotation : Host.MuzzleRotation;
            EffectRegistry.Spawn(_bundleName, _shotFlashEffectName, flashPos, flashRot, _poolSize, true);
            EffectRegistry.PlaySounds(_soundNames, flashPos);

            if (!projectile.IsValid) return;

            // 弾体エフェクト / フューズ / メッシュ等をアタッチ
            if (host != null)
                host.AttachProjectileEffects(projectile);

            // リコイル: 砲身の Rigidbody に逆方向の衝撃を加える
            if (_recoilMultiplier > 0f && host != null)
            {
                var blockRb = host.GetComponent<Rigidbody>();
                if (blockRb != null)
                    blockRb.AddForce(
                        -context.Direction * context.Shot.MuzzleVelocity * _recoilMultiplier,
                        ForceMode.Impulse);
            }
        }

        protected override void OnImpact(ImpactContext context)
        {
            if (context.HitObject != null && (_blockDamage > 0f || _entityDamage > 0f))
                DamageRegistry.Apply(context.HitObject, _blockDamage, _entityDamage);

            // useFreezingAttack=true: 命中対象(と子ブロック)を凍結する
            if (_freezing && context.HitObject != null)
                FreezeRegistry.Apply(context.HitObject);

            // Attaches=true: 命中対象に刺さって止まる。Despawn は寿命タイムアウト任せ(原ACM互換)
            if (_attaches && context.HitObject != null)
            {
                var attachHost = Host as OldCannonHostBehaviour;
                if (attachHost != null)
                {
                    attachHost.AttachProjectile(context.Projectile, context.HitObject.transform);
                    return;
                }
            }

            // ProjectilesExplode=true: OnExplosion の前に Despawn → ProjectileFuseTimer.OnDisable でフューズキャンセル → 二重爆発防止
            // ProjectilesDespawnImmediately=true: 爆発なし即消滅
            // いずれでもない(貫通弾等): フューズ / 寿命タイムアウトで消える
            if (_projectilesExplode || _projectilesDespawnImmediately)
                Host.Projectiles.Despawn(context.Projectile, DespawnReason.Impact);
        }

        protected override void OnExplosion(ImpactContext context)
        {
            if (!_projectilesExplode || context.ExplosionRadius <= 0f) return;
            if (context.SuppressDefaultExplosion) return;

            // 着弾音(原ACM互換: HitSounds は着弾時ではなく爆発時に再生される)
            EffectRegistry.PlaySounds(_hitSoundNames, context.Position);

            float scaledPower   = _explodePower   * 2f;
            float scaledUpPower = _explodeUpPower * 0.25f;

            Collider[] hits = Physics.OverlapSphere(context.Position, context.ExplosionRadius);
            foreach (Collider col in hits)
            {
                Rigidbody rb = col.attachedRigidbody;
                if (rb != null)
                    rb.AddExplosionForce(scaledPower, context.Position, context.ExplosionRadius, scaledUpPower);
            }

            EffectRegistry.Spawn(_bundleName, _explodeEffectName, context.Position, Quaternion.identity, _poolSize, true);
        }
    }
}

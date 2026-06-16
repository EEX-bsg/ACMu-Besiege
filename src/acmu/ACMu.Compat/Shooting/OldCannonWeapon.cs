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

        // サウンドリスト: OnSimulationStart で一度構築、ホットパスで参照のみ
        private readonly List<string> _soundNames    = new List<string>();
        private readonly List<string> _hitSoundNames = new List<string>();

        // バースト/弾薬管理の実行時状態
        private float _savedInterval;
        private int   _burstRemaining;
        private int   _ammoRemaining;

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

            Host.BaseSpec.Damage          = _entityDamage;
            Host.BaseSpec.ExplosionRadius = m.ProjectilesExplode ? m.ExplodeRadius : 0f;
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
            _burstShotNum     = m.BurstShotNum;
            _defaultAmmo      = m.DefaultAmmo;

            _savedInterval  = Host.BaseSpec.FireIntervalSeconds;
            _burstRemaining = 0;
            _ammoRemaining  = _defaultAmmo;

            _soundNames.Clear();
            if (m.Sounds != null)
                foreach (var s in m.Sounds)
                    if (s != null && !string.IsNullOrEmpty(s.Name)) _soundNames.Add(s.Name);

            _hitSoundNames.Clear();
            if (m.HitSounds != null)
                foreach (var s in m.HitSounds)
                    if (s != null && !string.IsNullOrEmpty(s.Name)) _hitSoundNames.Add(s.Name);
        }

        protected override void OnSimulationStop()
        {
            // バースト中にシミュが止まったとき、インターバルを戻す
            if (_useBurstShot && _savedInterval > 0f)
                Host.BaseSpec.FireIntervalSeconds = _savedInterval;
            _burstRemaining = 0;
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (!Host.IsAuthority) return;

            bool holdToShoot;
            if (!Host.Block.TryGetToggle(OldCannonModule.HoldToShootToggleName, out holdToShoot))
                holdToShoot = true;

            bool keyHeld    = Host.Block.IsKeyHeld(OldCannonModule.FireKeyName);
            bool keyPressed = Host.Block.IsKeyPressed(OldCannonModule.FireKeyName);
            bool trigger    = holdToShoot ? keyHeld : keyPressed;

            if (_useBurstShot)
            {
                // 新バースト開始: トリガーかつ前のバーストが完了済み
                if (trigger && _burstRemaining == 0)
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
            if (_defaultAmmo > 0 && _ammoRemaining <= 0 && !GameRulesRegistry.IsInfiniteAmmo())
                return FireDecision.Suppress;
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

            // バースト残弾カウント処理
            if (_useBurstShot && _burstRemaining > 0)
            {
                _burstRemaining--;
                if (_burstRemaining == 0)
                    Host.BaseSpec.FireIntervalSeconds = _savedInterval;
            }

            // 弾薬消費(GODMODE 弾薬無限時は消費しない)
            if (_defaultAmmo > 0 && _ammoRemaining > 0 && !GameRulesRegistry.IsInfiniteAmmo())
                _ammoRemaining--;

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

            // 発射フラッシュエフェクト(専用位置があればそちらを使う)
            var hostBehaviour = Host as OldCannonHostBehaviour;
            Vector3    flashPos = hostBehaviour != null ? hostBehaviour.FlashMuzzlePosition : Host.MuzzlePosition;
            Quaternion flashRot = hostBehaviour != null ? hostBehaviour.FlashMuzzleRotation : Host.MuzzleRotation;
            EffectRegistry.Spawn(_bundleName, _shotFlashEffectName, flashPos, flashRot, _poolSize, true);

            // 発射音
            EffectRegistry.PlaySounds(_soundNames, flashPos);
        }

        protected override void OnAfterFire(FireContext context, ProjectileHandle projectile)
        {
            if (!projectile.IsValid) return;

            // 弾体エフェクト / フューズ / メッシュ等をアタッチ
            var host = Host as OldCannonHostBehaviour;
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

            // 着弾音
            EffectRegistry.PlaySounds(_hitSoundNames, context.Position);

            // ProjectilesDespawnImmediately=true のときのみ着弾で消える。
            // false の場合はフューズ / 寿命タイムアウトで消える(原ACM互換)。
            if (_projectilesDespawnImmediately)
                Host.Projectiles.Despawn(context.Projectile, DespawnReason.Impact);
        }

        protected override void OnExplosion(ImpactContext context)
        {
            if (!_projectilesExplode || context.ExplosionRadius <= 0f) return;
            if (context.SuppressDefaultExplosion) return;

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

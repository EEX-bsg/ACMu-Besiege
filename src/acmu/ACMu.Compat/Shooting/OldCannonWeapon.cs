using ACMu.Core.Weapons;
using UnityEngine;

namespace ACMu.Compat.Shooting
{
    public class OldCannonWeapon : WeaponComponentBase
    {
        internal const string SharedProjectileKey = "compat-adshooting";

        // OldCannonHostBehaviour が OnSimulateStart 内でセットし、AttachTo 完了後にクリアする。
        // Unity はシングルスレッドのためこの static パターンは安全。
        internal static OldCannonModule LoadingModule;

        private bool   _projectilesExplode;
        private float  _explodePower;
        private float  _explodeUpPower;
        private string _bundleName;
        private string _explodeEffectName;
        private string _shotFlashEffectName;
        private int    _poolSize;
        private float  _blockDamage;
        private float  _entityDamage;

        protected override void OnAttached()
        {
            Host.BaseSpec.ProjectileKey             = SharedProjectileKey;
            Host.BaseSpec.MuzzleVelocity            = 250f;
            Host.BaseSpec.FireIntervalSeconds       = 0.37f;
            Host.BaseSpec.ProjectileLifetimeSeconds = 5f;
            Host.BaseSpec.Damage                    = 0f;
            Host.BaseSpec.ExplosionRadius           = 0f;

            OldCannonModule m = LoadingModule;
            if (m == null) return;

            _projectilesExplode  = m.ProjectilesExplode;
            _explodePower        = m.ExplodePower;
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

        protected override void OnUpdate(float deltaTime)
        {
            if (!Host.IsAuthority) return;

            bool holdToShoot;
            if (!Host.Block.TryGetToggle(OldCannonModule.HoldToShootToggleName, out holdToShoot))
                holdToShoot = true;

            bool fire;
            if (holdToShoot)
                fire = Host.Block.IsKeyHeld(OldCannonModule.FireKeyName);
            else
                fire = Host.Block.IsKeyPressed(OldCannonModule.FireKeyName);

            if (fire)
                Host.RequestFire();
        }

        protected override void OnBeforeFire(FireContext context)
        {
            float power;
            if (Host.Block.TryGetSlider(OldCannonModule.PowerSliderName, out power))
                context.Shot.MuzzleVelocity = power;

            float rateOfFire;
            if (Host.Block.TryGetSlider(OldCannonModule.RateOfFireSliderName, out rateOfFire))
                Host.BaseSpec.FireIntervalSeconds = rateOfFire;

            EffectRegistry.Spawn(_bundleName, _shotFlashEffectName, Host.MuzzlePosition, Host.MuzzleRotation, _poolSize, true);
        }

        // 発射後: このホストが所有する弾体にエフェクトをアタッチする。
        // Projectiles.Spawned はグローバルイベントで全ホストに届くが、
        // OnAfterFire は発射元ホストにのみ届くため複数ブロック配置でも重複しない。
        protected override void OnAfterFire(FireContext context, ProjectileHandle projectile)
        {
            if (!projectile.IsValid) return;
            var host = Host as OldCannonHostBehaviour;
            if (host != null) host.AttachProjectileEffects(projectile);
        }

        // 直接衝突時のダメージ。爆発範囲ダメージは含まない(docs/ACM/explosion-mechanics.md §3 参照)。
        protected override void OnImpact(ImpactContext context)
        {
            if (context.HitObject != null && (_blockDamage > 0f || _entityDamage > 0f))
                DamageRegistry.Apply(context.HitObject, _blockDamage, _entityDamage);
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

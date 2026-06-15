using ACMu.Core.Weapons;
using UnityEngine;

namespace ACMu.Compat.Shooting
{
    public class AdShootingWeapon : WeaponComponentBase
    {
        internal const string SharedProjectileKey = "compat-adshooting";

        // AdShootingHostBehaviour が OnSimulateStart 内でセットし、AttachTo 完了後にクリアする。
        // Unity はシングルスレッドのためこの static パターンは安全。
        internal static AdShootingModule LoadingModule;

        private bool  _projectilesExplode;
        private float _explodePower;
        private float _explodeUpPower;
        private string _explodeEffectName;
        private string _shotFlashEffectName;

        protected override void OnAttached()
        {
            Host.BaseSpec.ProjectileKey             = SharedProjectileKey;
            Host.BaseSpec.MuzzleVelocity            = 250f;
            Host.BaseSpec.FireIntervalSeconds       = 0.37f;
            Host.BaseSpec.ProjectileLifetimeSeconds = 5f;
            Host.BaseSpec.Damage                    = 0f;
            Host.BaseSpec.ExplosionRadius           = 0f;

            AdShootingModule m = LoadingModule;
            if (m == null) return;

            _projectilesExplode  = m.ProjectilesExplode;
            _explodePower        = m.ExplodePower;
            _explodeUpPower      = m.ExplodeUpPower;
            _explodeEffectName   = m.ExplodeEffect;
            _shotFlashEffectName = m.ShotFlashEffect;

            Host.BaseSpec.Damage          = m.Shooting.EntityDamage;
            Host.BaseSpec.ExplosionRadius = m.ProjectilesExplode ? m.ExplodeRadius : 0f;
            if (m.ProjectilesDespawnImmediately)
                Host.BaseSpec.ProjectileLifetimeSeconds = 0.05f;
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (!Host.IsAuthority) return;

            bool holdToShoot;
            if (!Host.Block.TryGetToggle(AdShootingModule.HoldToShootToggleName, out holdToShoot))
                holdToShoot = true;

            bool fire;
            if (holdToShoot)
                fire = Host.Block.IsKeyHeld(AdShootingModule.FireKeyName);
            else
                fire = Host.Block.IsKeyPressed(AdShootingModule.FireKeyName);

            if (fire)
                Host.RequestFire();
        }

        protected override void OnBeforeFire(FireContext context)
        {
            float power;
            if (Host.Block.TryGetSlider(AdShootingModule.PowerSliderName, out power))
                context.Shot.MuzzleVelocity = power;

            float rateOfFire;
            if (Host.Block.TryGetSlider(AdShootingModule.RateOfFireSliderName, out rateOfFire))
                Host.BaseSpec.FireIntervalSeconds = rateOfFire;

            EffectRegistry.Spawn(_shotFlashEffectName, Host.MuzzlePosition, Host.MuzzleRotation);
        }

        protected override void OnExplosion(ImpactContext context)
        {
            if (!_projectilesExplode || context.ExplosionRadius <= 0f) return;
            if (context.SuppressDefaultExplosion) return;

            // ACM 原典の変換: power × 2, upPower × 0.25 (explosion-mechanics.md §2)
            float scaledPower   = _explodePower   * 2f;
            float scaledUpPower = _explodeUpPower * 0.25f;

            Collider[] hits = Physics.OverlapSphere(context.Position, context.ExplosionRadius);
            foreach (Collider col in hits)
            {
                Rigidbody rb = col.attachedRigidbody;
                if (rb != null)
                    rb.AddExplosionForce(scaledPower, context.Position, context.ExplosionRadius, scaledUpPower);
            }

            EffectRegistry.Spawn(_explodeEffectName, context.Position, Quaternion.identity);
        }
    }
}

using ACMu.Core.Weapons;

namespace ACMu.Compat.Shooting
{
    public class AdShootingWeapon : WeaponComponentBase
    {
        internal const string SharedProjectileKey = "compat-adshooting";

        // AdShootingHostBehaviour が OnSimulateStart 内でセットし、AttachTo 完了後にクリアする。
        // Unity はシングルスレッドのためこの static パターンは安全。
        internal static AdShootingModule LoadingModule;

        protected override void OnAttached()
        {
            Host.BaseSpec.ProjectileKey          = SharedProjectileKey;
            Host.BaseSpec.MuzzleVelocity         = 250f;
            Host.BaseSpec.FireIntervalSeconds     = 0.37f;
            Host.BaseSpec.ProjectileLifetimeSeconds = 5f;
            Host.BaseSpec.Damage                 = 0f;
            Host.BaseSpec.ExplosionRadius        = 0f;

            AdShootingModule m = LoadingModule;
            if (m == null) return;

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
        }
    }
}

using ACMu.Core.Weapons;

namespace ACMu.Compat.TestCannon
{
    public class TestCannonWeapon : WeaponComponentBase
    {
        internal const string ProjectileKey = "acmu-test-sphere";

        protected override void OnAttached()
        {
            Host.BaseSpec.ProjectileKey = ProjectileKey;
            Host.BaseSpec.MuzzleVelocity = 100f;
            Host.BaseSpec.FireIntervalSeconds = 0.5f;
            Host.BaseSpec.ProjectileLifetimeSeconds = 5f;
            Host.BaseSpec.Damage = 0f;
            Host.BaseSpec.ExplosionRadius = 0f;
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (!Host.IsAuthority) return;
            if (Host.Block.IsKeyHeld(TestCannonModule.FireKeyName))
                Host.RequestFire();
        }

        protected override void OnBeforeFire(FireContext context)
        {
            float speed;
            if (Host.Block.TryGetSlider(TestCannonModule.SpeedSliderName, out speed))
                context.Shot.MuzzleVelocity = speed;
        }
    }
}

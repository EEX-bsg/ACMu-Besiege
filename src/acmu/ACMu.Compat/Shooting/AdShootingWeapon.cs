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

            _projectilesExplode = m.ProjectilesExplode;
            _explodePower       = m.ExplodePower;
            _explodeUpPower     = m.ExplodeUpPower;

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

        protected override void OnExplosion(ImpactContext context)
        {
            if (!_projectilesExplode || context.ExplosionRadius <= 0f) return;
            if (context.SuppressDefaultExplosion) return;

            // 範囲内の Rigidbody に爆発力を加える(純 Unity API、Adapter 不要)
            Collider[] hits = Physics.OverlapSphere(context.Position, context.ExplosionRadius);
            foreach (Collider col in hits)
            {
                Rigidbody rb = col.attachedRigidbody;
                if (rb != null)
                    rb.AddExplosionForce(_explodePower, context.Position, context.ExplosionRadius, _explodeUpPower);
            }

            DebugExplosionSphere(context.Position, context.ExplosionRadius);
        }

        // ---- デバッグ用一時ビジュアル(後で削除) ----
        private static void DebugExplosionSphere(Vector3 pos, float radius)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position   = pos;
            sphere.transform.localScale = Vector3.one * radius * 2f;

            var r = sphere.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(1f, 0.35f, 0f);

            // 衝突しないように Collider を除去
            var col = sphere.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            Object.Destroy(sphere, 0.3f);
        }
    }
}

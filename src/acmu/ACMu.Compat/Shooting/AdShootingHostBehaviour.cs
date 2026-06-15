using ACMu.Core.Weapons;
using ACMu.Weapons;
using UnityEngine;

namespace ACMu.Compat.Shooting
{
    public sealed class AdShootingHostBehaviour : WeaponHostBehaviour<AdShootingModule>
    {
        private CollisionDetectionMode _collisionMode = CollisionDetectionMode.ContinuousDynamic;

        public override Vector3 MuzzlePosition
        {
            get
            {
                AdShootingModule m = Module;
                if (m == null || m.ProjectileStart == null) return transform.position;
                return transform.position + transform.rotation * m.ProjectileStart.ToPosition();
            }
        }

        public override Quaternion MuzzleRotation
        {
            get
            {
                AdShootingModule m = Module;
                if (m == null || m.ProjectileStart == null) return transform.rotation;
                return transform.rotation * m.ProjectileStart.ToRotation();
            }
        }

        public override void OnSimulateStart()
        {
            AdShootingModule m = Module;
            if (m != null && m.Shooting != null)
                _collisionMode = ParseCollisionMode(m.Shooting.CollisionTypeS);

            var ps = Projectiles as ProjectileService;
            if (ps != null)
                ps.Spawned += ApplyCollisionMode;

            AdShootingWeapon.LoadingModule = m;
            base.OnSimulateStart();
            AdShootingWeapon.LoadingModule = null;
        }

        public override void OnSimulateStop()
        {
            var ps = Projectiles as ProjectileService;
            if (ps != null)
                ps.Spawned -= ApplyCollisionMode;

            base.OnSimulateStop();
        }

        private void ApplyCollisionMode(ProjectileHandle handle)
        {
            var ps = Projectiles as ProjectileService;
            if (ps == null) return;
            GameObject go;
            if (!ps.TryGetGameObject(handle, out go)) return;
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null)
                rb.collisionDetectionMode = _collisionMode;
        }

        private static CollisionDetectionMode ParseCollisionMode(string s)
        {
            if (s == "Continuous")        return CollisionDetectionMode.Continuous;
            if (s == "ContinuousDynamic") return CollisionDetectionMode.ContinuousDynamic;
            return CollisionDetectionMode.Discrete;
        }
    }
}

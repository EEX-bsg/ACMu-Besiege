using ACMu.Weapons;
using UnityEngine;

namespace ACMu.Compat.Shooting
{
    public sealed class AdShootingHostBehaviour : WeaponHostBehaviour<AdShootingModule>
    {
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
            AdShootingWeapon.LoadingModule = Module;
            base.OnSimulateStart();
            AdShootingWeapon.LoadingModule = null;
        }
    }
}

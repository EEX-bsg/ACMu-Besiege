using ACMu.Weapons;
using UnityEngine;

namespace ACMu.Compat.TestCannon
{
    public sealed class TestCannonHostBehaviour : WeaponHostBehaviour<TestCannonModule>
    {
        public override Vector3 MuzzlePosition
        {
            get
            {
                TestCannonModule m = Module;
                if (m == null) return transform.position;
                Vector3 localOffset = new Vector3(m.MuzzleOffsetX, m.MuzzleOffsetY, m.MuzzleOffsetZ);
                return transform.position + transform.rotation * localOffset;
            }
        }

        public override Quaternion MuzzleRotation
        {
            get
            {
                TestCannonModule m = Module;
                if (m == null) return transform.rotation;
                Vector3 localFwd = new Vector3(m.MuzzleForwardX, m.MuzzleForwardY, m.MuzzleForwardZ);
                if (localFwd == Vector3.zero) return transform.rotation;
                Vector3 worldFwd = transform.rotation * localFwd.normalized;
                return Quaternion.LookRotation(worldFwd, transform.up);
            }
        }
    }
}

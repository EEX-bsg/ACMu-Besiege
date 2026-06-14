using ACMu.Core.Weapons;
using UnityEngine;

namespace ACMu.Weapons
{
    internal sealed class ProjectileBody : MonoBehaviour
    {
        internal ProjectileHandle Handle;
        internal ProjectileService Service;
        private bool _hit;

        internal void PrepareForSpawn(ProjectileHandle handle, ProjectileService service)
        {
            Handle = handle;
            Service = service;
            _hit = false;
        }

        void OnCollisionEnter(Collision col)
        {
            if (_hit || Service == null) return;
            _hit = true;
            Service.HandleImpact(Handle, col);
        }
    }
}

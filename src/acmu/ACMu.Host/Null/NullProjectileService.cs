using System;
using ACMu.Core.Weapons;
using UnityEngine;

namespace ACMu.Host.Null
{
    internal class NullProjectileService : IProjectileService
    {
        public event Action<ProjectileHandle> Spawned { add { } remove { } }
        public event Action<ProjectileHandle, DespawnReason> Despawned { add { } remove { } }

        public void RegisterProjectile(string key, GameObject prefab, int prewarmCount) { }

        public ProjectileHandle Spawn(ProjectileSpawnRequest request)
        {
            return ProjectileHandle.Invalid;
        }

        public void Despawn(ProjectileHandle handle, DespawnReason reason) { }

        public bool IsAlive(ProjectileHandle handle)
        {
            return false;
        }

        public bool TryGetGameObject(ProjectileHandle handle, out GameObject gameObject)
        {
            gameObject = null;
            return false;
        }
    }
}

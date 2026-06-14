using System;
using System.Collections;
using System.Collections.Generic;
using ACMu.Core.Lifecycle;
using ACMu.Core.Logging;
using ACMu.Core.Weapons;
using UnityEngine;

namespace ACMu.Weapons
{
    public class ProjectileService : MonoBehaviour, IProjectileService, ILifecycleParticipant
    {
        private const float DefaultLifetime = 10f;

        private readonly Dictionary<string, Queue<GameObject>> _pools =
            new Dictionary<string, Queue<GameObject>>();
        private readonly Dictionary<string, GameObject> _prefabs =
            new Dictionary<string, GameObject>();
        private readonly Dictionary<int, GameObject> _active =
            new Dictionary<int, GameObject>();
        private readonly Dictionary<int, string> _activeKey =
            new Dictionary<int, string>();

        private int _nextId = 1;
        private int _epoch;
        private ILog _log;

        // Internal event for same-layer (FirePipeline) impact notifications
        internal event Action<ProjectileHandle, Collision> ImpactOccurred;

        public event Action<ProjectileHandle> Spawned;
        public event Action<ProjectileHandle, DespawnReason> Despawned;

        public int InitOrder { get { return 300; } }

        internal void InitializeService(ILog log)
        {
            _log = log;
        }

        public void OnModLoad() { }

        public void OnSimulationStart(bool isMultiplayer)
        {
            _nextId = 1;
            _epoch++;
        }

        public void OnSimulationStop()
        {
            StopAllCoroutines();
            var ids = new List<int>(_active.Keys);
            foreach (int id in ids)
                Despawn(new ProjectileHandle(id, 0), DespawnReason.SimulationStopped);
            _epoch++;
        }

        public void RegisterProjectile(string key, GameObject prefab, int prewarmCount)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("key must not be empty");
            if (prefab == null)
                throw new ArgumentNullException("prefab");
            if (_prefabs.ContainsKey(key))
                throw new InvalidOperationException("[ACMu] ProjectileService: duplicate key: " + key);

            _prefabs[key] = prefab;
            var pool = new Queue<GameObject>();
            _pools[key] = pool;
            for (int i = 0; i < prewarmCount; i++)
            {
                var go = CreateProjectileGo(key, prefab);
                go.SetActive(false);
                pool.Enqueue(go);
            }
        }

        public ProjectileHandle Spawn(ProjectileSpawnRequest request)
        {
            if (request == null || request.Shot == null)
                return ProjectileHandle.Invalid;

            string key = request.Shot.ProjectileKey;
            if (key == null)
                return ProjectileHandle.Invalid;

            Queue<GameObject> pool;
            if (!_pools.TryGetValue(key, out pool))
            {
                if (_log != null) _log.Warn("[ACMu] ProjectileService: unknown key: " + key);
                return ProjectileHandle.Invalid;
            }

            GameObject go;
            if (pool.Count > 0)
            {
                go = pool.Dequeue();
            }
            else
            {
                GameObject prefab;
                _prefabs.TryGetValue(key, out prefab);
                go = CreateProjectileGo(key, prefab);
            }

            var handle = new ProjectileHandle(_nextId++, 0);
            var body = go.GetComponent<ProjectileBody>();
            body.PrepareForSpawn(handle, this);

            go.transform.position = request.Position;
            go.transform.rotation = request.Rotation;
            go.SetActive(true);

            var rb = go.GetComponent<Rigidbody>();
            if (rb != null)
                rb.velocity = request.Velocity;

            _active[handle.Id] = go;
            _activeKey[handle.Id] = key;

            float lifetime = request.Shot.ProjectileLifetimeSeconds > 0f
                ? request.Shot.ProjectileLifetimeSeconds
                : DefaultLifetime;
            StartCoroutine(LifetimeCoroutine(handle, lifetime, _epoch));

            if (Spawned != null) Spawned(handle);

            return handle;
        }

        internal void HandleImpact(ProjectileHandle handle, Collision collision)
        {
            if (!_active.ContainsKey(handle.Id)) return;
            if (ImpactOccurred != null) ImpactOccurred(handle, collision);
            Despawn(handle, DespawnReason.Impact);
        }

        public void Despawn(ProjectileHandle handle, DespawnReason reason)
        {
            if (!handle.IsValid) return;

            GameObject go;
            if (!_active.TryGetValue(handle.Id, out go)) return;

            _active.Remove(handle.Id);
            go.SetActive(false);

            string key;
            if (_activeKey.TryGetValue(handle.Id, out key))
            {
                _activeKey.Remove(handle.Id);
                Queue<GameObject> pool;
                if (_pools.TryGetValue(key, out pool))
                    pool.Enqueue(go);
            }

            if (Despawned != null) Despawned(handle, reason);
        }

        public bool IsAlive(ProjectileHandle handle)
        {
            return handle.IsValid && _active.ContainsKey(handle.Id);
        }

        public bool TryGetGameObject(ProjectileHandle handle, out GameObject gameObject)
        {
            return _active.TryGetValue(handle.Id, out gameObject);
        }

        private GameObject CreateProjectileGo(string key, GameObject prefab)
        {
            var go = Instantiate(prefab);
            UnityEngine.Object.DontDestroyOnLoad(go);
            var body = go.GetComponent<ProjectileBody>();
            if (body == null) body = go.AddComponent<ProjectileBody>();
            return go;
        }

        private IEnumerator LifetimeCoroutine(ProjectileHandle handle, float lifetime, int epoch)
        {
            yield return new WaitForSeconds(lifetime);
            if (_epoch == epoch && IsAlive(handle))
                Despawn(handle, DespawnReason.Timeout);
        }
    }
}

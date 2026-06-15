using System;
using System.Collections;
using System.Collections.Generic;
using ACMu.Core.Game;
using ACMu.Core.Lifecycle;
using ACMu.Core.Logging;
using ACMu.Core.Weapons;
using UnityEngine;

namespace ACMu.Weapons
{
    public class ProjectileService : MonoBehaviour, IProjectileService, ILifecycleParticipant
    {
        private const float DefaultLifetime = 10f;

        // 共有プレハブ・プール
        private readonly Dictionary<string, Queue<GameObject>> _pools =
            new Dictionary<string, Queue<GameObject>>();
        private readonly Dictionary<string, GameObject> _prefabs =
            new Dictionary<string, GameObject>();

        // ホスト/SP: 生存弾
        private readonly Dictionary<int, GameObject> _active =
            new Dictionary<int, GameObject>();
        private readonly Dictionary<int, string> _activeKey =
            new Dictionary<int, string>();
        private readonly Dictionary<int, WeaponSpec> _activeSpec =
            new Dictionary<int, WeaponSpec>();

        // クライアント: プロキシ弾
        private readonly Dictionary<int, GameObject> _proxyGos =
            new Dictionary<int, GameObject>();
        private readonly Dictionary<int, string> _proxyKey =
            new Dictionary<int, string>();
        private readonly Dictionary<int, int> _proxySpawnFrame =
            new Dictionary<int, int>();

        private int _nextId = 1;
        private int _epoch;
        private ILog _log;
        private IGameSessionInfo _session;

        internal event Action<ProjectileHandle, Collision> ImpactOccurred;

        public event Action<ProjectileHandle> Spawned;
        public event Action<ProjectileHandle, DespawnReason> Despawned;

        public int InitOrder { get { return 300; } }

        internal void InitializeService(ILog log, IGameSessionInfo session)
        {
            _log     = log;
            _session = session;
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

            var proxyIds = new List<int>(_proxyGos.Keys);
            foreach (int id in proxyIds)
                DespawnProxy(id, DespawnReason.SimulationStopped);

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
            // 不変条件(1): スポーン権威はホスト。クライアントは Invalid を返す。
            if (_session != null && !_session.IsSimulating)
                return ProjectileHandle.Invalid;

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

            int ownerId = _session != null ? _session.LocalPlayerId : 0;
            var handle  = new ProjectileHandle(_nextId++, ownerId);

            var body = go.GetComponent<ProjectileBody>();
            body.PrepareForSpawn(handle, this);

            go.transform.position = request.Position;
            go.transform.rotation = request.Rotation;
            go.SetActive(true);

            var rb = go.GetComponent<Rigidbody>();
            if (rb != null)
                rb.velocity = request.Velocity;

            _active[handle.Id]     = go;
            _activeKey[handle.Id]  = key;
            _activeSpec[handle.Id] = request.Shot;

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
            _activeSpec.Remove(handle.Id);
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

        // ProjectileSyncTransport から Spawn 時の WeaponSpec を取得するために使用
        internal WeaponSpec GetSpawnSpec(int handleId)
        {
            WeaponSpec spec;
            _activeSpec.TryGetValue(handleId, out spec);
            return spec;
        }

        // ---- クライアント側プロキシ受信 ----

        internal void ReceiveProxySpawn(
            int handleId, int ownerPlayerId, string key,
            Vector3 pos, Vector3 vel,
            float lifetimeSeconds, float explosionRadius,
            int spawnFrame)
        {
            if (_active.ContainsKey(handleId))  return; // ホスト側で生存中
            if (_proxyGos.ContainsKey(handleId)) return; // 既に受信済み

            Queue<GameObject> pool;
            if (!_pools.TryGetValue(key, out pool))
            {
                if (_log != null) _log.Warn("[ACMu] ProxySpawn: unknown key=" + key);
                return;
            }

            GameObject prefab;
            _prefabs.TryGetValue(key, out prefab);
            GameObject go = pool.Count > 0 ? pool.Dequeue() : CreateProjectileGo(key, prefab);

            // 物理・衝突無効化(見た目のみ)
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            var col = go.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            var proxy = go.GetComponent<ProxyProjectile>();
            if (proxy == null) proxy = go.AddComponent<ProxyProjectile>();
            proxy.Initialize(pos, vel);

            go.SetActive(true);
            _proxyGos[handleId]        = go;
            _proxyKey[handleId]        = key;
            _proxySpawnFrame[handleId] = spawnFrame;

            float lifetime = lifetimeSeconds > 0f ? lifetimeSeconds : DefaultLifetime;
            var handle = new ProjectileHandle(handleId, ownerPlayerId);
            StartCoroutine(ProxyLifetimeCoroutine(handle, lifetime, _epoch));

            if (Spawned != null) Spawned(handle);
        }

        internal void ReceiveProxyDespawn(int handleId, DespawnReason reason)
        {
            DespawnProxy(handleId, reason);
        }

        internal void ReceiveProxySnapshot(int handleId, Vector3 pos, Vector3 vel)
        {
            GameObject go;
            if (!_proxyGos.TryGetValue(handleId, out go)) return;
            var proxy = go.GetComponent<ProxyProjectile>();
            if (proxy != null) proxy.ApplySnapshot(pos, vel);
        }

        internal void ReconcileProxyAliveList(int aliveListFrame, HashSet<int> aliveIds)
        {
            var toRemove = new List<int>();
            foreach (var pair in _proxySpawnFrame)
            {
                int handleId = pair.Key;
                int sf       = pair.Value;
                if (sf < aliveListFrame && !aliveIds.Contains(handleId))
                    toRemove.Add(handleId);
            }
            foreach (int id in toRemove)
                DespawnProxy(id, DespawnReason.NetworkCorrection);
        }

        private void DespawnProxy(int handleId, DespawnReason reason)
        {
            GameObject go;
            if (!_proxyGos.TryGetValue(handleId, out go)) return;

            _proxyGos.Remove(handleId);
            _proxySpawnFrame.Remove(handleId);

            var proxy = go.GetComponent<ProxyProjectile>();
            if (proxy != null) proxy.ResetProxy();

            // 物理・衝突を元に戻してプールへ返す
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;
            var col = go.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            go.SetActive(false);

            string key;
            if (_proxyKey.TryGetValue(handleId, out key))
            {
                _proxyKey.Remove(handleId);
                Queue<GameObject> pool;
                if (_pools.TryGetValue(key, out pool))
                    pool.Enqueue(go);
            }

            if (Despawned != null) Despawned(new ProjectileHandle(handleId, 0), reason);
        }

        // ---- ユーティリティ ----

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

        private IEnumerator ProxyLifetimeCoroutine(ProjectileHandle handle, float lifetime, int epoch)
        {
            yield return new WaitForSeconds(lifetime);
            if (_epoch == epoch && _proxyGos.ContainsKey(handle.Id))
                DespawnProxy(handle.Id, DespawnReason.Timeout);
        }
    }
}

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
        // key ごとの上限弾数(同時生存数の上限)。枯渇時はこの上限を超えず最古を再利用する。
        private readonly Dictionary<string, int> _maxSizes =
            new Dictionary<string, int>();
        // key ごとの生成済みGO総数(プール内+生存中)。上限判定に使う。
        private readonly Dictionary<string, int> _totalCounts =
            new Dictionary<string, int>();
        // key ごとの弾体GO置き場(ProjectilePool の直下に並ぶ子コンテナ)。
        private readonly Dictionary<string, Transform> _poolRoots =
            new Dictionary<string, Transform>();
        // EnsureTypePool で生成した型別プールのキー一覧。OnSimulationStop で破棄対象を特定する。
        private readonly HashSet<string> _typePoolKeys = new HashSet<string>();

        // ホスト/SP: 生存弾
        private readonly Dictionary<int, GameObject> _active =
            new Dictionary<int, GameObject>();
        private readonly Dictionary<int, string> _activeKey =
            new Dictionary<int, string>();
        private readonly Dictionary<int, WeaponSpec> _activeSpec =
            new Dictionary<int, WeaponSpec>();
        // ホスト/SP: スポーン順のハンドルID。枯渇時に最古を強制リサイクルするために使う。
        private readonly List<int> _activeOrder = new List<int>();

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
        private IGameEventSource _events;
        // 全弾体GOの格納先。ACMUcore の子として生成されるため DontDestroyOnLoad 不要。
        private Transform _projectilePoolRoot;

        internal event Action<ProjectileHandle, Collision> ImpactOccurred;

        public event Action<ProjectileHandle> Spawned;
        public event Action<ProjectileHandle, DespawnReason> Despawned;

        public int InitOrder { get { return 300; } }

        internal void InitializeService(ILog log, IGameSessionInfo session, IGameEventSource events)
        {
            _log     = log;
            _session = session;
            _events  = events;

            var poolGo = new GameObject("[ACMu] ProjectilePool");
            poolGo.transform.SetParent(transform, false);
            _projectilePoolRoot = poolGo.transform;
        }

        public void OnModLoad()
        {
            if (_events != null)
                _events.LevelLoaded += CleanupTypePools;
        }

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

            _activeOrder.Clear();
            _epoch++;
        }

        // レベルロード時(シーンチェンジ)に呼ばれる。型別プール GO を破棄してメモリを解放する。
        // ベースプール(RegisterProjectile 由来)は維持。次シミュ開始時に EnsureTypePool で再生成される。
        private void CleanupTypePools()
        {
            foreach (string typeKey in _typePoolKeys)
            {
                Queue<GameObject> pool;
                if (_pools.TryGetValue(typeKey, out pool))
                    pool.Clear();
                Transform root;
                if (_poolRoots.TryGetValue(typeKey, out root) && root != null)
                    Destroy(root.gameObject);
                _pools.Remove(typeKey);
                _prefabs.Remove(typeKey);
                _maxSizes.Remove(typeKey);
                _totalCounts.Remove(typeKey);
                _poolRoots.Remove(typeKey);
            }
            _typePoolKeys.Clear();
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
            _maxSizes[key]    = prewarmCount;
            _totalCounts[key] = 0;

            var root = new GameObject("[ACMu] " + key);
            root.transform.SetParent(_projectilePoolRoot, false);
            _poolRoots[key] = root.transform;

            for (int i = 0; i < prewarmCount; i++)
            {
                var go = CreateProjectileGo(key, prefab);
                go.SetActive(false);
                pool.Enqueue(go);
            }
        }

        // 武装側(WeaponHostBehaviour 経由)がシミュ開始時に呼ぶ。ブロックタイプ別の弾体プールを確保する。
        // baseKey に登録済みのプレハブを流用し、typeKey 用のプールを生成・サイズ設定する。
        // 全ピアのシミュ開始時に同じ typeKey で呼ばれるため、MP プロキシ受信側も同じプールを引ける。
        // 既存 typeKey の場合は上限のみ引き上げる(同型ブロック複数設置に対応・下げはしない)。
        internal void EnsureTypePool(string baseKey, string typeKey, int maxSize)
        {
            if (string.IsNullOrEmpty(typeKey) || maxSize <= 0) return;

            if (_pools.ContainsKey(typeKey))
            {
                int cur;
                _maxSizes.TryGetValue(typeKey, out cur);
                if (maxSize > cur) _maxSizes[typeKey] = maxSize;
                return;
            }

            GameObject prefab;
            if (string.IsNullOrEmpty(baseKey) || !_prefabs.TryGetValue(baseKey, out prefab) || prefab == null)
            {
                if (_log != null) _log.Warn("[ACMu] EnsureTypePool: base prefab not found for key=" + baseKey);
                return;
            }

            _prefabs[typeKey]     = prefab;
            var pool              = new Queue<GameObject>();
            _pools[typeKey]       = pool;
            _maxSizes[typeKey]    = maxSize;
            _totalCounts[typeKey] = 0;

            var root = new GameObject("[ACMu] " + typeKey);
            root.transform.SetParent(_projectilePoolRoot, false);
            _poolRoots[typeKey] = root.transform;
            _typePoolKeys.Add(typeKey);

            // PoolSize 分を事前生成(原ACM同様、戦闘中のアロケーションを避ける)。
            for (int i = 0; i < maxSize; i++)
            {
                var go = CreateProjectileGo(typeKey, prefab);
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

            GameObject go = AcquirePooledGo(key, pool);
            if (go == null) return ProjectileHandle.Invalid;

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
            _activeOrder.Add(handle.Id);

            float lifetime = request.Shot.ProjectileLifetimeSeconds > 0f
                ? request.Shot.ProjectileLifetimeSeconds
                : DefaultLifetime;
            StartCoroutine(LifetimeCoroutine(handle, lifetime, _epoch));

            if (Spawned != null) Spawned(handle);

            return handle;
        }

        // 衝突通知のみ。Despawn は武装側の OnImpact が責任を持つ。
        // ProjectilesDespawnImmediately=false の弾体はここで消えず、フューズ/寿命で消える。
        internal void HandleImpact(ProjectileHandle handle, Collision collision)
        {
            if (!_active.ContainsKey(handle.Id)) return;
            if (ImpactOccurred != null) ImpactOccurred(handle, collision);
        }

        public void Despawn(ProjectileHandle handle, DespawnReason reason)
        {
            if (!handle.IsValid) return;

            GameObject go;
            if (!_active.TryGetValue(handle.Id, out go)) return;

            _active.Remove(handle.Id);
            _activeSpec.Remove(handle.Id);
            _activeOrder.Remove(handle.Id);
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
                // typeKey ("baseKey:blockTypeId") がクライアント未登録の場合、baseKey から自動生成を試みる。
                // 通常は OnSimulateStart で全ピアに登録済みのはずだが、タイミング差の保険として持つ。
                int colon = key.LastIndexOf(':');
                if (colon > 0)
                {
                    string baseKey = key.Substring(0, colon);
                    int fallbackSize;
                    _maxSizes.TryGetValue(baseKey, out fallbackSize);
                    if (fallbackSize <= 0) fallbackSize = 32;
                    EnsureTypePool(baseKey, key, fallbackSize);
                    _pools.TryGetValue(key, out pool);
                }
                if (pool == null)
                {
                    if (_log != null) _log.Warn("[ACMu] ProxySpawn: unknown key=" + key);
                    return;
                }
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

        // プールから弾体GOを取り出す。
        //  1) プールに空きGOがあれば再利用
        //  2) 上限(_maxSizes = PoolSize)未満なら新規生成してプールを成長させる
        //  3) 上限到達なら同キーの最古アクティブ弾を強制リサイクルしてそのGOを再利用する
        // 無限増殖はしない / 球フォールバックもしない / 発射は止めない。
        private GameObject AcquirePooledGo(string key, Queue<GameObject> pool)
        {
            if (pool.Count > 0)
                return pool.Dequeue();

            int total, maxSize;
            _totalCounts.TryGetValue(key, out total);
            _maxSizes.TryGetValue(key, out maxSize);

            // 上限未満: プールを成長させる(PoolSize に達するまで)
            if (total < maxSize)
            {
                GameObject prefab;
                _prefabs.TryGetValue(key, out prefab);
                return CreateProjectileGo(key, prefab);
            }

            // 上限到達(枯渇): 同キーの最古アクティブ弾を Despawn して GO をプールへ返し再利用
            if (RecycleOldestActive(key) && pool.Count > 0)
                return pool.Dequeue();

            // 同キーのアクティブが存在しない異常時のみ新規生成(保険)
            GameObject pf;
            _prefabs.TryGetValue(key, out pf);
            return CreateProjectileGo(key, pf);
        }

        // 同キーの最古アクティブ弾を1つ強制 Despawn する。見つかれば true。
        private bool RecycleOldestActive(string key)
        {
            for (int i = 0; i < _activeOrder.Count; i++)
            {
                int id = _activeOrder[i];
                string activeKey;
                if (_active.ContainsKey(id)
                    && _activeKey.TryGetValue(id, out activeKey)
                    && activeKey == key)
                {
                    Despawn(new ProjectileHandle(id, 0), DespawnReason.Manual);
                    return true;
                }
            }
            return false;
        }

        private GameObject CreateProjectileGo(string key, GameObject prefab)
        {
            var go = Instantiate(prefab);
            go.name = "[ACMu]Proj";
            Transform poolParent;
            _poolRoots.TryGetValue(key, out poolParent);
            go.transform.SetParent(poolParent != null ? poolParent : _projectilePoolRoot, false);
            var body = go.GetComponent<ProjectileBody>();
            if (body == null) body = go.AddComponent<ProjectileBody>();

            int total;
            _totalCounts.TryGetValue(key, out total);
            _totalCounts[key] = total + 1;
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

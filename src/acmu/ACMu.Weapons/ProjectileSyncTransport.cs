using System;
using System.Collections.Generic;
using ACMu.Core.Game;
using ACMu.Core.Lifecycle;
using ACMu.Core.Logging;
using ACMu.Core.Maths;
using ACMu.Core.Net;
using ACMu.Core.Weapons;
using UnityEngine;

namespace ACMu.Weapons
{
    /// <summary>
    /// M4 弾体 MP 同期トランスポート。
    /// ホストが Spawn/Despawn/StateSnapshot/AliveList を全クライアントへ送信する。
    /// クライアントは受信してプロキシ弾を表示し、発射時は発射要求をホストへ送信する。
    /// </summary>
    internal sealed class ProjectileSyncTransport : MonoBehaviour, ILifecycleParticipant
    {
        // コア予約チャネル(0〜31)内の弾体同期用割り当て
        private const byte ChReliable   = 1; // Spawn / Despawn / AliveList (ホスト→クライアント)
        private const byte ChUnreliable = 2; // StateSnapshot (ホスト→クライアント)
        private const byte ChFireReq    = 3; // 発射要求 (クライアント→ホスト)

        // ChReliable メッセージ種別
        private const byte MsgSpawn    = 1;
        private const byte MsgDespawn  = 2;
        private const byte MsgAliveList = 3;

        private const float AliveListIntervalSeconds = 1f;

        private ILog _log;
        private IGameSessionInfo _session;
        private INetworkTransport _net;
        private ProjectileService _projectiles;

        // ホスト側: 生存弾追跡とフレームカウンタ
        private readonly HashSet<int> _aliveIds = new HashSet<int>();
        private int _frameCounter;
        private float _lastSnapshotTime;
        private float _lastAliveListTime;

        // スナップショット送信用 (アロケーション削減のため事前確保)
        private readonly List<int>      _snapIds = new List<int>(64);
        private readonly List<Vector3d> _snapPos = new List<Vector3d>(64);
        private readonly List<Vector3>  _snapVel = new List<Vector3>(64);

        // クライアント側: 古いパケット破棄用
        private int _lastReceivedSnapshotFrame = -1;

        public int InitOrder { get { return 350; } } // ProjectileService(300) の後

        internal void InitializeService(
            ILog log,
            IGameSessionInfo session,
            INetworkTransport net,
            ProjectileService projectiles)
        {
            _log         = log;
            _session     = session;
            _net         = net;
            _projectiles = projectiles;
        }

        public void OnModLoad()
        {
            _net.Subscribe(ChReliable,   OnReliableReceived);
            _net.Subscribe(ChUnreliable, OnUnreliableReceived);
            _net.Subscribe(ChFireReq,    OnFireReqReceived);

            _projectiles.Spawned   += OnProjectileSpawned;
            _projectiles.Despawned += OnProjectileDespawned;
        }

        private void OnDestroy()
        {
            if (_projectiles != null)
            {
                _projectiles.Spawned   -= OnProjectileSpawned;
                _projectiles.Despawned -= OnProjectileDespawned;
            }
        }

        public void OnSimulationStart(bool isMultiplayer)
        {
            _frameCounter              = 0;
            _lastSnapshotTime          = 0f;
            _lastAliveListTime         = 0f;
            _lastReceivedSnapshotFrame = -1;
            _aliveIds.Clear();
        }

        public void OnSimulationStop()
        {
            _aliveIds.Clear();
        }

        // ---- クライアント→ホスト 発射要求 ----

        internal void SendFireRequest(
            string key, Vector3 position, Vector3 velocity,
            float lifetimeSeconds, float explosionRadius)
        {
            if (!_net.IsReady) return;

            var w = _net.CreateWriter();
            w.WriteString(key);
            w.WriteVector3(position);
            w.WriteVector3(velocity);
            w.WriteSingle(lifetimeSeconds);
            w.WriteSingle(explosionRadius);
            _net.Send(ChFireReq, w.ToArray(), NetTarget.Host, NetDelivery.Reliable);
        }

        private void OnFireReqReceived(int senderId, IPacketReader r)
        {
            if (!_session.IsHost) return;

            try
            {
                HandleFireRequest(r);
            }
            catch (Exception ex)
            {
                _log.Error("[ACMu] ProjectileSync fire req: " + ex.Message);
            }
        }

        private void HandleFireRequest(IPacketReader r)
        {
            string  key   = r.ReadString();
            Vector3 pos   = r.ReadVector3();
            Vector3 vel   = r.ReadVector3();
            float   life  = r.ReadSingle();
            float   explR = r.ReadSingle();

            Quaternion rot = vel.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(vel)
                : Quaternion.identity;

            var shot = new WeaponSpec();
            shot.ProjectileKey            = key;
            shot.ProjectileLifetimeSeconds = life;
            shot.ExplosionRadius          = explR;

            var request = new ProjectileSpawnRequest
            {
                ProjectileKey = key,
                Position      = pos,
                Rotation      = rot,
                Velocity      = vel,
                Shot          = shot
            };
            _projectiles.Spawn(request);
        }

        // ---- ホスト送信 ----

        void Update()
        {
            if (!_session.IsHost || !_net.IsReady) return;

            float now = Time.time;

            float snapInterval = _session.NetworkSendRate > 0f
                ? 1f / _session.NetworkSendRate
                : 0.05f;

            if (now - _lastSnapshotTime >= snapInterval)
            {
                SendSnapshot();
                _lastSnapshotTime = now;
            }

            if (now - _lastAliveListTime >= AliveListIntervalSeconds)
            {
                SendAliveList();
                _lastAliveListTime = now;
            }
        }

        private void OnProjectileSpawned(ProjectileHandle handle)
        {
            _aliveIds.Add(handle.Id);

            if (!_session.IsHost || !_net.IsReady) return;

            GameObject go;
            if (!_projectiles.TryGetGameObject(handle, out go)) return;

            var rb  = go.GetComponent<Rigidbody>();
            var pos = go.transform.position;
            var vel = rb != null ? rb.velocity : Vector3.zero;

            var spec    = _projectiles.GetSpawnSpec(handle.Id);
            string key  = spec != null && spec.ProjectileKey != null ? spec.ProjectileKey : "";
            float life  = spec != null ? spec.ProjectileLifetimeSeconds : 0f;
            float explR = spec != null ? spec.ExplosionRadius : 0f;

            var w = _net.CreateWriter();
            w.WriteByte(MsgSpawn);
            w.WriteInt32(_frameCounter);
            w.WriteInt32(handle.Id);
            w.WriteInt32(handle.OwnerPlayerId);
            w.WriteString(key);
            w.WriteVector3d(new Vector3d(pos.x, pos.y, pos.z));
            w.WriteVector3(vel);
            w.WriteSingle(life);
            w.WriteSingle(explR);
            _net.Send(ChReliable, w.ToArray(), NetTarget.Others, NetDelivery.Reliable);
        }

        private void OnProjectileDespawned(ProjectileHandle handle, DespawnReason reason)
        {
            _aliveIds.Remove(handle.Id);

            if (!_session.IsHost || !_net.IsReady) return;

            var w = _net.CreateWriter();
            w.WriteByte(MsgDespawn);
            w.WriteInt32(handle.Id);
            w.WriteByte((byte)reason);
            _net.Send(ChReliable, w.ToArray(), NetTarget.Others, NetDelivery.Reliable);
        }

        private void SendSnapshot()
        {
            if (_aliveIds.Count == 0) return;

            _frameCounter++;

            _snapIds.Clear();
            _snapPos.Clear();
            _snapVel.Clear();

            foreach (int id in _aliveIds)
            {
                GameObject go;
                if (!_projectiles.TryGetGameObject(new ProjectileHandle(id, 0), out go)) continue;
                var rb  = go.GetComponent<Rigidbody>();
                var pos = go.transform.position;
                var vel = rb != null ? rb.velocity : Vector3.zero;
                _snapIds.Add(id);
                _snapPos.Add(new Vector3d(pos.x, pos.y, pos.z));
                _snapVel.Add(vel);
            }

            if (_snapIds.Count == 0) return;

            var w = _net.CreateWriter();
            w.WriteInt32(_frameCounter);
            w.WriteInt32(_snapIds.Count);
            for (int i = 0; i < _snapIds.Count; i++)
            {
                w.WriteInt32(_snapIds[i]);
                w.WriteVector3d(_snapPos[i]);
                w.WriteVector3(_snapVel[i]);
            }
            _net.Send(ChUnreliable, w.ToArray(), NetTarget.Others, NetDelivery.Unreliable);
        }

        private void SendAliveList()
        {
            var w = _net.CreateWriter();
            w.WriteByte(MsgAliveList);
            w.WriteInt32(_frameCounter);
            w.WriteInt32(_aliveIds.Count);
            foreach (int id in _aliveIds)
                w.WriteInt32(id);
            _net.Send(ChReliable, w.ToArray(), NetTarget.Others, NetDelivery.Reliable);
        }

        // ---- クライアント受信 ----

        private void OnReliableReceived(int senderId, IPacketReader r)
        {
            try
            {
                byte msgType = r.ReadByte();
                switch (msgType)
                {
                    case MsgSpawn:    HandleSpawn(r);     break;
                    case MsgDespawn:  HandleDespawn(r);   break;
                    case MsgAliveList: HandleAliveList(r); break;
                    default:
                        _log.Warn("[ACMu] ProjectileSync: unknown msgType=" + msgType);
                        break;
                }
            }
            catch (Exception ex)
            {
                _log.Error("[ACMu] ProjectileSync reliable recv: " + ex.Message);
            }
        }

        private void OnUnreliableReceived(int senderId, IPacketReader r)
        {
            try
            {
                HandleSnapshot(r);
            }
            catch (Exception ex)
            {
                _log.Error("[ACMu] ProjectileSync snapshot recv: " + ex.Message);
            }
        }

        private void HandleSpawn(IPacketReader r)
        {
            int      spawnFrame = r.ReadInt32();
            int      handleId   = r.ReadInt32();
            int      ownerPId   = r.ReadInt32();
            string   key        = r.ReadString();
            Vector3d pos3d      = r.ReadVector3d();
            Vector3  vel        = r.ReadVector3();
            float    lifetime   = r.ReadSingle();
            float    explR      = r.ReadSingle();

            var pos = new Vector3((float)pos3d.x, (float)pos3d.y, (float)pos3d.z);
            _projectiles.ReceiveProxySpawn(
                handleId, ownerPId, key, pos, vel, lifetime, explR, spawnFrame);
        }

        private void HandleDespawn(IPacketReader r)
        {
            int           handleId = r.ReadInt32();
            DespawnReason reason   = (DespawnReason)r.ReadByte();
            _projectiles.ReceiveProxyDespawn(handleId, reason);
        }

        private void HandleSnapshot(IPacketReader r)
        {
            int frame = r.ReadInt32();
            if (frame <= _lastReceivedSnapshotFrame) return;
            _lastReceivedSnapshotFrame = frame;

            int count = r.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                int      handleId = r.ReadInt32();
                Vector3d pos3d    = r.ReadVector3d();
                Vector3  vel      = r.ReadVector3();
                var pos = new Vector3((float)pos3d.x, (float)pos3d.y, (float)pos3d.z);
                _projectiles.ReceiveProxySnapshot(handleId, pos, vel);
            }
        }

        private void HandleAliveList(IPacketReader r)
        {
            int frame = r.ReadInt32();
            int count = r.ReadInt32();
            var aliveIds = new HashSet<int>();
            for (int i = 0; i < count; i++)
                aliveIds.Add(r.ReadInt32());
            _projectiles.ReconcileProxyAliveList(frame, aliveIds);
        }
    }
}

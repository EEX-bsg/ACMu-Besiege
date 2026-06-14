using System;
using System.Collections.Generic;
using ACMu.Core.Game;
using ACMu.Core.Lifecycle;
using ACMu.Core.Logging;
using ACMu.Core.Net;
using Modding;
using Modding.Common;
using UnityEngine;

namespace ACMu.Net
{
    public class ModNetTransport : MonoBehaviour, INetworkTransport, ILifecycleParticipant
    {
        private const int MaxPayloadBytes = 8 * 1024;
        private const byte HelloChannel = 0;
        private const byte EchoChannel = 1; // M3テスト用 後で削除

        private ILog _log;
        private IGameEventSource _events;
        private int _apiVersion;

        private MessageType _msgType;
        private byte _nextChannel = 32;
        private readonly Dictionary<byte, List<NetMessageHandler>> _handlers =
            new Dictionary<byte, List<NetMessageHandler>>();

        public event Action<int> PeerJoined;
        public event Action<int> PeerLeft;

        public int InitOrder { get { return 100; } }
        public bool IsReady { get { return ModNetworking.IsNetworkingReady; } }

        internal void InitializeService(ILog log, IGameEventSource events, int apiVersion)
        {
            _log = log;
            _events = events;
            _apiVersion = apiVersion;
        }

        public void OnModLoad()
        {
            _msgType = ModNetworking.CreateMessageType(new[] { DataType.ByteArray });
            ModNetworking.MessageReceived += OnRawMessageReceived;
            _events.PlayerJoined += OnPlayerJoined;
            _events.PlayerLeft  += OnPlayerLeft;
        }

        private void OnDestroy()
        {
            if (_events != null)
            {
                _events.PlayerJoined -= OnPlayerJoined;
                _events.PlayerLeft  -= OnPlayerLeft;
            }
            ModNetworking.MessageReceived -= OnRawMessageReceived;
        }

        public void OnSimulationStart(bool isMultiplayer) { }
        public void OnSimulationStop() { }

        public byte AllocateChannel(string ownerName)
        {
            if (_nextChannel >= 255)
                throw new InvalidOperationException("[ACMu] Channel pool exhausted");
            byte ch = _nextChannel++;
            _log.Info("[ACMu] Channel " + ch + " allocated: " + ownerName);
            return ch;
        }

        public void Subscribe(byte channelId, NetMessageHandler handler)
        {
            List<NetMessageHandler> list;
            if (!_handlers.TryGetValue(channelId, out list))
            {
                list = new List<NetMessageHandler>();
                _handlers[channelId] = list;
            }
            list.Add(handler);
        }

        public void Send(byte channelId, byte[] payload, NetTarget target, NetDelivery delivery)
        {
            if (!ModNetworking.IsNetworkingReady)
            {
                _log.Warn("[ACMu] Network not ready, dropped ch=" + channelId);
                return;
            }
            if (payload.Length + 1 > MaxPayloadBytes)
                throw new InvalidOperationException("[ACMu] Payload exceeds 8KB on ch=" + channelId);

            var msg = _msgType.CreateMessage(new object[] { Frame(channelId, payload) });
            switch (target)
            {
                case NetTarget.All:
                    ModNetworking.SendToAll(msg);
                    break;
                case NetTarget.Others:
                    SendToAllOthers(msg);
                    break;
                case NetTarget.Host:
                    ModNetworking.SendToHost(msg);
                    break;
            }
        }

        public void SendToPlayer(byte channelId, byte[] payload, int playerId, NetDelivery delivery)
        {
            if (!ModNetworking.IsNetworkingReady)
            {
                _log.Warn("[ACMu] Network not ready, dropped ch=" + channelId);
                return;
            }
            if (payload.Length + 1 > MaxPayloadBytes)
                throw new InvalidOperationException("[ACMu] Payload exceeds 8KB on ch=" + channelId);

            var player = Player.From((ushort)playerId);
            if (player == null)
            {
                _log.Warn("[ACMu] SendToPlayer: player " + playerId + " not found");
                return;
            }
            var msg = _msgType.CreateMessage(new object[] { Frame(channelId, payload) });
            ModNetworking.SendTo(player, msg);
        }

        public IPacketWriter CreateWriter() { return new PacketWriterImpl(); }
        public IPacketReader CreateReader(byte[] payload) { return new PacketReaderImpl(payload); }

        private void OnRawMessageReceived(Message message)
        {
            if (message == null || message.Type == null) return;
            if (message.Type.ID != _msgType.ID) return;

            byte[] framed = message.GetData(0) as byte[];
            if (framed == null || framed.Length < 1) return;

            byte channelId = framed[0];
            byte[] payload = new byte[framed.Length - 1];
            if (payload.Length > 0)
                Buffer.BlockCopy(framed, 1, payload, 0, payload.Length);

            int senderId = (int)message.Sender.NetworkId;

            if (channelId == HelloChannel) { HandleHello(senderId, payload); return; }
            if (channelId == EchoChannel)  { HandleEcho(senderId, payload); return; }

            List<NetMessageHandler> handlers;
            if (!_handlers.TryGetValue(channelId, out handlers)) return;

            foreach (NetMessageHandler handler in handlers)
            {
                try { handler(senderId, new PacketReaderImpl(payload)); }
                catch (Exception ex)
                {
                    _log.Error("[ACMu] NetHandler ch=" + channelId + ": " + ex.Message);
                }
            }
        }

        private void OnPlayerJoined(int playerId)
        {
            var local = Player.GetLocalPlayer();
            if (local != null && (int)local.NetworkId == playerId) return;

            var e = PeerJoined;
            if (e != null) e(playerId);

            SendHello(playerId);
            SendEchoPing(playerId); // M3テスト用 後で削除
        }

        private void OnPlayerLeft(int playerId)
        {
            var local = Player.GetLocalPlayer();
            if (local != null && (int)local.NetworkId == playerId) return;

            var e = PeerLeft;
            if (e != null) e(playerId);
        }

        // ---- チャネル0: バージョンhello ----

        private void SendHello(int peerId)
        {
            if (!ModNetworking.IsNetworkingReady) return;
            var w = new PacketWriterImpl();
            w.WriteInt32(_apiVersion);
            SendToPlayer(HelloChannel, w.ToArray(), peerId, NetDelivery.Reliable);
        }

        private void HandleHello(int senderId, byte[] payload)
        {
            try
            {
                int remoteVer = new PacketReaderImpl(payload).ReadInt32();
                if (remoteVer != _apiVersion)
                    _log.Warn("[ACMu] ApiVersion mismatch local=" + _apiVersion + " remote=" + remoteVer + " peer=" + senderId);
                else
                    _log.Info("[ACMu] Hello ok peer=" + senderId + " apiVersion=" + remoteVer);
            }
            catch (Exception ex)
            {
                _log.Error("[ACMu] Hello parse error from peer=" + senderId + ": " + ex.Message);
            }
        }

        // ---- チャネル1: エコーテスト (M3テスト用 後で削除) ----

        private void SendEchoPing(int peerId)
        {
            if (!ModNetworking.IsNetworkingReady) return;
            var w = new PacketWriterImpl();
            w.WriteByte(0); // isResponse=false
            w.WriteString("ACMU_ECHO_PING");
            SendToPlayer(EchoChannel, w.ToArray(), peerId, NetDelivery.Reliable);
        }

        private void HandleEcho(int senderId, byte[] payload)
        {
            try
            {
                var r = new PacketReaderImpl(payload);
                byte isResponse = r.ReadByte();
                string text = r.ReadString();
                _log.Info("[ACMu] Echo ch1 from=" + senderId + " isResponse=" + isResponse + " msg=" + text);
                if (isResponse == 0)
                {
                    // ピンに対してエコーバック
                    var w = new PacketWriterImpl();
                    w.WriteByte(1); // isResponse=true
                    w.WriteString(text);
                    SendToPlayer(EchoChannel, w.ToArray(), senderId, NetDelivery.Reliable);
                }
            }
            catch (Exception ex)
            {
                _log.Error("[ACMu] Echo parse error from=" + senderId + ": " + ex.Message);
            }
        }

        // ---- ユーティリティ ----

        private static byte[] Frame(byte channelId, byte[] payload)
        {
            byte[] framed = new byte[1 + payload.Length];
            framed[0] = channelId;
            if (payload.Length > 0)
                Buffer.BlockCopy(payload, 0, framed, 1, payload.Length);
            return framed;
        }

        private void SendToAllOthers(Message msg)
        {
            var local = Player.GetLocalPlayer();
            var all = Player.GetAllPlayers();
            foreach (Player p in all)
            {
                if (local != null && p.NetworkId == local.NetworkId) continue;
                ModNetworking.SendTo(p, msg);
            }
        }
    }
}

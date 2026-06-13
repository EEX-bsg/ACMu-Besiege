using System;
using ACMu.Core.Net;
using UnityEngine;

namespace ACMu.Host.Null
{
    internal class NullNetworkTransport : INetworkTransport
    {
        private byte _nextChannel = 32;

        public bool IsReady { get { return false; } }

        public event Action<int> PeerJoined { add { } remove { } }
        public event Action<int> PeerLeft { add { } remove { } }

        public byte AllocateChannel(string ownerName)
        {
            return _nextChannel++;
        }

        public void Subscribe(byte channelId, NetMessageHandler handler) { }

        public void Send(byte channelId, byte[] payload, NetTarget target, NetDelivery delivery)
        {
            Debug.LogWarning("[ACMu] Network N/A (M3): Send dropped ch=" + channelId);
        }

        public void SendToPlayer(byte channelId, byte[] payload, int playerId, NetDelivery delivery)
        {
            Debug.LogWarning("[ACMu] Network N/A (M3): SendToPlayer dropped ch=" + channelId);
        }

        public IPacketWriter CreateWriter()
        {
            return new NullPacketWriter();
        }

        public IPacketReader CreateReader(byte[] payload)
        {
            return new NullPacketReader();
        }

        private class NullPacketWriter : IPacketWriter
        {
            public void WriteBool(bool value) { }
            public void WriteByte(byte value) { }
            public void WriteInt16(short value) { }
            public void WriteInt32(int value) { }
            public void WriteSingle(float value) { }
            public void WriteDouble(double value) { }
            public void WriteString(string value) { }
            public void WriteVector3(UnityEngine.Vector3 value) { }
            public void WriteVector3d(ACMu.Core.Maths.Vector3d value) { }
            public void WriteQuaternion(UnityEngine.Quaternion value) { }
            public void WriteBytes(byte[] value) { }
            public byte[] ToArray() { return new byte[0]; }
        }

        private class NullPacketReader : IPacketReader
        {
            public int Remaining { get { return 0; } }
            public bool ReadBool() { return false; }
            public byte ReadByte() { return 0; }
            public short ReadInt16() { return 0; }
            public int ReadInt32() { return 0; }
            public float ReadSingle() { return 0f; }
            public double ReadDouble() { return 0.0; }
            public string ReadString() { return string.Empty; }
            public UnityEngine.Vector3 ReadVector3() { return UnityEngine.Vector3.zero; }
            public ACMu.Core.Maths.Vector3d ReadVector3d() { return ACMu.Core.Maths.Vector3d.Zero; }
            public UnityEngine.Quaternion ReadQuaternion() { return UnityEngine.Quaternion.identity; }
            public byte[] ReadBytes() { return new byte[0]; }
        }
    }
}

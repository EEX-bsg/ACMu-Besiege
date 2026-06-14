using System.IO;
using System.Text;
using ACMu.Core.Maths;
using ACMu.Core.Net;
using UnityEngine;

namespace ACMu.Net
{
    internal sealed class PacketWriterImpl : IPacketWriter
    {
        private readonly MemoryStream _ms = new MemoryStream();
        private readonly BinaryWriter _bw;

        internal PacketWriterImpl()
        {
            _bw = new BinaryWriter(_ms);
        }

        public void WriteBool(bool value)    { _bw.Write(value); }
        public void WriteByte(byte value)    { _bw.Write(value); }
        public void WriteInt16(short value)  { _bw.Write(value); }
        public void WriteInt32(int value)    { _bw.Write(value); }
        public void WriteSingle(float value) { _bw.Write(value); }
        public void WriteDouble(double value){ _bw.Write(value); }

        public void WriteString(string value)
        {
            byte[] b = Encoding.UTF8.GetBytes(value ?? string.Empty);
            _bw.Write(b.Length);
            _bw.Write(b);
        }

        public void WriteVector3(Vector3 v)
        {
            _bw.Write(v.x);
            _bw.Write(v.y);
            _bw.Write(v.z);
        }

        public void WriteVector3d(Vector3d v)
        {
            _bw.Write(v.x);
            _bw.Write(v.y);
            _bw.Write(v.z);
        }

        public void WriteQuaternion(Quaternion q)
        {
            _bw.Write(q.x);
            _bw.Write(q.y);
            _bw.Write(q.z);
            _bw.Write(q.w);
        }

        public void WriteBytes(byte[] value)
        {
            if (value == null) { _bw.Write(0); return; }
            _bw.Write(value.Length);
            _bw.Write(value);
        }

        public byte[] ToArray()
        {
            _bw.Flush();
            return _ms.ToArray();
        }
    }
}

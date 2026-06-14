using System.IO;
using System.Text;
using ACMu.Core.Maths;
using ACMu.Core.Net;
using UnityEngine;

namespace ACMu.Net
{
    internal sealed class PacketReaderImpl : IPacketReader
    {
        private readonly MemoryStream _ms;
        private readonly BinaryReader _br;

        internal PacketReaderImpl(byte[] payload)
        {
            _ms = new MemoryStream(payload, false);
            _br = new BinaryReader(_ms);
        }

        public int Remaining { get { return (int)(_ms.Length - _ms.Position); } }

        public bool ReadBool()    { return _br.ReadBoolean(); }
        public byte ReadByte()    { return _br.ReadByte(); }
        public short ReadInt16()  { return _br.ReadInt16(); }
        public int ReadInt32()    { return _br.ReadInt32(); }
        public float ReadSingle() { return _br.ReadSingle(); }
        public double ReadDouble(){ return _br.ReadDouble(); }

        public string ReadString()
        {
            int len = _br.ReadInt32();
            byte[] b = _br.ReadBytes(len);
            return Encoding.UTF8.GetString(b);
        }

        public Vector3 ReadVector3()
        {
            return new Vector3(_br.ReadSingle(), _br.ReadSingle(), _br.ReadSingle());
        }

        public Vector3d ReadVector3d()
        {
            return new Vector3d(_br.ReadDouble(), _br.ReadDouble(), _br.ReadDouble());
        }

        public Quaternion ReadQuaternion()
        {
            float x = _br.ReadSingle();
            float y = _br.ReadSingle();
            float z = _br.ReadSingle();
            float w = _br.ReadSingle();
            return new Quaternion(x, y, z, w);
        }

        public byte[] ReadBytes()
        {
            int len = _br.ReadInt32();
            return _br.ReadBytes(len);
        }
    }
}

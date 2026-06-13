using UnityEngine;
using ACMu.Core.Maths;

namespace ACMu.Core.Net
{
    /// <summary>
    /// ペイロード直列化の契約。エンディアン・圧縮表現は実装が決め、IPacketReader と対称であることのみを保証する。
    /// <para>不変条件: Write 系の呼び出し順序と Read 系の呼び出し順序が一致すれば元の値が復元される。
    /// ToArray 呼び出し後の追記は禁止。</para>
    /// <para>呼び出しタイミング: 送信処理の中で生成し、使い捨てる(フレームをまたいで保持しない)。</para>
    /// <para>スレッド制約: Unity メインスレッドのみ。</para>
    /// </summary>
    public interface IPacketWriter
    {
        void WriteBool(bool value);
        void WriteByte(byte value);
        void WriteInt16(short value);
        void WriteInt32(int value);
        void WriteSingle(float value);
        void WriteDouble(double value);
        void WriteString(string value);
        void WriteVector3(Vector3 value);
        void WriteVector3d(Vector3d value);
        void WriteQuaternion(Quaternion value);
        void WriteBytes(byte[] value);
        byte[] ToArray();
    }
}

using UnityEngine;
using ACMu.Core.Maths;

namespace ACMu.Core.Net
{
    /// <summary>
    /// ペイロード逆直列化の契約。IPacketWriter と対称。
    /// <para>不変条件: ペイロード末尾を越える Read は例外を送出する(呼び出し側はメッセージ単位で捕捉し、当該メッセージを破棄する)。</para>
    /// <para>呼び出しタイミング: 受信ハンドラ内で使い捨てる(フレームをまたいで保持しない)。</para>
    /// <para>スレッド制約: Unity メインスレッドのみ。</para>
    /// </summary>
    public interface IPacketReader
    {
        bool ReadBool();
        byte ReadByte();
        short ReadInt16();
        int ReadInt32();
        float ReadSingle();
        double ReadDouble();
        string ReadString();
        Vector3 ReadVector3();
        Vector3d ReadVector3d();
        Quaternion ReadQuaternion();
        byte[] ReadBytes();

        /// <summary>未読バイト数。</summary>
        int Remaining { get; }
    }
}

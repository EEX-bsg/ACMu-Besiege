namespace ACMu.Core.Net
{
    /// <summary>
    /// ネットワーク上を流れるデータ構造が実装する直列化契約。
    /// <para>不変条件: Serialize → Deserialize の往復で観測可能な状態が一致すること。
    /// バージョン互換が必要な構造は先頭にスキーマバージョン(byte)を自ら書き込むこと。</para>
    /// <para>スレッド制約: Unity メインスレッドのみ。</para>
    /// </summary>
    public interface INetSerializable
    {
        void Serialize(IPacketWriter writer);
        void Deserialize(IPacketReader reader);
    }
}

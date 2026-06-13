namespace ACMu.Core.Net
{
    /// <summary>受信ハンドラ。senderPlayerId は送信元プレイヤーID(ホスト送信時は 0)。</summary>
    public delegate void NetMessageHandler(int senderPlayerId, IPacketReader payload);
}

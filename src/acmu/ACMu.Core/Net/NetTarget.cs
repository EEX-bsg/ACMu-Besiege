namespace ACMu.Core.Net
{
    /// <summary>送信先の指定。</summary>
    public enum NetTarget : byte
    {
        /// <summary>ホストのみへ送る。自分がホストの場合はローカル配送される。</summary>
        Host = 0,
        /// <summary>全プレイヤー(自分を含む)。</summary>
        All = 1,
        /// <summary>自分以外の全プレイヤー。</summary>
        Others = 2
    }
}

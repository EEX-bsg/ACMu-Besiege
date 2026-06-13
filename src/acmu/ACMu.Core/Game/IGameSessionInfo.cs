namespace ACMu.Core.Game
{
    /// <summary>
    /// セッション状態(本体 StatMaster 等)の読み取り専用射影。本体 public 署名への依存を Adapter 層へ隔離するための契約。
    /// <para>不変条件: 各プロパティは副作用を持たない。シングルプレイ時は IsHost = true / IsClient = false を返す。</para>
    /// <para>呼び出しタイミング: 毎フレーム参照可(実装は軽量であること)。</para>
    /// <para>スレッド制約: Unity メインスレッドのみ。</para>
    /// </summary>
    public interface IGameSessionInfo
    {
        bool IsMultiplayer { get; }
        bool IsHost { get; }
        bool IsClient { get; }
        bool IsSimulating { get; }

        /// <summary>ローカルプレイヤーのネットワークID。非MP時は 0。</summary>
        int LocalPlayerId { get; }

        /// <summary>本体設定の送信レート(回/秒)。周期同期はこの値に従うこと。</summary>
        float NetworkSendRate { get; }
    }
}

using System;

namespace ACMu.Core.Net
{
    /// <summary>
    /// 通信境界の契約。下位は ModNetworking を想定するが、再送・順序保証・補間・圧縮などの内部設計は
    /// 本契約の関知外であり別セッションで設計する。利用側はチャネルIDとペイロード(byte[])のみを扱う。
    /// <para>不変条件:
    /// (1) チャネルID 0〜31 は ACMu コア予約。プラグインは AllocateChannel で取得したIDのみ使用する。
    /// (2) 同一チャネル・NetDelivery.Reliable のメッセージは送信順に配送される(チャネル間の順序は保証しない)。
    /// (3) ペイロード上限は 8KB(仮定。超過時は Send が例外を送出)。
    /// (4) IsReady = false の間の Send は黙って破棄されログに記録される(例外にしない)。</para>
    /// <para>呼び出しタイミング: AllocateChannel / Subscribe は OnModLoad 中のみ。Send はシミュレーション中の任意フレーム。</para>
    /// <para>スレッド制約: 全メンバーは Unity メインスレッドのみ。受信ハンドラもメインスレッドで呼ばれる。</para>
    /// </summary>
    public interface INetworkTransport
    {
        bool IsReady { get; }

        /// <summary>プラグイン用チャネルIDを払い出す(32〜254)。ownerName は衝突診断ログ用。</summary>
        byte AllocateChannel(string ownerName);

        /// <summary>チャネルへの受信ハンドラ登録。1チャネルに複数ハンドラ可。</summary>
        void Subscribe(byte channelId, NetMessageHandler handler);

        void Send(byte channelId, byte[] payload, NetTarget target, NetDelivery delivery);
        void SendToPlayer(byte channelId, byte[] payload, int playerId, NetDelivery delivery);

        IPacketWriter CreateWriter();
        IPacketReader CreateReader(byte[] payload);

        event Action<int> PeerJoined;
        event Action<int> PeerLeft;
    }
}

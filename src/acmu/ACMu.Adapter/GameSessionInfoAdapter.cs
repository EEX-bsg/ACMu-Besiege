using UnityEngine;
using ACMu.Core.Game;
using ACMu.Core.Lifecycle;

namespace ACMu.Adapter
{
    public class GameSessionInfoAdapter : MonoBehaviour, IGameSessionInfo, ILifecycleParticipant
    {
        public int InitOrder { get { return 0; } }

        public bool IsMultiplayer { get { return StatMaster.isMP; } }
        public bool IsHost { get { return !StatMaster.isMP || StatMaster.isHosting; } }
        public bool IsClient { get { return StatMaster.isMP && StatMaster.isClient; } }
        // 自マシンのシミュ実行権威。levelSimulating(=本体 isSimulating)を必須とし、
        // MP では「ホスト」または「ローカルシミュ実行中」のみ権威を持つ。
        // levelSimulating は観戦中クライアント(ホストの global シミュを見ているだけ)でも true になるため、
        // これ単体では権威判定にならない(契約 IGameSessionInfo.IsSimulating の不変条件)。
        public bool IsSimulating
        {
            get
            {
                if (!StatMaster.levelSimulating) return false; // シミュ中が必須
                if (!StatMaster.isMP) return true;             // SP は無条件で権威
                return StatMaster.isHosting || Modding.Game.IsSimulatingLocal; // MP: ホスト or ローカルシミュ中
            }
        }
        public int LocalPlayerId
        {
            get
            {
                var p = Modding.Common.Player.GetLocalPlayer();
                return p != null ? (int)p.NetworkId : 0;
            }
        }
        public float NetworkSendRate { get { return 20f; } }

        // GodMode の弾薬無限チート。Compat 層の GameRulesRegistry から委譲される。
        internal static bool IsInfiniteAmmoMode()
        {
            return StatMaster.GodTools.InfiniteAmmoMode;
        }

        public void OnModLoad() { }
        public void OnSimulationStart(bool isMultiplayer) { }
        public void OnSimulationStop() { }
    }
}

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
        public bool IsSimulating { get { return StatMaster.levelSimulating; } }
        public int LocalPlayerId { get { return 0; } }
        public float NetworkSendRate { get { return 20f; } }

        public void OnModLoad() { }
        public void OnSimulationStart(bool isMultiplayer) { }
        public void OnSimulationStop() { }
    }
}

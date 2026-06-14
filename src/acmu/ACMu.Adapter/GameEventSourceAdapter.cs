using System;
using UnityEngine;
using Modding;
using Modding.Blocks;
using Modding.Common;
using Modding.Levels;
using ACMu.Core.Game;
using ACMu.Core.Lifecycle;

namespace ACMu.Adapter
{
    public class GameEventSourceAdapter : MonoBehaviour, IGameEventSource, ILifecycleParticipant
    {
        public int InitOrder { get { return 0; } }

        public event Action<bool> SimulationToggled;
        public event Action<IBlockAccessor> BlockInitialized;
        public event Action<int> PlayerJoined;
        public event Action<int> PlayerLeft;
        public event Action LevelLoaded;

        public void OnModLoad()
        {
            Events.OnSimulationToggle += HandleSimulationToggle;
            Events.OnBlockInit += HandleBlockInit;
            Events.OnPlayerJoin += HandlePlayerJoin;
            Events.OnPlayerLeave += HandlePlayerLeave;
            Events.OnLevelLoaded += HandleLevelLoaded;
        }

        private void OnDestroy()
        {
            Events.OnSimulationToggle -= HandleSimulationToggle;
            Events.OnBlockInit -= HandleBlockInit;
            Events.OnPlayerJoin -= HandlePlayerJoin;
            Events.OnPlayerLeave -= HandlePlayerLeave;
            Events.OnLevelLoaded -= HandleLevelLoaded;
        }

        private void HandleSimulationToggle(bool started)
        {
            var e = SimulationToggled;
            if (e == null) return;
            foreach (Delegate d in e.GetInvocationList())
            {
                try { ((Action<bool>)d)(started); }
                catch (Exception ex) { Debug.LogError("[ACMu] SimulationToggled handler threw: " + ex.Message); }
            }
        }

        private void HandleBlockInit(Block block)
        {
            var e = BlockInitialized;
            if (e == null) return;
            var accessor = new BlockAccessorAdapter(block.InternalObject);
            foreach (Delegate d in e.GetInvocationList())
            {
                try { ((Action<IBlockAccessor>)d)(accessor); }
                catch (Exception ex) { Debug.LogError("[ACMu] BlockInitialized handler threw: " + ex.Message); }
            }
        }

        private void HandlePlayerJoin(Player p)
        {
            var e = PlayerJoined;
            if (e == null) return;
            int id = (int)p.NetworkId;
            foreach (Delegate d in e.GetInvocationList())
            {
                try { ((Action<int>)d)(id); }
                catch (Exception ex) { Debug.LogError("[ACMu] PlayerJoined handler threw: " + ex.Message); }
            }
        }

        private void HandlePlayerLeave(Player p)
        {
            var e = PlayerLeft;
            if (e == null) return;
            int id = (int)p.NetworkId;
            foreach (Delegate d in e.GetInvocationList())
            {
                try { ((Action<int>)d)(id); }
                catch (Exception ex) { Debug.LogError("[ACMu] PlayerLeft handler threw: " + ex.Message); }
            }
        }

        private void HandleLevelLoaded(Level level)
        {
            var e = LevelLoaded;
            if (e == null) return;
            foreach (Delegate d in e.GetInvocationList())
            {
                try { ((Action)d)(); }
                catch (Exception ex) { Debug.LogError("[ACMu] LevelLoaded handler threw: " + ex.Message); }
            }
        }

        public void OnSimulationStart(bool isMultiplayer) { }
        public void OnSimulationStop() { }
    }
}

using System;
using System.Collections.Generic;
using ACMu.Core.Game;
using ACMu.Core.Lifecycle;
using ACMu.Core.Logging;
using UnityEngine;

namespace ACMu.Host
{
    public class LifecycleCoordinator : MonoBehaviour
    {
        private readonly List<ILifecycleParticipant> _participants = new List<ILifecycleParticipant>();
        private IGameEventSource _gameEvents;
        private IGameSessionInfo _session;
        private ILog _log;

        internal void Initialize(IGameEventSource gameEvents, IGameSessionInfo session, ILog log)
        {
            _gameEvents = gameEvents;
            _session = session;
            _log = log;
            _gameEvents.SimulationToggled += OnSimulationToggled;
        }

        internal void AddParticipant(ILifecycleParticipant participant)
        {
            _participants.Add(participant);
        }

        internal void SortAndBootstrap()
        {
            _participants.Sort((a, b) => a.InitOrder.CompareTo(b.InitOrder));
            foreach (ILifecycleParticipant p in _participants)
            {
                try { p.OnModLoad(); }
                catch (Exception ex) { _log.Error("OnModLoad failed for " + p.GetType().Name, ex); }
            }
        }

        private void OnSimulationToggled(bool isSimulating)
        {
            bool isMP = _session.IsMultiplayer;
            if (isSimulating)
            {
                foreach (ILifecycleParticipant p in _participants)
                {
                    try { p.OnSimulationStart(isMP); }
                    catch (Exception ex) { _log.Error("OnSimulationStart failed for " + p.GetType().Name, ex); }
                }
            }
            else
            {
                for (int i = _participants.Count - 1; i >= 0; i--)
                {
                    try { _participants[i].OnSimulationStop(); }
                    catch (Exception ex) { _log.Error("OnSimulationStop failed for " + _participants[i].GetType().Name, ex); }
                }
            }
        }

        private void OnDestroy()
        {
            if (_gameEvents != null)
                _gameEvents.SimulationToggled -= OnSimulationToggled;
        }
    }
}

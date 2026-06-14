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
            for (int i = 0; i < _participants.Count; i++)
            {
                try { _participants[i].OnModLoad(); }
                catch (Exception ex) { _log.Error("OnModLoad failed (participant index " + i + ")", ex); }
            }
            _log.Info("ACMu loaded (" + _participants.Count + " participants)"); // DEBUG
        }

        private void OnSimulationToggled(bool isSimulating)
        {
            bool isMP = _session.IsMultiplayer;
            if (isSimulating)
            {
                string mode = isMP ? (_session.IsHost ? "MP/Host" : "MP/Client") : "SP"; // DEBUG
                _log.Info("Simulation started [" + mode + "]"); // DEBUG
                for (int i = 0; i < _participants.Count; i++)
                {
                    try { _participants[i].OnSimulationStart(isMP); }
                    catch (Exception ex) { _log.Error("OnSimulationStart failed (participant index " + i + ")", ex); }
                }
            }
            else
            {
                _log.Info("Simulation stopped"); // DEBUG
                for (int i = _participants.Count - 1; i >= 0; i--)
                {
                    try { _participants[i].OnSimulationStop(); }
                    catch (Exception ex) { _log.Error("OnSimulationStop failed (participant index " + i + ")", ex); }
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

using ACMu.Adapter;
using ACMu.Core;
using UnityEngine;

namespace ACMu.Host
{
    internal static class AcmuCoreBootstrap
    {
        internal static void Initialize()
        {
            var go = new GameObject(WellKnownNames.CoreObjectName);
            Object.DontDestroyOnLoad(go);

            var log = go.AddComponent<ConsoleLog>();
            var session = go.AddComponent<GameSessionInfoAdapter>();
            var events = go.AddComponent<GameEventSourceAdapter>();
            var config = go.AddComponent<ModIoConfigStore>();

            var services = go.AddComponent<AcmuServicesComponent>();
            services.Initialize(log, session, events, config);

            var coordinator = go.AddComponent<LifecycleCoordinator>();
            coordinator.Initialize(events, session, log);
            coordinator.AddParticipant(log);
            coordinator.AddParticipant(session);
            coordinator.AddParticipant(events);
            coordinator.AddParticipant(config);

            coordinator.SortAndBootstrap();
        }
    }
}

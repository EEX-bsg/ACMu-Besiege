using ACMu.Core;
using ACMu.Core.Config;
using ACMu.Core.Game;
using ACMu.Core.Logging;
using ACMu.Core.Net;
using ACMu.Core.Weapons;
using ACMu.Core.World;
using ACMu.Host.Null;
using ACMu.PluginApi;
using UnityEngine;

namespace ACMu.Host
{
    public class AcmuServicesComponent : MonoBehaviour, IAcmuServices, IAcmuPluginHost
    {
        private ILog _log;
        private IGameSessionInfo _session;
        private IGameEventSource _gameEvents;
        private IBlockAccessorFactory _blocks;
        private IConfigStore _config;
        private INetworkTransport _network;
        private IWorldFrame _world;
        private IProjectileService _projectiles;
        private IWeaponRegistry _weapons;

        public int ApiVersion { get { return 1; } }
        public IAcmuServices Services { get { return this; } }
        public IWeaponRegistry Weapons { get { return _weapons; } }

        public ILog Log { get { return _log; } }
        public IGameSessionInfo Session { get { return _session; } }
        public IGameEventSource GameEvents { get { return _gameEvents; } }
        public IBlockAccessorFactory Blocks { get { return _blocks; } }
        public IConfigStore Config { get { return _config; } }
        public INetworkTransport Network { get { return _network; } }
        public IWorldFrame World { get { return _world; } }
        public IProjectileService Projectiles { get { return _projectiles; } }

        internal void Initialize(
            ILog log,
            IGameSessionInfo session,
            IGameEventSource gameEvents,
            IBlockAccessorFactory blocks,
            IConfigStore config)
        {
            _log = log;
            _session = session;
            _gameEvents = gameEvents;
            _blocks = blocks;
            _config = config;
            _network = new NullNetworkTransport();
            _world = new NullWorldFrame();
            _projectiles = new NullProjectileService();
            _weapons = new NullWeaponRegistry();
        }
    }
}

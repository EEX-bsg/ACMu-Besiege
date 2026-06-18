using System;
using ACMu.Core;
using ACMu.Core.Game;
using ACMu.Core.Logging;
using ACMu.Core.Weapons;
using ACMu.PluginApi;
using Modding.Modules;
using UnityEngine;

namespace ACMu.Weapons
{
    public class WeaponHostBehaviour<TModule> : BlockModuleBehaviour<TModule>, IWeaponHost
        where TModule : BlockModule, new()
    {
        private IAcmuServices _services;
        private IBlockAccessor _block;
        private WeaponComponentBase _weapon;
        private WeaponSpec _baseSpec;
        private FirePipeline _pipeline;
        private ProjectileService _projectileService;
        private ProjectileSyncTransport _projSync;

        // IWeaponHost
        public IBlockAccessor Block { get { return _block; } }

        public ILog Log
        {
            get { return _services != null ? _services.Log : null; }
        }

        public IProjectileService Projectiles
        {
            get { return _services != null ? _services.Projectiles : null; }
        }

        public WeaponSpec BaseSpec { get { return _baseSpec; } }

        public bool IsAuthority
        {
            get { return _services != null && _services.Session.IsSimulating; }
        }

        public virtual Vector3 MuzzlePosition { get { return transform.position; } }
        public virtual Quaternion MuzzleRotation { get { return transform.rotation; } }

        public void RequestFire()
        {
            if (_pipeline != null) _pipeline.RequestFire();
        }

        public override void SafeAwake()
        {
            base.SafeAwake();
            var core = GameObject.Find(WellKnownNames.CoreObjectName);
            if (core == null)
            {
                Debug.LogError("[ACMu] WeaponHostBehaviour: ACMUcore not found");
                return;
            }
            var pluginHost = core.GetComponent<IAcmuPluginHost>();
            if (pluginHost == null)
            {
                Debug.LogError("[ACMu] WeaponHostBehaviour: IAcmuPluginHost not found");
                return;
            }
            _services = pluginHost.Services;
            _projSync = core.GetComponent<ProjectileSyncTransport>();
        }

        public override void OnSimulateStart()
        {
            base.OnSimulateStart();
            if (_services == null) return;

            _block = _services.Blocks.FromGameObject(gameObject);
            if (_block == null)
            {
                _services.Log.Error("[ACMu] WeaponHostBehaviour: failed to get IBlockAccessor");
                return;
            }

            var reg = WeaponHostRegistry.Get(typeof(TModule));
            if (reg == null)
            {
                _services.Log.Error("[ACMu] WeaponHostBehaviour: no registration found");
                return;
            }

            _baseSpec = reg.Defaults != null ? reg.Defaults.Clone() : new WeaponSpec();

            _weapon = reg.WeaponFactory();

            try
            {
                _weapon.AttachTo(this);
            }
            catch (Exception ex)
            {
                _services.Log.Error("[ACMu] WeaponHostBehaviour.AttachTo: " + ex.Message);
                _weapon = null;
                return;
            }

            _projectileService = _services.Projectiles as ProjectileService;
            _pipeline = new FirePipeline(this, _weapon, _projectileService, this, _services.Session, _projSync);

            try { _weapon.NotifySimulationStart(); }
            catch (Exception ex)
            {
                _services.Log.Error("[ACMu] WeaponHostBehaviour.NotifySimulationStart: " + ex.Message);
            }

            // BaseSpec.PoolSize > 0 の武装はブロックタイプ別の弾体プールに切り替える。
            // 全ピアの OnSimulateStart で同じ typeKey が生成されるため MP プロキシ側も同一キーを引ける。
            if (_baseSpec.PoolSize > 0
                && _baseSpec.ProjectileKey != null
                && _projectileService != null)
            {
                string baseKey  = _baseSpec.ProjectileKey;
                string typeKey  = _baseSpec.ProjectileKey + ":" + _block.BlockName;
                _projectileService.EnsureTypePool(baseKey, typeKey, _baseSpec.PoolSize);
                _baseSpec.ProjectileKey = typeKey;
            }
        }

        public override void SimulateUpdateAlways()
        {
            base.SimulateUpdateAlways();
            if (_weapon == null || !IsAuthority) return;
            try { _weapon.NotifyUpdate(Time.deltaTime); }
            catch (Exception ex)
            {
                if (_services != null)
                    _services.Log.Error("[ACMu] WeaponHostBehaviour.NotifyUpdate: " + ex.Message);
            }
        }

        public override void OnSimulateStop()
        {
            base.OnSimulateStop();
            if (_weapon != null)
            {
                try { _weapon.NotifySimulationStop(); }
                catch (Exception ex)
                {
                    if (_services != null)
                        _services.Log.Error("[ACMu] WeaponHostBehaviour.NotifySimulationStop: " + ex.Message);
                }
            }

            if (_pipeline != null)
            {
                _pipeline.Dispose();
                _pipeline = null;
            }

            _weapon = null;
            _block = null;
            _baseSpec = null;
            _projectileService = null;
        }
    }
}

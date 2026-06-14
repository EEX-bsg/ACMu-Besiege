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

        public Vector3 MuzzlePosition { get { return transform.position; } }
        public Quaternion MuzzleRotation { get { return transform.rotation; } }

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
        }

        public override void OnSimulateStart()
        {
            base.OnSimulateStart();
            if (_services == null) return;

            _block = _services.Blocks.FromGameObject(gameObject);
            if (_block == null)
            {
                _services.Log.Error("[ACMu] WeaponHostBehaviour: failed to get IBlockAccessor for "
                    + typeof(TModule).Name);
                return;
            }

            var reg = WeaponHostRegistry.Get(typeof(TModule));
            if (reg == null)
            {
                _services.Log.Error("[ACMu] WeaponHostBehaviour: no registration for "
                    + typeof(TModule).Name);
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
                _services.Log.Error("[ACMu] WeaponHostBehaviour.AttachTo: " + ex);
                _weapon = null;
                return;
            }

            _projectileService = _services.Projectiles as ProjectileService;
            _pipeline = new FirePipeline(this, _weapon, _projectileService, this);

            try { _weapon.NotifySimulationStart(); }
            catch (Exception ex)
            {
                _services.Log.Error("[ACMu] WeaponHostBehaviour.NotifySimulationStart: " + ex);
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
                    _services.Log.Error("[ACMu] WeaponHostBehaviour.NotifyUpdate: " + ex);
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
                        _services.Log.Error("[ACMu] WeaponHostBehaviour.NotifySimulationStop: " + ex);
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

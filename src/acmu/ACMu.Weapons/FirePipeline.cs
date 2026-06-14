using System;
using System.Collections;
using System.Collections.Generic;
using ACMu.Core.Weapons;
using UnityEngine;

namespace ACMu.Weapons
{
    internal sealed class FirePipeline
    {
        private readonly IWeaponHost _host;
        private readonly WeaponComponentBase _weapon;
        private readonly ProjectileService _projectileService;
        private readonly MonoBehaviour _coroutineRunner;

        private float _lastFireTime = float.NegativeInfinity;
        private readonly HashSet<int> _myHandles = new HashSet<int>();

        internal FirePipeline(
            IWeaponHost host,
            WeaponComponentBase weapon,
            ProjectileService projectileService,
            MonoBehaviour coroutineRunner)
        {
            _host = host;
            _weapon = weapon;
            _projectileService = projectileService;
            _coroutineRunner = coroutineRunner;

            if (_projectileService != null)
            {
                _projectileService.ImpactOccurred += OnImpactOccurred;
                _projectileService.Despawned += OnDespawned;
            }
        }

        internal void Dispose()
        {
            if (_projectileService != null)
            {
                _projectileService.ImpactOccurred -= OnImpactOccurred;
                _projectileService.Despawned -= OnDespawned;
            }
            _myHandles.Clear();
        }

        internal void RequestFire()
        {
            float now = Time.time;
            if (now - _lastFireTime < _host.BaseSpec.FireIntervalSeconds) return;
            _lastFireTime = now;

            var shot = _host.BaseSpec.Clone();
            var context = new FireContext(_host, shot);
            context.MuzzlePosition = _host.MuzzlePosition;
            context.MuzzleRotation = _host.MuzzleRotation;
            context.Direction = _host.MuzzleRotation * Vector3.forward;
            context.Seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

            FireDecision decision = FireDecision.Proceed;
            try { decision = _weapon.NotifyValidateFire(context); }
            catch (Exception ex) { _host.Log.Error("[ACMu] FirePipeline.ValidateFire: " + ex); }

            if (decision == FireDecision.Suppress) return;

            try { _weapon.NotifyBeforeFire(context); }
            catch (Exception ex) { _host.Log.Error("[ACMu] FirePipeline.BeforeFire: " + ex); }

            if (context.DelaySeconds > 0f)
                _coroutineRunner.StartCoroutine(DelayedFire(context));
            else
                ExecuteFire(context);
        }

        private IEnumerator DelayedFire(FireContext context)
        {
            yield return new WaitForSeconds(context.DelaySeconds);
            ExecuteFire(context);
        }

        private void ExecuteFire(FireContext context)
        {
            ProjectileHandle handle = ProjectileHandle.Invalid;

            if (context.Shot.ProjectileKey != null && _host.Projectiles != null)
            {
                var request = new ProjectileSpawnRequest
                {
                    ProjectileKey = context.Shot.ProjectileKey,
                    Position = context.MuzzlePosition,
                    Rotation = context.MuzzleRotation,
                    Velocity = context.Direction * context.Shot.MuzzleVelocity,
                    Owner = _host,
                    Shot = context.Shot,
                    Guidance = context.Guidance
                };
                handle = _host.Projectiles.Spawn(request);
                if (handle.IsValid)
                    _myHandles.Add(handle.Id);
            }

            try { _weapon.NotifyAfterFire(context, handle); }
            catch (Exception ex) { _host.Log.Error("[ACMu] FirePipeline.AfterFire: " + ex); }
        }

        private void OnImpactOccurred(ProjectileHandle handle, Collision collision)
        {
            if (!_myHandles.Contains(handle.Id)) return;

            var contacts = collision.contacts;
            Vector3 pos = contacts.Length > 0 ? contacts[0].point : _host.MuzzlePosition;
            Vector3 normal = contacts.Length > 0 ? contacts[0].normal : Vector3.up;

            var impact = new ImpactContext
            {
                Projectile = handle,
                Position = pos,
                Normal = normal,
                HitObject = collision.gameObject,
                Damage = _host.BaseSpec.Damage,
                PlayerDamage = _host.BaseSpec.PlayerDamage,
                ExplosionRadius = _host.BaseSpec.ExplosionRadius,
                SuppressDefaultExplosion = false
            };

            try { _weapon.NotifyImpact(impact); }
            catch (Exception ex) { _host.Log.Error("[ACMu] FirePipeline.NotifyImpact: " + ex); }

            try { _weapon.NotifyExplosion(impact); }
            catch (Exception ex) { _host.Log.Error("[ACMu] FirePipeline.NotifyExplosion: " + ex); }
        }

        private void OnDespawned(ProjectileHandle handle, DespawnReason reason)
        {
            _myHandles.Remove(handle.Id);
        }
    }
}

using System;
using ACMu.Adapter;
using ACMu.Compat.Shooting;
using ACMu.Compat.TestCannon;
using ACMu.Core;
using ACMu.Net;
using ACMu.PluginApi;
using ACMu.Weapons;
using UnityEngine;

namespace ACMu.Host
{
    internal static class AcmuCoreBootstrap
    {
        internal static void Initialize()
        {
            try
            {
                Boot();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[ACMu] Bootstrap failed: " + ex.Message);
            }
        }

        private static void Boot()
        {
            var go = new GameObject(WellKnownNames.CoreObjectName);
            UnityEngine.Object.DontDestroyOnLoad(go);

            var log = go.AddComponent<ConsoleLog>();
            var session = go.AddComponent<GameSessionInfoAdapter>();
            var events = go.AddComponent<GameEventSourceAdapter>();
            var blocks = go.AddComponent<BlockAccessorFactoryAdapter>();
            var config = go.AddComponent<ModIoConfigStore>();

            var network = go.AddComponent<ModNetTransport>();
            network.InitializeService(log, events, 1);

            var projectiles = go.AddComponent<ProjectileService>();
            projectiles.InitializeService(log, session);

            var projSync = go.AddComponent<ProjectileSyncTransport>();
            projSync.InitializeService(log, session, network, projectiles);

            var registry = new WeaponRegistryImpl();

            var services = go.AddComponent<AcmuServicesComponent>();
            services.Initialize(log, session, events, blocks, config, network, projectiles, registry);

            var coordinator = go.AddComponent<LifecycleCoordinator>();
            coordinator.Initialize(events, session, log);
            coordinator.AddParticipant(log);
            coordinator.AddParticipant(session);
            coordinator.AddParticipant(events);
            coordinator.AddParticipant(blocks);
            coordinator.AddParticipant(config);
            coordinator.AddParticipant(network);
            coordinator.AddParticipant(projectiles);
            coordinator.AddParticipant(projSync);

            coordinator.SortAndBootstrap();

            RegisterTestCannon(services, registry);
            RegisterAdShooting(services, registry);
        }

        private static void RegisterAdShooting(IAcmuServices services, IWeaponRegistry registry)
        {
            var spherePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spherePrefab.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
            spherePrefab.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(spherePrefab);
            var rb = spherePrefab.AddComponent<Rigidbody>();
            rb.mass = 0.4f;
            rb.drag = 0f;
            rb.angularDrag = 5f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            try
            {
                services.Projectiles.RegisterProjectile(
                    AdShootingWeapon.SharedProjectileKey, spherePrefab, 32);
            }
            catch (Exception ex)
            {
                services.Log.Error("[ACMu] Bootstrap: RegisterProjectile AdShooting failed: " + ex.Message);
            }

            try
            {
                registry.Register<AdShootingModule, AdShootingHostBehaviour>(new WeaponRegistration
                {
                    ModuleName = "AdShootingProp",
                    MultiplayerCompatible = true,
                    WeaponFactory = () => new AdShootingWeapon()
                });
                services.Log.Info("[ACMu] AdShootingProp registered");
            }
            catch (Exception ex)
            {
                services.Log.Error("[ACMu] Bootstrap: Register AdShooting failed: " + ex.Message);
            }
        }

        private static void RegisterTestCannon(IAcmuServices services, IWeaponRegistry registry)
        {
            // Build a sphere prefab at runtime (Cube流用 / Sphere) for M2 testing
            var spherePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spherePrefab.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            spherePrefab.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(spherePrefab);
            var rb = spherePrefab.AddComponent<Rigidbody>();
            rb.mass = 0.1f;

            try
            {
                services.Projectiles.RegisterProjectile(
                    TestCannonWeapon.ProjectileKey, spherePrefab, 5);
            }
            catch (Exception ex)
            {
                services.Log.Error("[ACMu] Bootstrap: RegisterProjectile failed: " + ex.Message);
            }

            try
            {
                registry.Register<TestCannonModule, TestCannonHostBehaviour>(new WeaponRegistration
                {
                    ModuleName = "AcmuTestCannon",
                    MultiplayerCompatible = false,
                    WeaponFactory = () => new TestCannonWeapon()
                });
                services.Log.Info("[ACMu] TestCannon registered");
            }
            catch (Exception ex)
            {
                services.Log.Error("[ACMu] Bootstrap: Register TestCannon failed: " + ex.Message);
            }
        }
    }
}

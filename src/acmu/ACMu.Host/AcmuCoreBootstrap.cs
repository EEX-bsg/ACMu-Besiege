using System;
using ACMu.Adapter;
using ACMu.Compat.Shooting;
using ACMu.Compat.TestCannon;
using ACMu.Core;
using ACMu.Net;
using ACMu.PluginApi;
using ACMu.Weapons;
using Modding.Modules;
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
            projectiles.InitializeService(log, session, events);

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

            DamageRegistry.SetApplyFn(DamageApplierAdapter.ApplyDamage);
            FreezeRegistry.SetApplyFn(FreezeApplierAdapter.ApplyFreeze);
            GameRulesRegistry.SetInfiniteAmmoFn(GameSessionInfoAdapter.IsInfiniteAmmoMode);

            ProjectileDebugVisual.SetProviders(
                BesiegeColliderVisuals.GetDebugMaterial,
                BesiegeColliderVisuals.GetGridSpherePrefab);

            var effectLoader = go.AddComponent<EffectBundleAdapter>();
            EffectRegistry.SetFunctions(
                effectLoader.Rent,
                effectLoader.Return,
                effectLoader.ReturnAll,
                effectLoader.BeginLifetime,
                effectLoader.Fade,
                effectLoader.LoadMeshAsset,
                effectLoader.LoadMaterialFromTexture);
            EffectRegistry.SetSoundFn(effectLoader.PlaySounds);

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

            try
            {
                services.Projectiles.RegisterProjectile(
                    OldCannonWeapon.SharedProjectileKey, spherePrefab, 32);
            }
            catch (Exception ex)
            {
                services.Log.Error("[ACMu] Bootstrap: RegisterProjectile AdShooting failed: " + ex.Message);
            }

            try
            {
                registry.Register<OldCannonModule>(new WeaponRegistration
                {
                    ModuleName = "AdShootingProp",
                    MultiplayerCompatible = true,
                    WeaponFactory = () => new OldCannonWeapon(),
                    // AddBlockModule 呼び出しはこの acmu.dll 内に焼かれるため AdShootingProp は
                    // ACMu 名義で登録される。互換ブロックは ACMu が提供する建付けなのでこれが正。
                    // canReload(第2引数)は旧来 MultiplayerCompatible を誤って流用していた値(true)を
                    // 明示的に踏襲。両者は別概念であり、適正値はオーナー確認待ち。
                    ModuleRegistrar = delegate
                    {
                        CustomModules.AddBlockModule<OldCannonModule, OldCannonHostBehaviour>("AdShootingProp", true);
                    }
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
                registry.Register<TestCannonModule>(new WeaponRegistration
                {
                    ModuleName = "AcmuTestCannon",
                    MultiplayerCompatible = false,
                    WeaponFactory = () => new TestCannonWeapon(),
                    // 名義は ACMu(acmu.dll 内呼び出し)。canReload は旧来の流用値(false)を明示踏襲。
                    ModuleRegistrar = delegate
                    {
                        CustomModules.AddBlockModule<TestCannonModule, TestCannonHostBehaviour>("AcmuTestCannon", false);
                    }
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

using System.Collections.Generic;
using ACMu.Core.Weapons;
using ACMu.Weapons;
using UnityEngine;

namespace ACMu.Compat.Shooting
{
    public sealed class AdShootingHostBehaviour : WeaponHostBehaviour<AdShootingModule>
    {
        private CollisionDetectionMode _collisionMode = CollisionDetectionMode.ContinuousDynamic;

        // handle.Id → 弾体にアタッチしたトレイル/弾体エフェクトGO
        private readonly Dictionary<int, GameObject> _trailGos  = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, GameObject> _bulletGos = new Dictionary<int, GameObject>();

        public override Vector3 MuzzlePosition
        {
            get
            {
                AdShootingModule m = Module;
                if (m == null || m.ProjectileStart == null) return transform.position;
                return transform.position + transform.rotation * m.ProjectileStart.ToPosition();
            }
        }

        public override Quaternion MuzzleRotation
        {
            get
            {
                AdShootingModule m = Module;
                if (m == null || m.ProjectileStart == null) return transform.rotation;
                return transform.rotation * m.ProjectileStart.ToRotation();
            }
        }

        public override void OnSimulateStart()
        {
            AdShootingModule m = Module;
            if (m != null && m.Shooting != null)
                _collisionMode = ParseCollisionMode(m.Shooting.CollisionTypeS);

            if (Projectiles != null)
            {
                Projectiles.Spawned   += ApplyProjectileSetup;
                Projectiles.Despawned += OnProjectileDespawned;
            }

            AdShootingWeapon.LoadingModule = m;
            base.OnSimulateStart();
            AdShootingWeapon.LoadingModule = null;
        }

        public override void OnSimulateStop()
        {
            if (Projectiles != null)
            {
                Projectiles.Spawned   -= ApplyProjectileSetup;
                Projectiles.Despawned -= OnProjectileDespawned;
            }

            // シミュ終了時に追跡中のエフェクトを破棄
            foreach (var kvp in _trailGos)
                if (kvp.Value != null) Object.Destroy(kvp.Value);
            _trailGos.Clear();

            foreach (var kvp in _bulletGos)
                if (kvp.Value != null) Object.Destroy(kvp.Value);
            _bulletGos.Clear();

            base.OnSimulateStop();
        }

        // Spawned ハンドラ: 衝突判定モード + トレイル/弾体エフェクト設定
        private void ApplyProjectileSetup(ProjectileHandle handle)
        {
            GameObject projGo;
            if (!Projectiles.TryGetGameObject(handle, out projGo)) return;

            var rb = projGo.GetComponent<Rigidbody>();
            if (rb != null) rb.collisionDetectionMode = _collisionMode;

            AdShootingModule m = Module;
            if (m == null) return;

            string bundle = m.AssetBundleName != null ? m.AssetBundleName.Name : "";

            if (!string.IsNullOrEmpty(m.TrailEffect) && m.TrailEffect != "none")
            {
                var trail = EffectRegistry.Spawn(bundle, m.TrailEffect, projGo.transform.position, projGo.transform.rotation);
                if (trail != null)
                {
                    trail.transform.SetParent(projGo.transform, false);
                    _trailGos[handle.Id] = trail;
                }
            }

            if (!string.IsNullOrEmpty(m.BulletEffect) && m.BulletEffect != "none")
            {
                var bullet = EffectRegistry.Spawn(bundle, m.BulletEffect, projGo.transform.position, projGo.transform.rotation);
                if (bullet != null)
                {
                    bullet.transform.SetParent(projGo.transform, false);
                    _bulletGos[handle.Id] = bullet;
                }
            }
        }

        // Despawned ハンドラ: エフェクトを切り離してフェードアウトさせる
        // ※ Despawned は SetActive(false) 後に発火するため TryGetGameObject は使えない
        private void OnProjectileDespawned(ProjectileHandle handle, DespawnReason reason)
        {
            DetachAndFade(handle.Id, _trailGos);
            DetachAndFade(handle.Id, _bulletGos);
        }

        private static void DetachAndFade(int handleId, Dictionary<int, GameObject> dict)
        {
            GameObject go;
            if (!dict.TryGetValue(handleId, out go)) return;
            dict.Remove(handleId);
            if (go == null) return;
            go.transform.SetParent(null, true);
            go.SetActive(true);
            Object.Destroy(go, 5f);
        }

        private static CollisionDetectionMode ParseCollisionMode(string s)
        {
            if (s == "Continuous")        return CollisionDetectionMode.Continuous;
            if (s == "ContinuousDynamic") return CollisionDetectionMode.ContinuousDynamic;
            return CollisionDetectionMode.Discrete;
        }
    }
}

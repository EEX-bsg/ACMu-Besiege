using System.Collections.Generic;
using ACMu.Core.Weapons;
using ACMu.Weapons;
using UnityEngine;

namespace ACMu.Compat.Shooting
{
    public sealed class OldCannonHostBehaviour : WeaponHostBehaviour<OldCannonModule>
    {
        private CollisionDetectionMode _collisionMode = CollisionDetectionMode.ContinuousDynamic;

        // handle.Id → 弾体にアタッチしたトレイル/弾体エフェクトGO
        private readonly Dictionary<int, GameObject> _trailGos  = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, GameObject> _bulletGos = new Dictionary<int, GameObject>();

        public override Vector3 MuzzlePosition
        {
            get
            {
                OldCannonModule m = Module;
                if (m == null || m.ProjectileStart == null) return transform.position;
                return transform.position + transform.rotation * m.ProjectileStart.ToPosition();
            }
        }

        public override Quaternion MuzzleRotation
        {
            get
            {
                OldCannonModule m = Module;
                if (m == null || m.ProjectileStart == null) return transform.rotation;
                return transform.rotation * m.ProjectileStart.ToRotation();
            }
        }

        public override void OnSimulateStart()
        {
            OldCannonModule m = Module;
            if (m != null && m.Shooting != null)
                _collisionMode = ParseCollisionMode(m.Shooting.CollisionTypeS);

            if (Projectiles != null)
                Projectiles.Despawned += OnProjectileDespawned;

            OldCannonWeapon.LoadingModule = m;
            base.OnSimulateStart();
            OldCannonWeapon.LoadingModule = null;
        }

        public override void OnSimulateStop()
        {
            if (Projectiles != null)
                Projectiles.Despawned -= OnProjectileDespawned;

            _trailGos.Clear();
            _bulletGos.Clear();
            EffectRegistry.ReturnAll();

            base.OnSimulateStop();
        }

        // OldCannonWeapon.OnAfterFire から呼ばれる。
        // このホストが所有する弾体のみを受け取るため、複数ブロック配置でも重複しない。
        internal void AttachProjectileEffects(ProjectileHandle handle)
        {
            GameObject projGo;
            if (!Projectiles.TryGetGameObject(handle, out projGo)) return;

            var rb = projGo.GetComponent<Rigidbody>();
            if (rb != null) rb.collisionDetectionMode = _collisionMode;

            OldCannonModule m = Module;
            if (m == null) return;

            string bundle   = m.AssetBundleName != null ? m.AssetBundleName.Name : "";
            int    poolSize = m.PoolSize;

            // 弾体メッシュ/テクスチャ: 指定があれば球体デフォルトを上書き。プール返却時に自動復元。
            if (m.Shooting != null)
            {
                string meshName    = m.Shooting.Mesh    != null ? m.Shooting.Mesh.Name    : "";
                string textureName = m.Shooting.Texture != null ? m.Shooting.Texture.Name : "";
                Mesh     mesh = EffectRegistry.LoadMesh(bundle, meshName);
                Material mat  = EffectRegistry.LoadMaterial(bundle, textureName);
                if (mesh != null || mat != null)
                {
                    var restorer = projGo.GetComponent<ProjectileMeshRestorer>();
                    if (restorer == null) restorer = projGo.AddComponent<ProjectileMeshRestorer>();
                    restorer.Apply(mesh, mat);
                }
            }

            if (!string.IsNullOrEmpty(m.TrailEffect) && m.TrailEffect != "none")
            {
                var trail = EffectRegistry.Spawn(bundle, m.TrailEffect, projGo.transform.position, projGo.transform.rotation, poolSize, false);
                if (trail != null)
                {
                    trail.transform.SetParent(projGo.transform, false);
                    trail.transform.localPosition = Vector3.zero;
                    trail.transform.localRotation = Quaternion.identity;
                    _trailGos[handle.Id] = trail;
                }
            }

            if (!string.IsNullOrEmpty(m.BulletEffect) && m.BulletEffect != "none")
            {
                var bullet = EffectRegistry.Spawn(bundle, m.BulletEffect, projGo.transform.position, projGo.transform.rotation, poolSize, false);
                if (bullet != null)
                {
                    bullet.transform.SetParent(projGo.transform, false);
                    bullet.transform.localPosition = Vector3.zero;
                    bullet.transform.localRotation = Quaternion.identity;
                    _bulletGos[handle.Id] = bullet;
                }
            }
        }

        // Despawned ハンドラ
        // ※ Despawned は SetActive(false) 後に発火するため TryGetGameObject は使えない
        private void OnProjectileDespawned(ProjectileHandle handle, DespawnReason reason)
        {
            OldCannonModule m = Module;
            string bundle      = m != null && m.AssetBundleName != null ? m.AssetBundleName.Name : "";

            GameObject trailGo;
            if (_trailGos.TryGetValue(handle.Id, out trailGo))
            {
                _trailGos.Remove(handle.Id);
                DetachAndFade(trailGo);
            }

            GameObject bulletGo;
            if (_bulletGos.TryGetValue(handle.Id, out bulletGo))
            {
                _bulletGos.Remove(handle.Id);
                if (reason == DespawnReason.Impact)
                {
                    string bulletEffect = m != null ? m.BulletEffect : "";
                    EffectRegistry.Return(bundle, bulletEffect, bulletGo);
                }
                else
                {
                    DetachAndFade(bulletGo);
                }
            }
        }

        private static void DetachAndFade(GameObject go)
        {
            if (go == null) return;
            go.transform.SetParent(null, true);
            go.SetActive(true);
            EffectRegistry.Fade(go);
        }

        private static CollisionDetectionMode ParseCollisionMode(string s)
        {
            if (s == "Continuous")        return CollisionDetectionMode.Continuous;
            if (s == "ContinuousDynamic") return CollisionDetectionMode.ContinuousDynamic;
            return CollisionDetectionMode.Discrete;
        }
    }
}

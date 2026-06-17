using System;
using System.Collections.Generic;
using ACMu.Core.Weapons;
using ACMu.Weapons;
using UnityEngine;

namespace ACMu.Compat.Shooting
{
    public sealed class OldCannonHostBehaviour : WeaponHostBehaviour<OldCannonModule>
    {
        // ThrustDelayTimerSlider が未設置のブロックで使うフォールバック既定値
        private const float FallbackThrustDelayTimer = 0.5f;

        private CollisionDetectionMode _collisionMode = CollisionDetectionMode.ContinuousDynamic;

        // handle.Id → 弾体にアタッチしたトレイル/弾体エフェクトGO
        private readonly Dictionary<int, GameObject> _trailGos  = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, GameObject> _bulletGos = new Dictionary<int, GameObject>();

        // handle.Id → Attaches=true で命中対象に刺さって止まっている弾体GO(Despawn時に親子関係を復元する)
        private readonly Dictionary<int, GameObject> _attachedGos = new Dictionary<int, GameObject>();

        // エフェクト・爆発に必要な情報をシミュ開始時にキャッシュ
        private string _bundleName      = "";
        private string _explodeEffect   = "";
        private float  _explodePower    = 0f;
        private float  _explodeUpPower  = 0f;
        private bool   _projectilesExplode = false;
        private float  _explosionRadius = 0f;
        private int    _poolSize        = 10;
        private float  _randomFuseInterval = 0f;
        private bool   _useTimefuse     = false;
        private float  _fuseTime        = 3f;
        private float  _fuseDelayTime   = 0f;
        private XmlTransform _shotFlashTransform;

        // ブースター: useBooster=スラスター全体有効化 / useThrustDelayTimer=点火遅延時間の有無
        private bool    _useBooster          = false;
        private bool    _useThrustDelayTimer = false;
        private Vector3 _purgeVector         = Vector3.forward;
        private float   _purgePower          = 0f;

        // 着弾音(実態は爆発音): OnFuseExplosion は OldCannonWeapon.OnExplosion を経由しないため自前で保持
        private readonly List<string> _hitSoundNames = new List<string>();

        // フューズ爆発デリゲート: OnSimulateStart で1回生成してキャッシュ
        private Action<ProjectileHandle> _fuseDelegate;

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

        internal Vector3 FlashMuzzlePosition
        {
            get
            {
                if (_shotFlashTransform == null) return MuzzlePosition;
                return transform.position + transform.rotation * _shotFlashTransform.ToPosition();
            }
        }

        internal Quaternion FlashMuzzleRotation
        {
            get
            {
                if (_shotFlashTransform == null) return MuzzleRotation;
                return transform.rotation * _shotFlashTransform.ToRotation();
            }
        }

        public override void OnSimulateStart()
        {
            OldCannonModule m = Module;
            if (m != null && m.Shooting != null)
                _collisionMode = ParseCollisionMode(m.Shooting.CollisionTypeS);

            if (m != null)
            {
                _bundleName        = m.AssetBundleName != null ? m.AssetBundleName.Name : "";
                _explodeEffect     = m.ExplodeEffect ?? "";
                _explodePower      = m.ExplodePower;
                _explodeUpPower    = m.ExplodeUpPower;
                _projectilesExplode = m.ProjectilesExplode;
                _poolSize          = m.PoolSize;
                _randomFuseInterval = m.RandomFuseInterval;
                _useTimefuse       = m.UseTimefuse ?? false;
                _fuseTime          = m.FuseTime ?? 0f;
                _fuseDelayTime     = m.FuseDelayTime;
                _shotFlashTransform = m.ShotFlashPosition;

                _useBooster          = m.UseBooster ?? false;
                _useThrustDelayTimer = m.UseThrustDelayTimer ?? false;
                _purgeVector = m.PurgeVector != null
                    ? new Vector3(m.PurgeVector.x, m.PurgeVector.y, m.PurgeVector.z)
                    : Vector3.forward;
                _purgePower  = m.PurgePower ?? 0f;

                _hitSoundNames.Clear();
                if (m.HitSounds != null)
                    foreach (var s in m.HitSounds)
                        if (s != null && !string.IsNullOrEmpty(s.Name)) _hitSoundNames.Add(s.Name);
            }

            if (Projectiles != null)
                Projectiles.Despawned += OnProjectileDespawned;

            OldCannonWeapon.LoadingModule = m;
            base.OnSimulateStart();
            OldCannonWeapon.LoadingModule = null;

            // ExplosionRadius は OnAttached 内で BaseSpec に設定されるため base() の後で読む
            _explosionRadius = BaseSpec != null ? BaseSpec.ExplosionRadius : 0f;

            _fuseDelegate = OnFuseExplosion;
        }

        public override void OnSimulateStop()
        {
            if (Projectiles != null)
                Projectiles.Despawned -= OnProjectileDespawned;

            // シミュ終了でブロックが破棄される前に、刺さっている弾体を必ず解放する
            // (親の破棄に巻き込まれてプール GO 自体が消えるのを防ぐ)
            foreach (var kv in _attachedGos)
                DetachFromTarget(kv.Value);
            _attachedGos.Clear();

            _trailGos.Clear();
            _bulletGos.Clear();
            EffectRegistry.ReturnAll();
            _fuseDelegate = null;

            base.OnSimulateStop();
        }

        // OldCannonWeapon.OnAfterFire から呼ばれる。
        // このホストが所有する弾体のみを受け取るため、複数ブロック配置でも重複しない。
        internal void AttachProjectileEffects(ProjectileHandle handle)
        {
            GameObject projGo;
            if (!Projectiles.TryGetGameObject(handle, out projGo)) return;

            OldCannonModule m = Module;
            if (m == null) return;

            string bundle   = _bundleName;
            int    poolSize = _poolSize;

            // ---- Rigidbody / Collider / PhysicMaterial ----
            var rb = projGo.GetComponent<Rigidbody>();
            if (rb != null) rb.collisionDetectionMode = _collisionMode;

            if (m.Shooting != null)
            {
                var physics = projGo.GetComponent<ProjectilePhysicsSetup>();
                if (physics == null) physics = projGo.AddComponent<ProjectilePhysicsSetup>();
                physics.Configure(m.Shooting);
            }

            // ---- 弾頭メッシュ/テクスチャ ----
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

                    Vector3    offset   = m.Shooting.Mesh != null ? m.Shooting.Mesh.GetPosition() : Vector3.zero;
                    Quaternion rotation = m.Shooting.Mesh != null ? m.Shooting.Mesh.GetRotation() : Quaternion.identity;
                    Vector3    scale    = m.Shooting.Mesh != null ? m.Shooting.Mesh.GetScale()    : Vector3.one;
                    restorer.Apply(mesh, mat, offset, rotation, scale);
                }
            }

            // ---- タイムフューズ ----
            if (_useTimefuse && _fuseDelegate != null)
            {
                float fuse;
                if (!Block.TryGetSlider(OldCannonModule.FuseTimerSliderName, out fuse))
                    fuse = _fuseTime > 0f ? _fuseTime : 3f;

                float jitter = _randomFuseInterval > 0f
                    ? UnityEngine.Random.Range(-_randomFuseInterval, _randomFuseInterval)
                    : 0f;
                fuse = Mathf.Max(fuse + jitter, _fuseDelayTime);

                var fuseTimer = projGo.GetComponent<ProjectileFuseTimer>();
                if (fuseTimer == null) fuseTimer = projGo.AddComponent<ProjectileFuseTimer>();
                fuseTimer.Activate(handle, fuse, _fuseDelegate);
            }

            // ---- ブースター: パージ + 横方向安定化 + 連続前方推力(×100fは原ACM固定スケール) ----
            if (_useBooster)
            {
                // パージ: 発射時に必ず1回掛かる初速付与
                if (rb != null)
                    rb.AddRelativeForce(_purgeVector * _purgePower * 100f, ForceMode.Impulse);

                // boosterPower = PowerSlider.Value(原ACM互換: 連続推力はパワースライダー参照)
                float boosterPower;
                if (!Block.TryGetSlider(OldCannonModule.PowerSliderName, out boosterPower))
                    boosterPower = 0f;

                // useThrustDelayTimer=true: ThrustDelayTimerSlider 秒後に点火 / false: 即時点火(遅延0)
                float delay = 0f;
                if (_useThrustDelayTimer)
                {
                    if (!Block.TryGetSlider(OldCannonModule.ThrustDelayTimerSliderName, out delay))
                        delay = FallbackThrustDelayTimer;
                }

                var booster = projGo.GetComponent<ProjectileBoosterBehaviour>();
                if (booster == null) booster = projGo.AddComponent<ProjectileBoosterBehaviour>();
                booster.Activate(rb, boosterPower, delay);
            }

            // ---- トレイル/弾体エフェクト ----
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

        // タイムフューズが発火したときにホストが直接爆発処理を行う。
        private void OnFuseExplosion(ProjectileHandle handle)
        {
            GameObject projGo;
            if (Projectiles == null || !Projectiles.TryGetGameObject(handle, out projGo)) return;

            Vector3 pos = projGo.transform.position;

            if (_projectilesExplode && _explosionRadius > 0f)
            {
                float scaledPower = _explodePower   * 2f;
                float scaledUp    = _explodeUpPower * 0.25f;
                Collider[] hits = Physics.OverlapSphere(pos, _explosionRadius);
                foreach (var col in hits)
                {
                    Rigidbody colRb = col.attachedRigidbody;
                    if (colRb != null)
                        colRb.AddExplosionForce(scaledPower, pos, _explosionRadius, scaledUp);
                }
            }

            // 着弾音(原ACM互換: HitSounds は着弾時ではなく爆発時に再生される)。
            // フューズ爆発は ProjectilesExplode(force適用有無)に関わらず必ず鳴る/見える(エフェクトと同じ扱い)
            EffectRegistry.PlaySounds(_hitSoundNames, pos);

            if (!string.IsNullOrEmpty(_explodeEffect))
                EffectRegistry.Spawn(_bundleName, _explodeEffect, pos, Quaternion.identity, _poolSize, true);

            Projectiles.Despawn(handle, DespawnReason.Manual);
        }

        // Attaches=true: 弾体を命中対象へ刺して固定する。OldCannonWeapon.OnImpact から呼ばれる。
        internal void AttachProjectile(ProjectileHandle handle, Transform target)
        {
            GameObject projGo;
            if (!Projectiles.TryGetGameObject(handle, out projGo)) return;

            var rb = projGo.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            projGo.transform.SetParent(target, true);
            _attachedGos[handle.Id] = projGo;
        }

        // 刺さっている弾体の親子関係・物理状態を復元する(Despawn / シミュ終了時)
        private static void DetachFromTarget(GameObject projGo)
        {
            if (projGo == null) return;
            projGo.transform.SetParent(null, true);
            var rb = projGo.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;
        }

        // Despawned ハンドラ
        // ※ Despawned は SetActive(false) 後に発火するため TryGetGameObject は使えない
        private void OnProjectileDespawned(ProjectileHandle handle, DespawnReason reason)
        {
            OldCannonModule m = Module;
            string bundle      = _bundleName;

            GameObject attachedGo;
            if (_attachedGos.TryGetValue(handle.Id, out attachedGo))
            {
                _attachedGos.Remove(handle.Id);
                DetachFromTarget(attachedGo);
            }

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
                if (reason == DespawnReason.Impact || reason == DespawnReason.Manual)
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

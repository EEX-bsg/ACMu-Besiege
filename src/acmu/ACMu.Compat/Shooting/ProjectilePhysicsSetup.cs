using System.Collections.Generic;
using UnityEngine;

namespace ACMu.Compat.Shooting
{
    // プロジェクタイルGOに付けるコンポーネント。
    // ShootingState の値を Rigidbody・Collider・PhysicMaterial に反映する。
    // Configure は最初の AttachProjectileEffects 呼び出し時に1回設定される。
    // OnDisable(プール返却)でコライダー状態を復元し、OnEnable(次の発射)で再適用する。
    internal sealed class ProjectilePhysicsSetup : MonoBehaviour
    {
        // 発射直後、自機/発射元への即時誤爆を防ぐためコライダーを無効化しておく時間(原ACM互換固定値)
        private const float ColliderEnableDelay = 0.02f;

        private ShootingState _config;
        private Collider _originalCollider;
        private readonly List<Collider> _addedColliders = new List<Collider>();
        private PhysicMaterial _physicMat;
        private bool _collidersApplied;

        private bool  _colliderEnableDelayActive;
        private float _colliderEnableElapsed;

        internal void Configure(ShootingState state)
        {
            if (state == null) return;
            _config = state;

            ApplyRigidbody();

            if (!_collidersApplied)
            {
                ApplyColliders();
                _collidersApplied = true;
            }

            ApplyPhysicMaterial();

            SetActiveColliders(false);
            _colliderEnableElapsed = 0f;
            _colliderEnableDelayActive = true;
        }

        void Update()
        {
            if (!_colliderEnableDelayActive) return;
            _colliderEnableElapsed += Time.deltaTime;
            if (_colliderEnableElapsed >= ColliderEnableDelay)
            {
                _colliderEnableDelayActive = false;
                SetActiveColliders(true);
            }
        }

        void OnDisable()
        {
            _colliderEnableDelayActive = false;

            // カスタムコライダーを無効化してオリジナルを復元
            if (_addedColliders.Count > 0)
            {
                if (_originalCollider != null) _originalCollider.enabled = true;
                foreach (var col in _addedColliders)
                    if (col != null) col.enabled = false;
            }
        }

        void OnEnable()
        {
            if (!_collidersApplied) return;
            // カスタムコライダーを再有効化
            if (_addedColliders.Count > 0)
            {
                if (_originalCollider != null) _originalCollider.enabled = false;
                foreach (var col in _addedColliders)
                    if (col != null) col.enabled = true;
            }
        }

        // 現在有効化されているべきコライダー群(カスタム優先、無ければ素体)に enabled を適用
        private void SetActiveColliders(bool active)
        {
            if (_addedColliders.Count > 0)
            {
                foreach (var added in _addedColliders)
                    if (added != null) added.enabled = active;
                return;
            }

            Collider baseCollider = _originalCollider != null ? _originalCollider : GetComponent<Collider>();
            if (baseCollider != null) baseCollider.enabled = active;
        }

        private void ApplyRigidbody()
        {
            if (_config == null) return;
            var rb = GetComponent<Rigidbody>();
            if (rb == null) return;
            rb.mass        = _config.Mass;
            rb.drag        = _config.Drag;
            rb.angularDrag = _config.AngularDrag;
            rb.useGravity  = !_config.IgnoreGravity;
        }

        private void ApplyColliders()
        {
            if (_config == null) return;
            var list = _config.ProjectileColliders;
            if (list == null || list.Count == 0) return;

            _originalCollider = GetComponent<Collider>();
            if (_originalCollider != null) _originalCollider.enabled = false;

            foreach (var def in list)
            {
                if (def == null) continue;
                var col = def.AddTo(gameObject);
                if (col != null)
                {
                    _addedColliders.Add(col);
                    if (_physicMat != null) col.sharedMaterial = _physicMat;
                }
            }
        }

        private void ApplyPhysicMaterial()
        {
            if (_config == null) return;
            if (_config.BounceStr <= 0f && _config.FrictionStr <= 0f) return;

            if (_physicMat == null)
                _physicMat = new PhysicMaterial("[ACMu]ProjMat");

            _physicMat.bounciness      = _config.BounceStr;
            _physicMat.bounceCombine   = ParseCombine(_config.BounceCombineType);
            _physicMat.dynamicFriction = _config.FrictionStr;
            _physicMat.staticFriction  = _config.FrictionStr;
            _physicMat.frictionCombine = ParseCombine(_config.FrictionCombineType);

            if (_addedColliders.Count > 0)
            {
                foreach (var col in _addedColliders)
                    if (col != null) col.sharedMaterial = _physicMat;
            }
            else
            {
                var col = GetComponent<Collider>();
                if (col != null) col.sharedMaterial = _physicMat;
            }
        }

        // カスタムコライダーが定義されていればそちらを返し、なければ素体コライダーを返す。
        // ProjectileDebugVisual がデバッグ可視化の対象コライダーを特定するために使う。
        internal Collider[] GetEffectiveColliders()
        {
            if (_addedColliders.Count > 0)
                return _addedColliders.ToArray();
            var col = GetComponent<Collider>();
            if (col != null) return new Collider[] { col };
            return new Collider[0];
        }

        private static PhysicMaterialCombine ParseCombine(string s)
        {
            if (s == "Maximum")  return PhysicMaterialCombine.Maximum;
            if (s == "Minimum")  return PhysicMaterialCombine.Minimum;
            if (s == "Multiply") return PhysicMaterialCombine.Multiply;
            return PhysicMaterialCombine.Average;
        }
    }
}

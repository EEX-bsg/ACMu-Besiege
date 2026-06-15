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
        private ShootingState _config;
        private Collider _originalCollider;
        private readonly List<Collider> _addedColliders = new List<Collider>();
        private PhysicMaterial _physicMat;
        private bool _collidersApplied;

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
        }

        void OnDisable()
        {
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

        private static PhysicMaterialCombine ParseCombine(string s)
        {
            if (s == "Maximum")  return PhysicMaterialCombine.Maximum;
            if (s == "Minimum")  return PhysicMaterialCombine.Minimum;
            if (s == "Multiply") return PhysicMaterialCombine.Multiply;
            return PhysicMaterialCombine.Average;
        }
    }
}

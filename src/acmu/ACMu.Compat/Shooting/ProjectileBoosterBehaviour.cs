using UnityEngine;

namespace ACMu.Compat.Shooting
{
    // useBooster=true のときに弾体にアタッチされ、スラスターシステム全体を管理する。
    // 原ACM WingDragBehavour2 相当: 横方向安定ドラッグ + 点火遅延 + 連続前方推力。
    // OnDisable(プール返却)で自動リセットされるため Activate で毎回初期化される。
    internal sealed class ProjectileBoosterBehaviour : MonoBehaviour
    {
        private const float CapLateral     = 500f;
        private const float ResistanceLow  = 1f;   // 遅延中: フィン抵抗最小(ソフトローンチ)
        private const float ResistanceHigh = 10f;  // 点火後: フィン抵抗最大(安定飛行)

        private Rigidbody _rb;
        private float     _boosterPower;  // PowerSlider.Value — 連続前方推力に使用
        private float     _delay;         // 0 = 即時点火
        private float     _elapsed;
        private bool      _thrusting;     // 遅延終了→連続推力フェーズ
        private bool      _active;

        // boosterPower = PowerSlider.Value(原ACM互換)、delay = 0 なら即時点火
        internal void Activate(Rigidbody rb, float boosterPower, float delay)
        {
            _rb           = rb;
            _boosterPower = boosterPower;
            _delay        = Mathf.Max(0f, delay);
            _elapsed      = 0f;
            _thrusting    = _delay <= 0f;
            _active       = true;
        }

        void FixedUpdate()
        {
            if (!_active || _rb == null) return;

            // 点火遅延カウントダウン
            if (!_thrusting)
            {
                _elapsed += Time.fixedDeltaTime;
                if (_elapsed >= _delay)
                    _thrusting = true;
            }

            // 横方向安定ドラッグ(WingDragBehavour2相当): 弾体前方に垂直な速度成分を打ち消す
            // 遅延中は resistance=1(弱安定)、点火後は resistance=10(強安定・直進誘導)
            float resistance = _thrusting ? ResistanceHigh : ResistanceLow;
            Vector3 velocity = _rb.velocity;
            if (velocity.sqrMagnitude > 0.0001f)
            {
                Vector3 fwd     = transform.forward;
                Vector3 lateral = velocity - Vector3.Project(velocity, fwd);
                float   mag     = lateral.magnitude * resistance;
                if (mag > 0.0001f)
                    _rb.AddForce(-lateral.normalized * Mathf.Min(mag, CapLateral));
            }

            // 連続前方推力(×100fは原ACM固定スケール): 点火後 FixedUpdate ごとに加算
            if (_thrusting && _boosterPower > 0f)
                _rb.AddForce(transform.forward * 100f * _boosterPower);
        }

        void OnDisable()
        {
            _active    = false;
            _thrusting = false;
            _elapsed   = 0f;
        }
    }
}

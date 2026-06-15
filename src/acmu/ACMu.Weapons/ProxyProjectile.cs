using UnityEngine;

namespace ACMu.Weapons
{
    /// <summary>
    /// クライアント側プロキシ弾の表示制御。物理なし・Transform のみ駆動。
    /// 速度外挿(snap + vel * dt)でほぼゼロ遅延表示、新スナップ受信時にスムーズ補正(lerp)。
    /// </summary>
    internal sealed class ProxyProjectile : MonoBehaviour
    {
        private Vector3 _snapPos;
        private Vector3 _snapVel;
        private float   _snapTime;
        private Vector3 _displayPos;
        private bool    _initialized;

        private const float SmoothSpeed   = 10f;
        private const float CutoffSeconds = 0.5f;

        internal void Initialize(Vector3 position, Vector3 velocity)
        {
            _snapPos     = position;
            _snapVel     = velocity;
            _snapTime    = Time.time;
            _displayPos  = position;
            _initialized = true;
            transform.position = position;
            ApplyRotation(velocity);
        }

        internal void ResetProxy()
        {
            _initialized = false;
        }

        internal void ApplySnapshot(Vector3 position, Vector3 velocity)
        {
            _snapPos  = position;
            _snapVel  = velocity;
            _snapTime = Time.time;
        }

        void Update()
        {
            if (!_initialized) return;

            float elapsed = Time.time - _snapTime;
            if (elapsed > CutoffSeconds) return;

            Vector3 extrapolated = _snapPos + _snapVel * elapsed;
            _displayPos = Vector3.Lerp(_displayPos, extrapolated, Time.deltaTime * SmoothSpeed);
            transform.position = _displayPos;
            ApplyRotation(_snapVel);
        }

        private void ApplyRotation(Vector3 velocity)
        {
            if (velocity.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(velocity);
        }
    }
}

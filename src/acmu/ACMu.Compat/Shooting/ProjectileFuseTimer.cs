using System;
using ACMu.Core.Weapons;
using UnityEngine;

namespace ACMu.Compat.Shooting
{
    // プロジェクタイルGOに付けるタイムフューズ。
    // Activate 後 FuseTime 秒が経過すると OnFired コールバックを1回呼ぶ。
    // OnDisable(プール返却)で自動キャンセルされる。
    internal sealed class ProjectileFuseTimer : MonoBehaviour
    {
        private float _fuseTime;
        private float _elapsed;
        private bool _armed;
        private ProjectileHandle _handle;
        private Action<ProjectileHandle> _callback;

        internal void Activate(ProjectileHandle handle, float fuseTime, Action<ProjectileHandle> callback)
        {
            _handle   = handle;
            _fuseTime = fuseTime;
            _elapsed  = 0f;
            _armed    = fuseTime > 0f && callback != null;
            _callback = callback;
        }

        void Update()
        {
            if (!_armed) return;
            _elapsed += Time.deltaTime;
            if (_elapsed >= _fuseTime)
            {
                _armed = false;
                try { _callback(_handle); }
                catch { }
            }
        }

        void OnDisable()
        {
            _armed = false;
        }
    }
}

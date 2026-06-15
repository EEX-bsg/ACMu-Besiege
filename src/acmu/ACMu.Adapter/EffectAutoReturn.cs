using System;
using System.Collections;
using UnityEngine;

namespace ACMu.Adapter
{
    // プール対応エフェクトGOに AddComponent で付与する。
    // BeginLifetime() を呼ぶと ParticleSystem 完了後に ReturnCallback でプールへ返す。
    internal sealed class EffectAutoReturn : MonoBehaviour
    {
        internal string PoolKey;
        internal Action<string, GameObject> ReturnCallback;

        private ParticleSystem[] _particleSystems;

        void Awake()
        {
            _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        }

        void OnDisable()
        {
            StopAllCoroutines();
        }

        // Flash/爆発用: パーティクルをリセットして再生し、完了後にプールへ返す
        internal void BeginLifetime()
        {
            StopAllCoroutines();
            foreach (var ps in _particleSystems)
            {
                if (ps == null) continue;
                ps.Clear();
                ps.Play();
            }
            StartCoroutine(WaitAndReturn());
        }

        // Trail/Bullet切り離し後: エミッションを止めて既存パーティクルが消えたらプールへ返す
        internal void BeginFade()
        {
            StopAllCoroutines();
            foreach (var ps in _particleSystems)
                if (ps != null) ps.Stop();
            StartCoroutine(WaitAndReturn());
        }

        private IEnumerator WaitAndReturn()
        {
            if (_particleSystems.Length > 0)
            {
                while (IsAnyAlive()) yield return null;
            }
            else
            {
                yield return new WaitForSeconds(3f);
            }
            if (ReturnCallback != null && gameObject != null && gameObject.activeSelf)
                ReturnCallback(PoolKey, gameObject);
        }

        private bool IsAnyAlive()
        {
            foreach (var ps in _particleSystems)
                if (ps != null && ps.IsAlive(true)) return true;
            return false;
        }
    }
}

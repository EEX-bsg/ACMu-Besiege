using System;
using System.Collections.Generic;
using UnityEngine;

namespace ACMu.Compat.Shooting
{
    // BootstrapがSetLoaderでAdapterのローダーを注入する。
    // エフェクトPrefabはLazyLoadされ、初回Spawn時にバンドルから取得する。
    internal static class EffectRegistry
    {
        private static readonly Dictionary<string, GameObject> _cache = new Dictionary<string, GameObject>();
        private static Func<string, GameObject> _loader;

        internal static void SetLoader(Func<string, GameObject> loader)
        {
            _loader = loader;
        }

        internal static void Clear()
        {
            _cache.Clear();
            _loader = null;
        }

        // nameが空/"none"の場合は何もせずnullを返す
        internal static GameObject Spawn(string name, Vector3 pos, Quaternion rot)
        {
            var prefab = TryGet(name);
            if (prefab == null) return null;
            return (GameObject)UnityEngine.Object.Instantiate(prefab, pos, rot);
        }

        private static GameObject TryGet(string name)
        {
            if (string.IsNullOrEmpty(name) || name == "none") return null;
            GameObject prefab;
            if (_cache.TryGetValue(name, out prefab)) return prefab;
            if (_loader == null) return null;
            try { prefab = _loader(name); }
            catch { return null; }
            if (prefab != null) _cache[name] = prefab;
            return prefab;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ACMu.Compat.Shooting
{
    // Bootstrap が SetLoader で (bundleName, prefabName) -> GO のローダーを注入する。
    // キャッシュキーは "bundleName/prefabName"。
    internal static class EffectRegistry
    {
        private static readonly Dictionary<string, GameObject> _cache = new Dictionary<string, GameObject>();
        private static Func<string, string, GameObject> _loader;

        internal static void SetLoader(Func<string, string, GameObject> loader)
        {
            _loader = loader;
        }

        internal static void Clear()
        {
            _cache.Clear();
            _loader = null;
        }

        // bundleName が空 / effectName が空/"none" の場合は何もしない
        internal static GameObject Spawn(string bundleName, string effectName, Vector3 pos, Quaternion rot)
        {
            var prefab = TryGet(bundleName, effectName);
            if (prefab == null) return null;
            return (GameObject)UnityEngine.Object.Instantiate(prefab, pos, rot);
        }

        private static GameObject TryGet(string bundleName, string effectName)
        {
            if (string.IsNullOrEmpty(bundleName) || string.IsNullOrEmpty(effectName) || effectName == "none")
                return null;

            string key = bundleName + "/" + effectName;
            GameObject prefab;
            if (_cache.TryGetValue(key, out prefab)) return prefab;
            if (_loader == null) return null;

            try { prefab = _loader(bundleName, effectName); }
            catch { return null; }

            if (prefab != null) _cache[key] = prefab;
            return prefab;
        }
    }
}

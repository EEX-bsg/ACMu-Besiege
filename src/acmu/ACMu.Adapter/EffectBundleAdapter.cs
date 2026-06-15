using System;
using System.Collections.Generic;
using Modding;
using UnityEngine;

namespace ACMu.Adapter
{
    // エフェクトバンドルの lazy load を担当。
    // Mod.xml <Resources> に使用バンドルを宣言する:
    //   <AssetBundle name="tank_effect"    path="assets/tank_effect" />
    //   <AssetBundle name="tank_effectMac" path="assets/tank_effectMac" />
    // ACM 互換命名規則: Mac/Unix では resourceName + "Mac" のリソースを使用する。
    public class EffectBundleAdapter : MonoBehaviour
    {
        // bundleResourceName(Win) -> ModAssetBundle (ロード済み or null)
        private readonly Dictionary<string, ModAssetBundle> _bundles = new Dictionary<string, ModAssetBundle>();

        // "bundleName/prefabName" -> prefab GO
        private readonly Dictionary<string, GameObject> _cache = new Dictionary<string, GameObject>();

        // EffectRegistry.SetLoader に渡す。(bundleResourceName, prefabName) -> GO
        internal GameObject TryGetPrefab(string bundleName, string prefabName)
        {
            if (string.IsNullOrEmpty(bundleName) || string.IsNullOrEmpty(prefabName) || prefabName == "none")
                return null;

            string cacheKey = bundleName + "/" + prefabName;
            GameObject cached;
            if (_cache.TryGetValue(cacheKey, out cached)) return cached;

            var bundle = GetOrLoadBundle(bundleName);
            if (bundle == null) return null;

            try
            {
                var go = bundle.LoadAsset<GameObject>(prefabName);
                if (go != null) _cache[cacheKey] = go;
                return go;
            }
            catch
            {
                return null;
            }
        }

        // Win用リソース名を受け取り、Mac/Unix では自動的に + "Mac" で読む
        private ModAssetBundle GetOrLoadBundle(string resourceName)
        {
            ModAssetBundle bundle;
            if (_bundles.TryGetValue(resourceName, out bundle)) return bundle;

            bool isMacOrUnix = Application.platform == RuntimePlatform.OSXPlayer
                            || Application.platform == RuntimePlatform.LinuxPlayer;
            string loadName = isMacOrUnix ? resourceName + "Mac" : resourceName;

            try
            {
                bundle = ModResource.GetAssetBundle(loadName);
            }
            catch (Exception ex)
            {
                Debug.LogError("[ACMu] EffectBundleAdapter: bundle load failed '" + loadName + "': " + ex.Message);
                bundle = null;
            }

            _bundles[resourceName] = bundle;
            return bundle;
        }
    }
}

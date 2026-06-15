using System;
using System.Collections.Generic;
using Modding;
using UnityEngine;

namespace ACMu.Adapter
{
    // Mod.xml <Resources> に下記名称でバンドルを宣言する必要がある:
    //   <AssetBundle name="acmu_effects"     path="assets/acmu_effects" />
    //   <AssetBundle name="acmu_effects_mac" path="assets/acmu_effects_mac" />
    public class EffectBundleAdapter : MonoBehaviour
    {
        private const string WinBundleResourceName = "acmu_effects";
        private const string MacBundleResourceName = "acmu_effects_mac";

        private ModAssetBundle _bundle;
        private bool _loaded;
        private readonly Dictionary<string, GameObject> _cache = new Dictionary<string, GameObject>();

        // EffectRegistry.SetLoader に渡す。lazy load: 初回 TryGetPrefab 呼び出し時にバンドルをロード。
        internal GameObject TryGetPrefab(string name)
        {
            if (string.IsNullOrEmpty(name) || name == "none") return null;
            if (!_loaded) LoadBundle();
            if (_bundle == null) return null;

            GameObject cached;
            if (_cache.TryGetValue(name, out cached)) return cached;

            try
            {
                var go = _bundle.LoadAsset<GameObject>(name);
                if (go != null) _cache[name] = go;
                return go;
            }
            catch
            {
                return null;
            }
        }

        private void LoadBundle()
        {
            _loaded = true;
            string bundleName = (Application.platform == RuntimePlatform.OSXPlayer)
                ? MacBundleResourceName
                : WinBundleResourceName;
            try
            {
                _bundle = ModResource.GetAssetBundle(bundleName);
            }
            catch (Exception ex)
            {
                Debug.LogError("[ACMu] EffectBundleAdapter: bundle load failed '" + bundleName + "': " + ex.Message);
            }
        }
    }
}

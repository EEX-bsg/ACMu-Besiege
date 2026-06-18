using System;
using System.Collections.Generic;
using Modding;
using UnityEngine;

namespace ACMu.Adapter
{
    // エフェクトの Lazy バンドルロード・プール管理を担当。
    // Mod.xml <Resources> に下記を宣言する(ACM互換命名: Mac/Unix は Win名+"Mac"):
    //   <AssetBundle name="tank_effect"    path="assets/tank_effect" />
    //   <AssetBundle name="tank_effectMac" path="assets/tank_effectMac" />
    public class EffectBundleAdapter : MonoBehaviour
    {
        // Win名 → ModAssetBundle
        private readonly Dictionary<string, ModAssetBundle> _bundles = new Dictionary<string, ModAssetBundle>();
        // "bundle/prefab" → prefab GO (バンドルから取得済みの原本)
        private readonly Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();
        // poolKey → 非アクティブGOのキュー
        private readonly Dictionary<string, Queue<GameObject>> _pools = new Dictionary<string, Queue<GameObject>>();
        // poolKey → 最大プールサイズ
        private readonly Dictionary<string, int> _poolSizes = new Dictionary<string, int>();
        // poolKey → 生成済みGO総数(プール内+レンタル中)。上限チェックに使用
        private readonly Dictionary<string, int> _totalCounts = new Dictionary<string, int>();
        // レンタル中(アクティブ)のGO。ReturnAll 用
        private readonly List<GameObject> _rentedList = new List<GameObject>();
        // "bundle/asset" → 読み込み済みメッシュ/マテリアル/サウンド (破棄しない)
        private readonly Dictionary<string, Mesh>      _meshCache     = new Dictionary<string, Mesh>();
        private readonly Dictionary<string, Material>  _materialCache = new Dictionary<string, Material>();
        private readonly Dictionary<string, AudioClip> _soundCache    = new Dictionary<string, AudioClip>();

        // 非アクティブGOの格納先。ACMUcore の子として生成されるため DontDestroyOnLoad 不要。
        private Transform _poolRoot;

        void Awake()
        {
            var poolGo = new GameObject("[ACMu] EffectPool");
            poolGo.transform.SetParent(transform, false);
            _poolRoot = poolGo.transform;
        }

        // (bundle, prefab, poolSize) → 非アクティブGO (SetActive/位置設定は呼び出し元が行う)
        internal GameObject Rent(string bundle, string prefab, int poolSize)
        {
            if (string.IsNullOrEmpty(bundle) || string.IsNullOrEmpty(prefab) || prefab == "none")
                return null;

            string key = bundle + "/" + prefab;

            int cur;
            _poolSizes.TryGetValue(key, out cur);
            if (poolSize > cur) _poolSizes[key] = poolSize;

            Queue<GameObject> pool;
            if (!_pools.TryGetValue(key, out pool)) { pool = new Queue<GameObject>(); _pools[key] = pool; }

            GameObject go;
            if (pool.Count > 0)
            {
                go = pool.Dequeue();
            }
            else
            {
                int total;
                _totalCounts.TryGetValue(key, out total);
                int maxSize;
                _poolSizes.TryGetValue(key, out maxSize);

                // 上限到達: 負荷ゼロでスキップ(エフェクト枯渇 → エフェクト非表示。これが正しい挙動)
                if (total >= maxSize) return null;

                go = CreateEffectGo(key, bundle, prefab);
                if (go == null) return null;
                _totalCounts[key] = total + 1;
            }
            if (go == null) return null;

            _rentedList.Add(go);
            return go;
        }

        // poolKey で返却。EffectAutoReturn の ReturnCallback として渡す。
        internal void ReturnByKey(string poolKey, GameObject go)
        {
            if (go == null) return;
            go.transform.SetParent(_poolRoot, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.SetActive(false);
            _rentedList.Remove(go);

            int maxSize;
            if (!_poolSizes.TryGetValue(poolKey, out maxSize)) maxSize = 30;

            Queue<GameObject> pool;
            if (!_pools.TryGetValue(poolKey, out pool)) { pool = new Queue<GameObject>(); _pools[poolKey] = pool; }

            if (pool.Count < maxSize)
            {
                pool.Enqueue(go);
            }
            else
            {
                // プールが満杯: GO を破棄し総数カウントを戻す
                int t;
                _totalCounts.TryGetValue(poolKey, out t);
                if (t > 0) _totalCounts[poolKey] = t - 1;
                UnityEngine.Object.Destroy(go);
            }
        }

        // EffectRegistry.Return から呼ばれる (bundle, prefab) オーバーロード
        internal void Return(string bundle, string prefab, GameObject go)
        {
            ReturnByKey(bundle + "/" + prefab, go);
        }

        // シミュストップ時などに全アクティブエフェクトを即時非表示・プールへ返却
        internal void ReturnAll()
        {
            var arr = new GameObject[_rentedList.Count];
            _rentedList.CopyTo(arr);
            foreach (var go in arr)
            {
                if (go == null) continue;
                var ar = go.GetComponent<EffectAutoReturn>();
                ReturnByKey(ar != null ? ar.PoolKey : "", go);
            }
            _rentedList.Clear();
        }

        // ParticleSystem をリセット・再生してライフタイム計測開始
        internal void BeginLifetime(GameObject go)
        {
            if (go == null) return;
            var ar = go.GetComponent<EffectAutoReturn>();
            if (ar != null) ar.BeginLifetime();
        }

        // Trail/Bullet切り離し後: エミッションを止めて既存パーティクルが消えたらプールへ返す
        internal void Fade(GameObject go)
        {
            if (go == null) return;
            var ar = go.GetComponent<EffectAutoReturn>();
            if (ar != null) ar.BeginFade();
        }

        // Mesh をキャッシュして返す。
        // 試行順: ① ModResource.GetMesh (ACM互換) → ② AssetBundle raw Mesh → ③ AssetBundle GO prefab MeshFilter
        internal Mesh LoadMeshAsset(string bundle, string assetName)
        {
            if (string.IsNullOrEmpty(assetName)) return null;

            string modResKey = "modres:" + assetName;
            string bundleKey = bundle + "/" + assetName;
            Mesh cached;
            if (_meshCache.TryGetValue(modResKey, out cached)) return cached;
            if (!string.IsNullOrEmpty(bundle) && _meshCache.TryGetValue(bundleKey, out cached)) return cached;

            // ① ModResource.GetMesh — Mod.xml に <Mesh name="..." /> で登録されたリソース
            try
            {
                var modMesh = ModResource.GetMesh(assetName);
                if (modMesh != null && modMesh.Available)
                {
                    Mesh mesh = modMesh;
                    if (mesh != null)
                    {
                        _meshCache[modResKey] = mesh;
                        return mesh;
                    }
                }
            }
            catch { }

            // ② AssetBundle フォールバック
            if (string.IsNullOrEmpty(bundle)) return null;

            var mb = GetOrLoadBundle(bundle);
            if (mb == null)
            {
                Debug.LogWarning("[ACMu] LoadMesh: '" + assetName + "' not in ModResource, bundle '" + bundle + "' not found");
                return null;
            }
            try
            {
                var mesh = mb.LoadAsset<Mesh>(assetName);
                if (mesh != null) { _meshCache[bundleKey] = mesh; return mesh; }

                var go = mb.LoadAsset<GameObject>(assetName);
                if (go != null)
                {
                    var mf = go.GetComponentInChildren<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        _meshCache[bundleKey] = mf.sharedMesh;
                        return mf.sharedMesh;
                    }
                }

                Debug.LogWarning("[ACMu] LoadMesh: '" + assetName + "' not found in ModResource or bundle '" + bundle + "'");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[ACMu] LoadMesh: error loading '" + assetName + "': " + ex.Message);
                return null;
            }
        }

        // Material をキャッシュして返す。
        // 試行順: ① ModResource.GetTexture (ACM互換) → ② AssetBundle raw Material → ③ AssetBundle Texture2D → ④ AssetBundle GO MeshRenderer
        internal Material LoadMaterialFromTexture(string bundle, string textureName)
        {
            if (string.IsNullOrEmpty(textureName)) return null;

            string modResKey = "modres:" + textureName;
            string bundleKey = bundle + "/" + textureName;
            Material cached;
            if (_materialCache.TryGetValue(modResKey, out cached)) return cached;
            if (!string.IsNullOrEmpty(bundle) && _materialCache.TryGetValue(bundleKey, out cached)) return cached;

            // ① ModResource.GetTexture — Mod.xml に <Texture name="..." /> で登録されたリソース
            try
            {
                var modTex = ModResource.GetTexture(textureName);
                if (modTex != null && modTex.Available)
                {
                    Texture2D tex2d = modTex;
                    if (tex2d != null)
                    {
                        var shader = Shader.Find("Standard")
                                  ?? Shader.Find("Legacy Shaders/Diffuse")
                                  ?? Shader.Find("Unlit/Texture");
                        if (shader != null)
                        {
                            var mat = new Material(shader);
                            mat.mainTexture = tex2d;
                            _materialCache[modResKey] = mat;
                            return mat;
                        }
                    }
                }
            }
            catch { }

            // ② AssetBundle フォールバック
            if (string.IsNullOrEmpty(bundle)) return null;

            var mb = GetOrLoadBundle(bundle);
            if (mb == null)
            {
                Debug.LogWarning("[ACMu] LoadMaterial: '" + textureName + "' not in ModResource, bundle '" + bundle + "' not found");
                return null;
            }
            try
            {
                var mat = mb.LoadAsset<Material>(textureName);
                if (mat != null) { _materialCache[bundleKey] = mat; return mat; }

                var tex = mb.LoadAsset<Texture2D>(textureName);
                if (tex != null)
                {
                    var shader = Shader.Find("Standard")
                              ?? Shader.Find("Legacy Shaders/Diffuse")
                              ?? Shader.Find("Unlit/Texture");
                    if (shader != null)
                    {
                        var newMat = new Material(shader);
                        newMat.mainTexture = tex;
                        _materialCache[bundleKey] = newMat;
                        return newMat;
                    }
                }

                var go = mb.LoadAsset<GameObject>(textureName);
                if (go != null)
                {
                    var mr = go.GetComponentInChildren<MeshRenderer>();
                    if (mr != null && mr.sharedMaterial != null)
                    {
                        _materialCache[bundleKey] = mr.sharedMaterial;
                        return mr.sharedMaterial;
                    }
                }

                Debug.LogWarning("[ACMu] LoadMaterial: '" + textureName + "' not found in ModResource or bundle '" + bundle + "'");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[ACMu] LoadMaterial: error loading '" + textureName + "': " + ex.Message);
                return null;
            }
        }

        private GameObject CreateEffectGo(string key, string bundle, string prefab)
        {
            var prefabGo = LoadPrefab(bundle, prefab);
            if (prefabGo == null) return null;

            var go = (GameObject)UnityEngine.Object.Instantiate(prefabGo);
            go.transform.SetParent(_poolRoot, false);
            go.SetActive(false);

            var ar = go.AddComponent<EffectAutoReturn>();
            ar.PoolKey = key;
            ar.ReturnCallback = ReturnByKey;
            return go;
        }

        private GameObject LoadPrefab(string bundle, string prefab)
        {
            string key = bundle + "/" + prefab;
            GameObject cached;
            if (_prefabCache.TryGetValue(key, out cached)) return cached;

            var mb = GetOrLoadBundle(bundle);
            if (mb == null) return null;

            try
            {
                var go = mb.LoadAsset<GameObject>(prefab);
                if (go != null) _prefabCache[key] = go;
                return go;
            }
            catch { return null; }
        }

        // clipNames からランダムに1つ選んで position で再生する。ModResource.GetAudioClip を使用。
        internal void PlaySounds(System.Collections.Generic.List<string> clipNames, Vector3 position)
        {
            if (clipNames == null || clipNames.Count == 0) return;
            int idx = clipNames.Count == 1 ? 0 : UnityEngine.Random.Range(0, clipNames.Count);
            string name = clipNames[idx];
            AudioClip clip = LoadAudioClip(name);
            if (clip != null) AudioSource.PlayClipAtPoint(clip, position);
        }

        private AudioClip LoadAudioClip(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            AudioClip cached;
            if (_soundCache.TryGetValue(name, out cached)) return cached;

            try
            {
                var modClip = ModResource.GetAudioClip(name);
                if (modClip != null && modClip.Available)
                {
                    AudioClip clip = modClip;
                    if (clip != null) { _soundCache[name] = clip; return clip; }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[ACMu] LoadAudioClip: '" + name + "': " + ex.Message);
            }
            return null;
        }

        private ModAssetBundle GetOrLoadBundle(string resourceName)
        {
            ModAssetBundle bundle;
            if (_bundles.TryGetValue(resourceName, out bundle)) return bundle;

            bool isMacOrUnix = Application.platform == RuntimePlatform.OSXPlayer
                            || Application.platform == RuntimePlatform.LinuxPlayer;
            string loadName = isMacOrUnix ? resourceName + "Mac" : resourceName;

            try { bundle = ModResource.GetAssetBundle(loadName); }
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

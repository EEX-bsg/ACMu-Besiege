using System;
using System.Collections.Generic;
using UnityEngine;

namespace ACMu.Compat.Shooting
{
    // Bootstrap が SetFunctions/SetSoundFn でデリゲートを注入する。
    // Compat 層が Adapter 型を直接参照せずプール/ライフタイム管理・アセット読み込みを委譲できる。
    internal static class EffectRegistry
    {
        private static Func<string, string, int, GameObject> _rentFn;
        private static Action<string, string, GameObject>    _returnFn;
        private static Action                                _returnAllFn;
        private static Action<GameObject>                    _beginLifetimeFn;
        private static Action<GameObject>                    _fadeFn;
        private static Func<string, string, Mesh>            _loadMeshFn;
        private static Func<string, string, Material>        _loadMaterialFn;
        private static Action<List<string>, Vector3>         _playSoundsFn;

        internal static void SetFunctions(
            Func<string, string, int, GameObject> rentFn,
            Action<string, string, GameObject>    returnFn,
            Action                                returnAllFn,
            Action<GameObject>                    beginLifetimeFn,
            Action<GameObject>                    fadeFn,
            Func<string, string, Mesh>            loadMeshFn,
            Func<string, string, Material>        loadMaterialFn)
        {
            _rentFn          = rentFn;
            _returnFn        = returnFn;
            _returnAllFn     = returnAllFn;
            _beginLifetimeFn = beginLifetimeFn;
            _fadeFn          = fadeFn;
            _loadMeshFn      = loadMeshFn;
            _loadMaterialFn  = loadMaterialFn;
        }

        internal static void SetSoundFn(Action<List<string>, Vector3> fn)
        {
            _playSoundsFn = fn;
        }

        // GO をプールからレンタルして pos/rot に配置・アクティブ化する。
        // autoReturn=true なら BeginLifetime を呼んで PS完了後にプールへ自動返却する。
        // bundleName/effectName が空/"none" の場合は null を返す。
        internal static GameObject Spawn(string bundleName, string effectName, Vector3 pos, Quaternion rot, int poolSize, bool autoReturn)
        {
            if (_rentFn == null) return null;
            if (string.IsNullOrEmpty(bundleName) || string.IsNullOrEmpty(effectName) || effectName == "none")
                return null;

            GameObject go;
            try { go = _rentFn(bundleName, effectName, poolSize); }
            catch { return null; }
            if (go == null) return null;

            go.transform.position = pos;
            go.transform.rotation = rot;
            go.SetActive(true);

            if (autoReturn && _beginLifetimeFn != null)
                _beginLifetimeFn(go);

            return go;
        }

        // 即時返却(着弾時の弾体エフェクト消去など)
        internal static void Return(string bundleName, string effectName, GameObject go)
        {
            if (_returnFn == null || go == null) return;
            try { _returnFn(bundleName, effectName, go); }
            catch { }
        }

        // 既にアクティブな GO の PS をリスタートして完了後にプールへ返却させる
        internal static void BeginLifetime(GameObject go)
        {
            if (_beginLifetimeFn == null || go == null) return;
            try { _beginLifetimeFn(go); }
            catch { }
        }

        // シミュ停止時などに全アクティブエフェクトを即時非表示にする
        internal static void ReturnAll()
        {
            if (_returnAllFn == null) return;
            try { _returnAllFn(); }
            catch { }
        }

        // Trail/Bullet切り離し後: エミッションを止めて既存パーティクルが消えたらプールへ返す
        internal static void Fade(GameObject go)
        {
            if (_fadeFn == null || go == null) return;
            try { _fadeFn(go); }
            catch { }
        }

        // Mesh アセットを返す。ModResource優先、なければ bundle 内を検索。assetName が空/"none" は null を返す。
        internal static Mesh LoadMesh(string bundle, string assetName)
        {
            if (_loadMeshFn == null) return null;
            if (string.IsNullOrEmpty(assetName) || assetName == "none") return null;
            try { return _loadMeshFn(bundle, assetName); }
            catch { return null; }
        }

        // Material/Texture アセットを返す。ModResource優先、なければ bundle 内を検索。
        internal static Material LoadMaterial(string bundle, string textureName)
        {
            if (_loadMaterialFn == null) return null;
            if (string.IsNullOrEmpty(textureName) || textureName == "none") return null;
            try { return _loadMaterialFn(bundle, textureName); }
            catch { return null; }
        }

        // 名前リストからランダムに1つを選択して position で再生する。リストが空ならスキップ。
        internal static void PlaySounds(List<string> clipNames, Vector3 position)
        {
            if (_playSoundsFn == null || clipNames == null || clipNames.Count == 0) return;
            try { _playSoundsFn(clipNames, position); }
            catch { }
        }
    }
}

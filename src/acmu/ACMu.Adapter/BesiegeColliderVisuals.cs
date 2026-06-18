using UnityEngine;

namespace ACMu.Adapter
{
    // Besiege の Resources にある格子メッシュプレハブと透過マテリアルを提供する。
    // BlockLoader (InternalModding.Blocks) はサンドボックスで禁止されているため、
    // Resources.Load と Shader.Find でオリジナルと同等の素材を再現する。
    internal static class BesiegeColliderVisuals
    {
        private static Material   _debugMaterial;
        private static GameObject _gridSphere;

        internal static Material GetDebugMaterial()
        {
            if (_debugMaterial != null) return _debugMaterial;

            Shader shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
            if (shader == null) shader = Shader.Find("Transparent/Diffuse");
            if (shader == null)
            {
                Debug.LogWarning("[ACMu] BesiegeColliderVisuals: transparent shader not found");
                return null;
            }

            _debugMaterial       = new Material(shader);
            _debugMaterial.color = new Color(0f, 0.8f, 1f, 0.25f);
            return _debugMaterial;
        }

        internal static GameObject GetGridSpherePrefab()
        {
            if (_gridSphere != null) return _gridSphere;
            _gridSphere = Resources.Load<GameObject>("Modding/GridSphere");
            return _gridSphere;
        }
    }
}

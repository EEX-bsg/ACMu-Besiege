using UnityEngine;

namespace ACMu.Weapons
{
    // プロジェクタイルGOの見た目を一元管理する。
    // プール用の素体プリミティブ(球)の MeshRenderer は捕捉して常に無効化し、
    // カスタムメッシュが読み込めた場合のみ子GOに描画する。
    // メッシュが無い・読み込み失敗時は何も描画しない。素体の球がそのまま飛ぶ
    // 「球フォールバック」は禁止仕様であり、ここでは決して素体を再表示しない。
    internal sealed class ProjectileMeshRestorer : MonoBehaviour
    {
        private MeshRenderer _rootRenderer;
        private bool _captured;
        private GameObject _childGo;

        // mesh が null の場合は何も描画しない(球フォールバックしない)。
        internal void Apply(Mesh mesh, Material material, Vector3 offset, Quaternion rotation, Vector3 scale)
        {
            // 初回: 素体(球)の MeshRenderer を捕捉し、以後この弾体では常に無効のまま扱う。
            if (!_captured)
            {
                _rootRenderer = GetComponent<MeshRenderer>();
                _captured = true;
            }
            if (_rootRenderer != null) _rootRenderer.enabled = false;

            if (mesh == null)
            {
                // カスタムメッシュ無し/読み込み失敗: 何も表示しない。
                if (_childGo != null) _childGo.SetActive(false);
                return;
            }

            if (_childGo == null)
            {
                _childGo = new GameObject("[ACMu]ProjMesh");
                _childGo.transform.SetParent(transform, false);
                _childGo.AddComponent<MeshFilter>();
                _childGo.AddComponent<MeshRenderer>();
            }

            _childGo.transform.localPosition = offset;
            _childGo.transform.localRotation = rotation;
            _childGo.transform.localScale    = (scale == Vector3.zero) ? Vector3.one : scale;

            var cmf = _childGo.GetComponent<MeshFilter>();
            var cmr = _childGo.GetComponent<MeshRenderer>();
            if (cmf != null) cmf.sharedMesh = mesh;
            if (cmr != null && material != null) cmr.sharedMaterial = material;
            _childGo.SetActive(true);
        }

        void OnDisable()
        {
            // プール返却時: 子メッシュを隠す。素体の球は決して復元しない(常に無効のまま)。
            if (_childGo != null) _childGo.SetActive(false);
            if (_rootRenderer != null) _rootRenderer.enabled = false;
        }
    }
}

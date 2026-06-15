using UnityEngine;

namespace ACMu.Weapons
{
    // プロジェクタイルGOに付けるコンポーネント。
    // 武装ごとのカスタムメッシュ/マテリアルを適用し、プール返却時(SetActive(false) → OnDisable)に
    // 元の状態を自動復元する。これにより次の武装が意図しない外観を引き継がない。
    // offset/rotation/scale が非同一の場合は子GOを作成してそこにメッシュを配置し、
    // ルートのMeshRendererを非表示にする。
    internal sealed class ProjectileMeshRestorer : MonoBehaviour
    {
        private Mesh     _originalMesh;
        private Material _originalMaterial;
        private bool     _rootModified;

        private GameObject _childGo;

        internal void Apply(Mesh mesh, Material material, Vector3 offset, Quaternion rotation, Vector3 scale)
        {
            if (mesh == null && material == null) return;

            bool isIdentity = offset == Vector3.zero
                           && rotation == Quaternion.identity
                           && (scale == Vector3.one || scale == Vector3.zero);

            if (!isIdentity || _childGo != null)
            {
                // 子GO アプローチ: ルートMeshRendererを隠して子GOにメッシュを置く
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
                if (cmf != null && mesh     != null) cmf.sharedMesh    = mesh;
                if (cmr != null && material != null) cmr.sharedMaterial = material;
                _childGo.SetActive(true);

                // ルート MeshRenderer を隠す
                var rootMr = GetComponent<MeshRenderer>();
                if (rootMr != null) rootMr.enabled = false;
            }
            else
            {
                // ルートGO直接変更
                var mf = GetComponent<MeshFilter>();
                var mr = GetComponent<MeshRenderer>();

                if (!_rootModified)
                {
                    _originalMesh     = mf != null ? mf.sharedMesh    : null;
                    _originalMaterial = mr != null ? mr.sharedMaterial : null;
                    _rootModified = true;
                }
                if (mf != null && mesh     != null) mf.sharedMesh    = mesh;
                if (mr != null && material != null) mr.sharedMaterial = material;
            }
        }

        void OnDisable()
        {
            // 子GO を非表示にしてルート MeshRenderer を復元
            if (_childGo != null)
            {
                _childGo.SetActive(false);
                var rootMr = GetComponent<MeshRenderer>();
                if (rootMr != null) rootMr.enabled = true;
            }

            // ルート直接変更の復元
            if (_rootModified)
            {
                var mf = GetComponent<MeshFilter>();
                var mr = GetComponent<MeshRenderer>();
                if (mf != null) mf.sharedMesh    = _originalMesh;
                if (mr != null) mr.sharedMaterial = _originalMaterial;
                _rootModified = false;
            }
        }
    }
}

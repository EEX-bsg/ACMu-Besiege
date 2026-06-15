using UnityEngine;

namespace ACMu.Weapons
{
    // プロジェクタイルGOに付けるコンポーネント。
    // 武装ごとのカスタムメッシュ/マテリアルを適用し、プール返却時(SetActive(false) → OnDisable)に
    // 元のメッシュ/マテリアルを自動復元する。これにより次の武装が意図しない外観を引き継がない。
    internal sealed class ProjectileMeshRestorer : MonoBehaviour
    {
        private Mesh     _originalMesh;
        private Material _originalMaterial;
        private bool     _modified;

        // 呼び出し元が取得した Mesh/Material を適用する。null は「変更しない」。
        internal void Apply(Mesh mesh, Material material)
        {
            if (mesh == null && material == null) return;

            var mf = GetComponent<MeshFilter>();
            var mr = GetComponent<MeshRenderer>();

            if (!_modified)
            {
                _originalMesh     = mf != null ? mf.sharedMesh    : null;
                _originalMaterial = mr != null ? mr.sharedMaterial : null;
                _modified         = true;
            }

            if (mf != null && mesh     != null) mf.sharedMesh    = mesh;
            if (mr != null && material != null) mr.sharedMaterial = material;
        }

        void OnDisable()
        {
            if (!_modified) return;
            var mf = GetComponent<MeshFilter>();
            var mr = GetComponent<MeshRenderer>();
            if (mf != null) mf.sharedMesh    = _originalMesh;
            if (mr != null) mr.sharedMaterial = _originalMaterial;
            _modified = false;
        }
    }
}

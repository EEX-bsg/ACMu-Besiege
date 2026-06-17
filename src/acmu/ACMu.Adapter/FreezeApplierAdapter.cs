using UnityEngine;

namespace ACMu.Adapter
{
    // 命中対象(とその子ブロック)を凍結する。Bootstrap から FreezeRegistry へ静的メソッドを渡す。
    // BlockBehaviour / IceTag / BlockPrefab は本体デコンパイル型のため Adapter 内のみ。
    public static class FreezeApplierAdapter
    {
        public static void ApplyFreeze(GameObject hitObject)
        {
            if (hitObject == null) return;

            var bb = hitObject.GetComponent<BlockBehaviour>()
                  ?? hitObject.GetComponentInParent<BlockBehaviour>();
            if (bb == null) return;

            if (bb.gotChildBlocks)
            {
                bb.CreateSimLists();
                foreach (var child in bb.parentedColliders.Keys)
                {
                    if (child != null && child.Prefab != null && child.Prefab.canFreeze && child.iceTag != null)
                        child.iceTag.Freeze();
                }
            }

            if (bb.Prefab != null && bb.Prefab.canFreeze && bb.iceTag != null)
                bb.iceTag.Freeze();
        }
    }
}

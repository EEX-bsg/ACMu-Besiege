using System;
using UnityEngine;

namespace ACMu.Compat.Shooting
{
    // Bootstrap が SetApplyFn でデリゲートを注入する。
    // Compat 層が Adapter 型を直接参照せず凍結適用を委譲できる。
    internal static class FreezeRegistry
    {
        private static Action<GameObject> _applyFn;

        internal static void SetApplyFn(Action<GameObject> fn)
        {
            _applyFn = fn;
        }

        // hitObject(とその子ブロック群)を凍結する。canFreeze=false のブロックは Adapter 側で無視される。
        internal static void Apply(GameObject hitObject)
        {
            if (_applyFn == null || hitObject == null) return;
            try { _applyFn(hitObject); }
            catch { }
        }
    }
}

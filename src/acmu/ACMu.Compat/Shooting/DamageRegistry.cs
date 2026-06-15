using System;
using UnityEngine;

namespace ACMu.Compat.Shooting
{
    // Bootstrap が SetApplyFn でデリゲートを注入する。
    // Compat 層が Adapter 型を直接参照せずダメージ適用を委譲できる。
    internal static class DamageRegistry
    {
        private static Action<GameObject, float, float> _applyFn;

        internal static void SetApplyFn(Action<GameObject, float, float> fn)
        {
            _applyFn = fn;
        }

        // hitObject にブロックダメージ / エンティティダメージを適用する。
        // いずれか一方が 0 以下なら対象種別への適用をスキップする。
        internal static void Apply(GameObject hitObject, float blockDamage, float entityDamage)
        {
            if (_applyFn == null || hitObject == null) return;
            try { _applyFn(hitObject, blockDamage, entityDamage); }
            catch { }
        }
    }
}

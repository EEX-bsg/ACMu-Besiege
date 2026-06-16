using System;

namespace ACMu.Compat.Shooting
{
    // Bootstrap が SetInfiniteAmmoFn でデリゲートを注入する。
    // Compat 層が StatMaster を直接参照せず GodMode ルールを確認できる。
    internal static class GameRulesRegistry
    {
        private static Func<bool> _isInfiniteAmmoFn;

        internal static void SetInfiniteAmmoFn(Func<bool> fn)
        {
            _isInfiniteAmmoFn = fn;
        }

        internal static bool IsInfiniteAmmo()
        {
            if (_isInfiniteAmmoFn == null) return false;
            try { return _isInfiniteAmmoFn(); }
            catch { return false; }
        }
    }
}

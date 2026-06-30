using System;
using System.Collections.Generic;
using ACMu.PluginApi;

namespace ACMu.Weapons
{
    internal static class WeaponHostRegistry
    {
        private static readonly Dictionary<Type, WeaponRegistration> _registrations =
            new Dictionary<Type, WeaponRegistration>();

        internal static void Register(Type moduleType, WeaponRegistration registration)
        {
            _registrations[moduleType] = registration;
        }

        internal static WeaponRegistration Get(Type moduleType)
        {
            WeaponRegistration reg;
            _registrations.TryGetValue(moduleType, out reg);
            return reg;
        }

        internal static bool Contains(Type moduleType)
        {
            return _registrations.ContainsKey(moduleType);
        }

        // ModuleRegistrar(Besiege 登録)が失敗したときのロールバック用。
        // factory だけ登録済みで Besiege 未登録という不整合を残さないため Register 側から呼ぶ。
        internal static void Remove(Type moduleType)
        {
            _registrations.Remove(moduleType);
        }
    }
}

using ACMu.PluginApi;
using Modding.Modules;
using UnityEngine;

namespace ACMu.Host.Null
{
    internal class NullWeaponRegistry : IWeaponRegistry
    {
        public void Register<TModule>(WeaponRegistration registration)
            where TModule : BlockModule, new()
        {
            Debug.LogWarning("[ACMu] WeaponRegistry not initialized (M2). Registration ignored: " + (registration != null ? registration.ModuleName : "null"));
        }
    }
}

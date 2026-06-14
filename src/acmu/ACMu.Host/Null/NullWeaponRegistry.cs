using ACMu.PluginApi;
using Modding.Modules;
using UnityEngine;

namespace ACMu.Host.Null
{
    internal class NullWeaponRegistry : IWeaponRegistry
    {
        public void Register<TModule, TBehaviour>(WeaponRegistration registration)
            where TModule : BlockModule, new()
            where TBehaviour : BlockModuleBehaviour<TModule>
        {
            Debug.LogWarning("[ACMu] WeaponRegistry not initialized (M2). Registration ignored: " + (registration != null ? registration.ModuleName : "null"));
        }
    }
}

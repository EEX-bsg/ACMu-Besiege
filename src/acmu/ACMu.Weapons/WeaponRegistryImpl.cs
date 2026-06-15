using System;
using ACMu.PluginApi;
using Modding.Modules;

namespace ACMu.Weapons
{
    public class WeaponRegistryImpl : IWeaponRegistry
    {
        public void Register<TModule, TBehaviour>(WeaponRegistration registration)
            where TModule : BlockModule, new()
            where TBehaviour : BlockModuleBehaviour<TModule>
        {
            if (registration == null)
                throw new ArgumentNullException("registration");
            if (string.IsNullOrEmpty(registration.ModuleName))
                throw new ArgumentException("ModuleName must not be empty");
            if (registration.WeaponFactory == null)
                throw new ArgumentException("WeaponFactory must not be null");
            if (WeaponHostRegistry.Contains(typeof(TModule)))
                throw new InvalidOperationException(
                    "[ACMu] WeaponRegistry: duplicate ModuleName: " + registration.ModuleName);

            WeaponHostRegistry.Register(typeof(TModule), registration);
            CustomModules.AddBlockModule<TModule, TBehaviour>(registration.ModuleName, registration.MultiplayerCompatible);
        }
    }
}

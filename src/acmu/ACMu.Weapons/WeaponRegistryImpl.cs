using System;
using ACMu.PluginApi;
using Modding.Modules;

namespace ACMu.Weapons
{
    public class WeaponRegistryImpl : IWeaponRegistry
    {
        public void Register<TModule>(WeaponRegistration registration) where TModule : BlockModule, new()
        {
            if (registration == null)
                throw new ArgumentNullException("registration");
            if (string.IsNullOrEmpty(registration.ModuleName))
                throw new ArgumentException("ModuleName must not be empty");
            if (registration.WeaponFactory == null)
                throw new ArgumentException("WeaponFactory must not be null");
            if (WeaponHostRegistry.Contains(typeof(TModule)))
                throw new InvalidOperationException(
                    "[ACMu] WeaponRegistry: duplicate registration for " + typeof(TModule).Name);

            WeaponHostRegistry.Register(typeof(TModule), registration);
            CustomModules.AddBlockModule<TModule, WeaponHostBehaviour<TModule>>(
                registration.ModuleName, registration.MultiplayerCompatible);
        }
    }
}

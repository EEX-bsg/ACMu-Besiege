using System;
using ACMu.PluginApi;
using Modding.Modules;

namespace ACMu.Weapons
{
    public class WeaponRegistryImpl : IWeaponRegistry
    {
        public void Register<TModule>(WeaponRegistration registration)
            where TModule : BlockModule, new()
        {
            if (registration == null)
                throw new ArgumentNullException("registration");
            if (string.IsNullOrEmpty(registration.ModuleName))
                throw new ArgumentException("ModuleName must not be empty");
            if (registration.WeaponFactory == null)
                throw new ArgumentException("WeaponFactory must not be null");
            if (registration.ModuleRegistrar == null)
                throw new ArgumentException("ModuleRegistrar must not be null");
            if (WeaponHostRegistry.Contains(typeof(TModule)))
                throw new InvalidOperationException(
                    "[ACMu] WeaponRegistry: duplicate module type: " + registration.ModuleName);

            // factory を先に格納してから Besiege 登録(ModuleRegistrar)を invoke する。
            // AddBlockModule の呼び出しはプラグインのデリゲート本体に置く: GetCallingAssembly() が
            // その本体の所属アセンブリを Mod 名義にするため、ここ(acmu.dll)から代理で呼ぶと
            // 全武装が ACMu 名義になってしまう。詳細は WeaponRegistration.ModuleRegistrar 参照。
            // ModuleRegistrar が例外を投げた場合は factory エントリを巻き戻し、
            // 「factory はあるが Besiege 未登録」という中途半端な状態を残さない。
            WeaponHostRegistry.Register(typeof(TModule), registration);
            try
            {
                registration.ModuleRegistrar();
            }
            catch
            {
                WeaponHostRegistry.Remove(typeof(TModule));
                throw;
            }
        }
    }
}

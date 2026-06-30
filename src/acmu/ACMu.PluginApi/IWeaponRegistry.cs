using Modding.Modules;

namespace ACMu.PluginApi
{
    /// <summary>
    /// 武装プラグインの登録窓口。Reflection 禁止環境のため、発見は走査ではなく本契約への明示登録で行う。
    /// <para>不変条件: 同一 TModule の二重登録は例外。登録順はブロックID採番に影響しうるため、
    /// プラグインは自 Mod 内で登録順を固定すること。</para>
    /// <para>呼び出しタイミング: 自 Mod の ModEntryPoint.OnLoad 中のみ(ACMu ロード済みであること。
    /// Mods.IsModLoaded でガードし、未ロード時は機能を無効化するのが利用側の責務)。</para>
    /// <para>スレッド制約: Unity メインスレッドのみ。</para>
    /// </summary>
    public interface IWeaponRegistry
    {
        /// <summary>
        /// 武装モジュールを登録する。
        /// <para>TModule: XML モジュールクラス([XmlRoot] が registration.ModuleName と一致する BlockModule 派生)。</para>
        /// <para>Besiege への <c>CustomModules.AddBlockModule</c> 呼び出しは ACMu 側では行わず、
        /// <see cref="WeaponRegistration.ModuleRegistrar"/> 内のプラグインコードに委ねる。
        /// これは AddBlockModule が GetCallingAssembly() で Mod 名義を決めるため、
        /// 名義を「武装を提供したプラグイン自身」にするにはプラグインのアセンブリから呼ぶ必要があるから。
        /// (旧設計のように ACMu が代理で呼ぶと全武装が ACMu 名義になり、MP の Mod 不一致検出・
        /// セーブ可搬性が壊れる。)その結果 TBehaviour は本シグネチャから不要になっている。</para>
        /// </summary>
        void Register<TModule>(WeaponRegistration registration)
            where TModule : BlockModule, new();
    }
}

using Modding.Modules;

namespace ACMu.PluginApi
{
    /// <summary>
    /// 武装プラグインの登録窓口。Reflection 禁止環境のため、発見は走査ではなく本契約への明示登録で行う。
    /// 内部で CustomModules.AddBlockModule&lt;TModule, (ACMu 提供ホスト Behaviour)&gt; を呼び、
    /// プラグインは BlockModuleBehaviour を自作しなくてよい(UC-1 の継承難所の解消)。
    /// <para>不変条件: 同一 TModule / 同一 ModuleName の二重登録は例外。登録順はブロックID採番に影響しうるため、
    /// プラグインは自 Mod 内で登録順を固定すること。</para>
    /// <para>呼び出しタイミング: 自 Mod の ModEntryPoint.OnLoad 中のみ(ACMu ロード済みであること。
    /// Mods.IsModLoaded でガードし、未ロード時は機能を無効化するのが利用側の責務)。</para>
    /// <para>スレッド制約: Unity メインスレッドのみ。</para>
    /// </summary>
    public interface IWeaponRegistry
    {
        /// <summary>
        /// 武装モジュールを登録する。TModule はプラグインが定義する XML モジュール
        /// ([XmlRoot] が registration.ModuleName と一致する BlockModule 派生)。
        /// </summary>
        void Register<TModule>(WeaponRegistration registration) where TModule : BlockModule, new();
    }
}

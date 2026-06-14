using Modding.Modules;

namespace ACMu.PluginApi
{
    /// <summary>
    /// 武装プラグインの登録窓口。Reflection 禁止環境のため、発見は走査ではなく本契約への明示登録で行う。
    /// <para>不変条件: 同一 TModule / 同一 ModuleName の二重登録は例外。登録順はブロックID採番に影響しうるため、
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
        /// <para>TBehaviour: WeaponHostBehaviour&lt;TModule&gt; の具象非汎用サブクラス。
        /// Unity 5.4 は AddComponent(Type) に汎用型を渡せないため、プラグインごとに
        /// <c>class MyBehaviour : WeaponHostBehaviour&lt;MyModule&gt; {}</c> を宣言して渡すこと。
        /// 制約はコンパイル上 BlockModuleBehaviour&lt;TModule&gt; だが、実際には WeaponHostBehaviour 派生のみ動作する。</para>
        /// </summary>
        void Register<TModule, TBehaviour>(WeaponRegistration registration)
            where TModule : BlockModule, new()
            where TBehaviour : BlockModuleBehaviour<TModule>;
    }
}

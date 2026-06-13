using ACMu.Core;

namespace ACMu.PluginApi
{
    /// <summary>
    /// 他 Mod から見た ACMu の公開面の根。ACMUcore GameObject 上のコンポーネントとして実装され、
    /// GameObject.Find(WellKnownNames.CoreObjectName) → GetComponent で取得する(Reflection 不使用)。
    /// <para>不変条件: ApiVersion はセマンティックバージョニングのメジャー値。破壊的変更時のみ増える。</para>
    /// <para>呼び出しタイミング: ACMu の OnLoad 完了後(Mods.OnModLoaded 以降)に取得すること。</para>
    /// <para>スレッド制約: Unity メインスレッドのみ。</para>
    /// </summary>
    public interface IAcmuPluginHost
    {
        int ApiVersion { get; }
        IAcmuServices Services { get; }
        IWeaponRegistry Weapons { get; }
    }
}

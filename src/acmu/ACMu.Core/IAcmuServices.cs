using ACMu.Core.Config;
using ACMu.Core.Game;
using ACMu.Core.Logging;
using ACMu.Core.Net;
using ACMu.Core.Weapons;
using ACMu.Core.World;

namespace ACMu.Core
{
    /// <summary>
    /// ACMu が内部モジュールおよび他 Mod へ公開するサービス集約点。
    /// 実装は ACMUcore GameObject 上のコンポーネントが提供し、各サービスへの参照を保持する。
    /// <para>不変条件: 全プロパティは OnModLoad 完了後 null を返さない。シーン遷移をまたいで同一インスタンスを返す。</para>
    /// <para>呼び出しタイミング: 取得は OnModLoad 完了後(他 Mod からは Mods.OnModLoaded 以降)。</para>
    /// <para>スレッド制約: Unity メインスレッドのみ(ILog を除く)。</para>
    /// </summary>
    public interface IAcmuServices
    {
        ILog Log { get; }
        IGameSessionInfo Session { get; }
        IGameEventSource GameEvents { get; }

        /// <summary>ブロック GameObject から IBlockAccessor を生成するファクトリ。</summary>
        IBlockAccessorFactory Blocks { get; }

        IConfigStore Config { get; }
        INetworkTransport Network { get; }
        IWorldFrame World { get; }
        IProjectileService Projectiles { get; }
    }
}

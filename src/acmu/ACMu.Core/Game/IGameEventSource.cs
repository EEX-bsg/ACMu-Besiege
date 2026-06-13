using System;

namespace ACMu.Core.Game
{
    /// <summary>
    /// 本体イベント(Modding.Events 等)の再公開契約。購読側は本体イベントを直接購読しない。
    /// <para>不変条件: イベント発火順は本体の発火順を保存する。ハンドラ内の例外は実装側が捕捉し、
    /// 他のハンドラの実行を妨げないこと。</para>
    /// <para>呼び出しタイミング: 購読は OnModLoad 中に行うこと。解除は任意。</para>
    /// <para>スレッド制約: 全イベントは Unity メインスレッドで発火する。</para>
    /// </summary>
    public interface IGameEventSource
    {
        /// <summary>シミュレーション切替。引数 true = 開始, false = 終了。</summary>
        event Action<bool> SimulationToggled;

        /// <summary>ブロック初期化完了。シミュレーション側インスタンスの生成後に発火する。</summary>
        event Action<IBlockAccessor> BlockInitialized;

        event Action<int> PlayerJoined;
        event Action<int> PlayerLeft;
        event Action LevelLoaded;
    }
}

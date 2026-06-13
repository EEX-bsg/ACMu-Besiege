namespace ACMu.Core.Lifecycle
{
    /// <summary>
    /// ACMUcore 配下の各サービスが実装するライフサイクル契約。
    /// Host 層のコーディネータが InitOrder 昇順で各メソッドを呼ぶ。
    /// <para>不変条件: 各メソッドは冪等であること(同フェーズの重複呼び出しで状態が壊れない)。
    /// OnModLoad では登録系処理のみを行い、シーン上のオブジェクトへ触れてはならない。</para>
    /// <para>呼び出しタイミング: OnModLoad は ModEntryPoint.OnLoad 中に1回。
    /// OnSimulationStart / OnSimulationStop はシミュレーション切替ごとに対で呼ばれる。</para>
    /// <para>スレッド制約: 全メソッドは Unity メインスレッドからのみ呼ばれる。</para>
    /// </summary>
    public interface ILifecycleParticipant
    {
        /// <summary>初期化順。小さいほど先に初期化される。Adapter=0, Net=100, World=200, Weapons=300, Compat=400 を目安とする。</summary>
        int InitOrder { get; }

        void OnModLoad();
        void OnSimulationStart(bool isMultiplayer);
        void OnSimulationStop();
    }
}

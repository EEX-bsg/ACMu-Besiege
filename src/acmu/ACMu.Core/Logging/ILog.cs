namespace ACMu.Core.Logging
{
    /// <summary>
    /// ログ出力の抽象。実装は ModConsole / UnityEngine.Debug への委譲を想定する。
    /// <para>不変条件: いずれのメソッドも例外を外へ送出してはならない(ログ失敗は黙って握り潰す)。</para>
    /// <para>呼び出しタイミング: 任意のフェーズで呼び出し可。</para>
    /// <para>スレッド制約: 全メソッドはスレッドセーフであること(ワーカースレッドからの呼び出しを許容する唯一の契約)。</para>
    /// </summary>
    public interface ILog
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message);
        void Error(string message, System.Exception exception);
    }
}

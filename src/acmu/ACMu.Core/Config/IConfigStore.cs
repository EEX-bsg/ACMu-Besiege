namespace ACMu.Core.Config
{
    /// <summary>
    /// 設定永続化の契約。実装は Modding.ModIO による Mod フォルダ内 XML を想定する(System.IO の直接使用は禁止環境)。
    /// <para>不変条件: LoadOrCreate は失敗時に例外を送出せず、既定値インスタンス(new T())を返して続行する
    /// (フェイルソフト方針)。同一 fileName に対する Save → LoadOrCreate は保存値を返すこと。</para>
    /// <para>呼び出しタイミング: OnModLoad 以降の任意。フレーム毎の呼び出しは禁止(I/O コスト)。</para>
    /// <para>スレッド制約: Unity メインスレッドのみ。</para>
    /// </summary>
    public interface IConfigStore
    {
        T LoadOrCreate<T>(string fileName) where T : class, new();
        void Save<T>(string fileName, T value) where T : class;
    }
}

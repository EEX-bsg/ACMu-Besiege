namespace ACMu.Core
{
    /// <summary>
    /// Reflection 禁止環境で他 Mod が ACMu を発見するための既知名。
    /// 他 Mod は GameObject.Find(CoreObjectName) → GetComponent で公開面(IAcmuServices 実装)へ到達する。
    /// </summary>
    public static class WellKnownNames
    {
        /// <summary>ACMu の管理 GameObject 名(DontDestroyOnLoad)。</summary>
        public const string CoreObjectName = "ACMUcore";
    }
}

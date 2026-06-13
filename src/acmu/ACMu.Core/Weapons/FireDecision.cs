namespace ACMu.Core.Weapons
{
    /// <summary>OnValidateFire の判定結果。</summary>
    public enum FireDecision : byte
    {
        /// <summary>発射を続行する。</summary>
        Proceed = 0,
        /// <summary>今回の発射を抑止する(クールダウンは消費しない)。</summary>
        Suppress = 1
    }
}

namespace ACMu.Core.Weapons
{
    /// <summary>弾体消滅の理由。</summary>
    public enum DespawnReason : byte
    {
        Impact = 0,
        Timeout = 1,
        OwnerDestroyed = 2,
        Manual = 3,
        /// <summary>同期補正による強制消滅(ホストに存在しない弾の掃除等)。</summary>
        NetworkCorrection = 4,
        SimulationStopped = 5
    }
}

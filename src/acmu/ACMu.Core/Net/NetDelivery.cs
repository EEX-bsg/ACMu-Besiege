namespace ACMu.Core.Net
{
    /// <summary>配送品質の要求。実装が下位(ModNetworking)でどう実現するかは本契約の関知外。</summary>
    public enum NetDelivery : byte
    {
        /// <summary>到達保証あり。スポーン/消滅などのイベントに用いる。</summary>
        Reliable = 0,
        /// <summary>到達保証なし。座標スナップショットなど最新値のみ意味を持つデータに用いる。</summary>
        Unreliable = 1
    }
}

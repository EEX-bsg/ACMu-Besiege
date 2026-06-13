namespace ACMu.Core.Weapons
{
    /// <summary>
    /// 武装の数値仕様。XML モジュール既定値を起点に、プラグインが書き換え可能な可変データ。
    /// 「元のブロックの情報を書き換える」拡張は本クラスの値変更として表現する。
    /// <para>不変条件: 共有インスタンス(IWeaponHost.BaseSpec)の変更は以後の全ショットへ反映される。
    /// ショット単体の変更は FireContext.Shot(Clone 済みコピー)に対して行うこと。</para>
    /// <para>スレッド制約: Unity メインスレッドのみ。</para>
    /// </summary>
    public class WeaponSpec
    {
        /// <summary>ブロック・構造物への基本ダメージ。</summary>
        public float Damage = 0f;

        /// <summary>プレイヤー(コア)への追加ダメージ。</summary>
        public float PlayerDamage = 0f;

        public float MuzzleVelocity = 100f;
        public float FireIntervalSeconds = 0.5f;
        public float ExplosionRadius = 0f;
        public float SpreadDegrees = 0f;

        /// <summary>弾倉サイズ。-1 は無限。</summary>
        public int MagazineSize = -1;

        public float ReloadSeconds = 0f;

        /// <summary>弾体寿命(秒)。0 以下は実装既定値。</summary>
        public float ProjectileLifetimeSeconds = 0f;

        /// <summary>IProjectileService に登録した弾体キー。null は弾体を生成しない武装(セイバー等)。</summary>
        public string ProjectileKey = null;

        public WeaponSpec Clone()
        {
            return (WeaponSpec)MemberwiseClone();
        }
    }
}

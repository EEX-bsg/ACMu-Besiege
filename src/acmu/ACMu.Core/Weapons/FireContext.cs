using UnityEngine;

namespace ACMu.Core.Weapons
{
    /// <summary>
    /// 1回の発射要求の可変コンテキスト。OnValidateFire / OnBeforeFire / OnAfterFire の間で共有される。
    /// プラグインは本オブジェクトの書き換えによって発射へ介入する(遅延・弾速変更・誘導差し替え等)。
    /// <para>不変条件: Host / Shot は null にならない。Shot は BaseSpec の Clone であり、変更はこのショットにのみ作用する。
    /// Seed は全ピアで同一値が供給される(拡散など確率挙動は必ず Seed 由来の乱数を使うこと)。</para>
    /// <para>呼び出しタイミング: 発射パイプライン内で生成され、OnAfterFire 完了後は無効。保持してはならない。</para>
    /// <para>スレッド制約: Unity メインスレッドのみ。</para>
    /// </summary>
    public class FireContext
    {
        public FireContext(IWeaponHost host, WeaponSpec shot)
        {
            Host = host;
            Shot = shot;
        }

        public IWeaponHost Host { get; private set; }

        /// <summary>このショット専用の仕様コピー。</summary>
        public WeaponSpec Shot { get; private set; }

        public Vector3 MuzzlePosition;
        public Quaternion MuzzleRotation;
        public Vector3 Direction;

        /// <summary>0 より大きい値を設定すると発射がその秒数だけ遅延される(OnBeforeFire で設定可)。</summary>
        public float DelaySeconds;

        /// <summary>弾体へ適用する誘導戦略。null = 無誘導。</summary>
        public IGuidanceStrategy Guidance;

        /// <summary>決定論用乱数シード(全ピア同値)。</summary>
        public int Seed;
    }
}

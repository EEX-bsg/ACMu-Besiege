using UnityEngine;
using ACMu.Core.Game;
using ACMu.Core.Logging;

namespace ACMu.Core.Weapons
{
    /// <summary>
    /// 武装ロジック(WeaponComponentBase)から見たホスト側(BlockModuleBehaviour 実装)の窓口。
    /// 武装ロジックは Besiege / BesiegeCustomModule の型に直接触れず、必ず本契約を経由する。
    /// <para>不変条件: Block / Log / Projectiles は OnAttached 以降 null にならない。
    /// BaseSpec の変更は以後の全ショットの既定値に反映される。</para>
    /// <para>呼び出しタイミング: シミュレーション中のみ有効。OnSimulationStop 以降のメンバー呼び出し結果は未定義。</para>
    /// <para>スレッド制約: Unity メインスレッドのみ。</para>
    /// </summary>
    public interface IWeaponHost
    {
        IBlockAccessor Block { get; }
        ILog Log { get; }
        IProjectileService Projectiles { get; }

        /// <summary>この武装の既定仕様。XML モジュールの値で初期化される。</summary>
        WeaponSpec BaseSpec { get; }

        /// <summary>このピアが発射権威(ホスト)側かどうか。</summary>
        bool IsAuthority { get; }

        Vector3 MuzzlePosition { get; }
        Quaternion MuzzleRotation { get; }

        /// <summary>プレイヤー入力以外からの発射要求。発射パイプライン(Validate → BeforeFire → …)を通る。</summary>
        void RequestFire();
    }
}

using UnityEngine;

namespace ACMu.Core.Weapons
{
    /// <summary>
    /// 弾体スポーン要求。IProjectileService.Spawn の入力。
    /// <para>不変条件: ProjectileKey は登録済みであること(未登録は無効ハンドルが返る)。Position / Velocity はシーン座標。</para>
    /// <para>スレッド制約: Unity メインスレッドのみ。Spawn 呼び出し後の再利用は不可。</para>
    /// </summary>
    public class ProjectileSpawnRequest
    {
        public string ProjectileKey;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;

        /// <summary>発射元。所有者破壊時の弾処理・ダメージ帰属に用いる。null 可(レベル起因の弾)。</summary>
        public IWeaponHost Owner;

        /// <summary>このショットの仕様(Damage / ExplosionRadius / Lifetime 等を参照)。null 不可。</summary>
        public WeaponSpec Shot;

        /// <summary>誘導戦略。null = 無誘導。</summary>
        public IGuidanceStrategy Guidance;
    }
}

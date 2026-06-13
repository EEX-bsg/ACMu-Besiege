using UnityEngine;

namespace ACMu.Core.Weapons
{
    /// <summary>
    /// 誘導計算の入力。毎物理ステップ、サービス側が値を更新してから IGuidanceStrategy へ渡す。
    /// <para>不変条件: 値はシーン座標(float)。HasTarget = false のとき TargetPosition は未定義。</para>
    /// <para>スレッド制約: Unity メインスレッドのみ。インスタンスは弾体ごとに1つで、ステップ間で再利用される。</para>
    /// </summary>
    public class GuidanceContext
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public bool HasTarget;
        public Vector3 TargetPosition;
        public Vector3 TargetVelocity;
        public float ElapsedSeconds;

        /// <summary>許容最大加速度(m/s^2)。戦略の出力はこの大きさにクランプされる。</summary>
        public float MaxAcceleration;
    }
}

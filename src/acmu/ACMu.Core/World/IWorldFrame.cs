using UnityEngine;
using ACMu.Core.Maths;

namespace ACMu.Core.World
{
    /// <summary>
    /// ワールド座標(double)とシーン座標(float)の変換規約。拡張EXの中核契約。
    /// 通信路上の座標は常に Vector3d(ワールド座標)で表現し、シーンへの適用時にのみ本契約で変換する。
    /// <para>不変条件:
    /// (1) 座標権威はホスト。クライアントの RequestOriginShift は無視される。
    /// (2) ToScene(ToWorld(p)) は原点から 10km 以内で誤差 1mm 未満であること。
    /// (3) Origin の変更は OriginShifted の発火と必ず対であること。初期実装では Origin は恒等(Zero)固定でよい。</para>
    /// <para>呼び出しタイミング: 変換は任意のフレームで可。原点シフトの実行は FixedUpdate 群の完了後
    /// (物理ステップの途中で剛体座標が書き換わってはならない)。</para>
    /// <para>スレッド制約: Unity メインスレッドのみ。</para>
    /// </summary>
    public interface IWorldFrame
    {
        /// <summary>現在のシーン原点が指すワールド座標。</summary>
        Vector3d Origin { get; }

        Vector3 ToScene(Vector3d worldPosition);
        Vector3d ToWorld(Vector3 scenePosition);

        /// <summary>原点シフトを要求する。ホストでのみ有効。実行は次の物理ステップ境界まで遅延される。</summary>
        void RequestOriginShift(Vector3d newOrigin);

        event WorldOriginShiftedHandler OriginShifted;
    }
}

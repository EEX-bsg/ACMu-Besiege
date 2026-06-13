using UnityEngine;

namespace ACMu.Core.Weapons
{
    /// <summary>
    /// 弾体誘導の差し替え点。「ミサイルの遊動方式の変更」は本契約の実装差し替えで表現する。
    /// <para>不変条件: ComputeAcceleration は context 以外の外部状態を変更しない。
    /// 出力ベクトルの大きさは context.MaxAcceleration を超えてもよい(サービス側でクランプされる)。
    /// 例外を送出した場合、当該弾は無誘導に降格し本戦略は以後呼ばれない。</para>
    /// <para>呼び出しタイミング: OnLaunch はスポーン直後に1回。ComputeAcceleration はホスト側でのみ毎 FixedUpdate。
    /// クライアントでは呼ばれない(結果は座標同期で受け取る)。</para>
    /// <para>スレッド制約: Unity メインスレッドのみ。インスタンスは弾体ごとに新規生成し、共有しないこと。</para>
    /// </summary>
    public interface IGuidanceStrategy
    {
        void OnLaunch(GuidanceContext context);
        Vector3 ComputeAcceleration(GuidanceContext context, float fixedDeltaTime);
    }
}

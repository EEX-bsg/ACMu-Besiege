using UnityEngine;

namespace ACMu.Core.Weapons
{
    /// <summary>
    /// 着弾・爆発の可変コンテキスト。OnImpact / OnExplosion で共有され、ダメージ補正や既定爆発の抑止に用いる。
    /// 「着弾地点への追加オブジェクト発生」は本コンテキストを受けた側が IProjectileService 等で行う。
    /// <para>不変条件: ホスト側でのみ生成される(着弾判定はホスト権威)。Position / Normal はシーン座標。</para>
    /// <para>呼び出しタイミング: 着弾判定の直後、弾体 Despawn の前。フック完了後は無効。保持してはならない。</para>
    /// <para>スレッド制約: Unity メインスレッドのみ。</para>
    /// </summary>
    public class ImpactContext
    {
        public ProjectileHandle Projectile;
        public Vector3 Position;
        public Vector3 Normal;

        /// <summary>命中対象。地形・不明対象の場合は null。</summary>
        public GameObject HitObject;

        public float Damage;
        public float PlayerDamage;
        public float ExplosionRadius;

        /// <summary>true にすると既定の爆発処理(範囲ダメージ・エフェクト)を抑止する。</summary>
        public bool SuppressDefaultExplosion;
    }
}

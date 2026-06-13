using System;
using UnityEngine;

namespace ACMu.Core.Weapons
{
    /// <summary>
    /// 弾体の生成・プール・寿命・同期の単一管理点。既知不具合「スポーン/デスポーン同期の弱さ」への対策として、
    /// 弾体ライフサイクルの正をここに一本化する。
    /// <para>不変条件:
    /// (1) スポーン権威はホスト。MP クライアントでの Spawn は ProjectileHandle.Invalid を返し、何も生成しない。
    /// (2) Spawned / Despawned は全ピアで同一ハンドル列に対して発火する(順序: Spawned → Despawned が必ず対)。
    /// (3) Despawn は冪等(無効・消滅済みハンドルに対しては何もしない)。
    /// (4) RegisterProjectile の key は一意。重複登録は例外。</para>
    /// <para>呼び出しタイミング: RegisterProjectile は OnModLoad 中のみ。Spawn / Despawn はシミュレーション中のみ。</para>
    /// <para>スレッド制約: 全メンバーは Unity メインスレッドのみ。</para>
    /// </summary>
    public interface IProjectileService
    {
        /// <summary>弾体プレハブを登録し、prewarmCount 分をプールに事前生成する。</summary>
        void RegisterProjectile(string key, GameObject prefab, int prewarmCount);

        ProjectileHandle Spawn(ProjectileSpawnRequest request);
        void Despawn(ProjectileHandle handle, DespawnReason reason);
        bool IsAlive(ProjectileHandle handle);

        /// <summary>生存中の弾体の GameObject を取得する。消滅済みは false。</summary>
        bool TryGetGameObject(ProjectileHandle handle, out GameObject gameObject);

        event Action<ProjectileHandle> Spawned;
        event Action<ProjectileHandle, DespawnReason> Despawned;
    }
}

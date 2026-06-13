using System;

namespace ACMu.Core.Weapons
{
    /// <summary>
    /// 武装ロジックの抽象基底。プラグインはこれを継承し、フックを上書きして挙動を拡張する。
    /// BlockModuleBehaviour とは継承関係を持たず、ホスト側が合成(保持・委譲)する。
    /// <para>不変条件: Host は AttachTo 以降 null にならず、再アタッチは不可。
    /// フック内で送出された例外はホスト側で捕捉され、当該武装インスタンスは無効化される(Mod 全体は停止しない)。</para>
    /// <para>呼び出しタイミング(ホストが保証する順序):
    /// AttachTo → OnAttached → (OnSimulationStart → [OnUpdate]* / 発射毎: OnValidateFire → OnBeforeFire → OnAfterFire
    /// / 着弾毎: OnImpact → OnExplosion → OnSimulationStop)*。
    /// Notify 系 public メソッドはホスト実装専用であり、プラグインから呼んではならない。</para>
    /// <para>スレッド制約: 全メンバーは Unity メインスレッドのみ。</para>
    /// </summary>
    public abstract class WeaponComponentBase
    {
        public IWeaponHost Host { get; private set; }

        /// <summary>ホストが武装生成直後に1回だけ呼ぶ。</summary>
        public void AttachTo(IWeaponHost host)
        {
            if (host == null)
            {
                throw new ArgumentNullException("host");
            }
            if (Host != null)
            {
                throw new InvalidOperationException("WeaponComponentBase は再アタッチできません。");
            }
            Host = host;
            OnAttached();
        }

        // ---- ホスト実装専用の呼び出し口(プラグインは呼ばない) ----

        public void NotifySimulationStart()
        {
            OnSimulationStart();
        }

        public void NotifySimulationStop()
        {
            OnSimulationStop();
        }

        public void NotifyUpdate(float deltaTime)
        {
            OnUpdate(deltaTime);
        }

        public FireDecision NotifyValidateFire(FireContext context)
        {
            return OnValidateFire(context);
        }

        public void NotifyBeforeFire(FireContext context)
        {
            OnBeforeFire(context);
        }

        public void NotifyAfterFire(FireContext context, ProjectileHandle projectile)
        {
            OnAfterFire(context, projectile);
        }

        public void NotifyImpact(ImpactContext context)
        {
            OnImpact(context);
        }

        public void NotifyExplosion(ImpactContext context)
        {
            OnExplosion(context);
        }

        // ---- プラグインが上書きするフック ----

        /// <summary>Host 確定直後。BaseSpec の書き換え(既定値の上書き)はここで行う。</summary>
        protected virtual void OnAttached()
        {
        }

        protected virtual void OnSimulationStart()
        {
        }

        protected virtual void OnSimulationStop()
        {
        }

        /// <summary>シミュレーション中、権威側で毎フレーム呼ばれる。</summary>
        protected virtual void OnUpdate(float deltaTime)
        {
        }

        /// <summary>発射可否の判定。Suppress を返すと今回の発射は行われない。</summary>
        protected virtual FireDecision OnValidateFire(FireContext context)
        {
            return FireDecision.Proceed;
        }

        /// <summary>発射直前の介入点。遅延(DelaySeconds)・弾速・誘導(Guidance)の差し替えはここで行う。</summary>
        protected virtual void OnBeforeFire(FireContext context)
        {
        }

        /// <summary>弾体スポーン直後。projectile は弾なし武装では Invalid。</summary>
        protected virtual void OnAfterFire(FireContext context, ProjectileHandle projectile)
        {
        }

        /// <summary>着弾直後(爆発処理前)。</summary>
        protected virtual void OnImpact(ImpactContext context)
        {
        }

        /// <summary>爆発処理の介入点。ダメージ補正・既定爆発の抑止はここで行う。</summary>
        protected virtual void OnExplosion(ImpactContext context)
        {
        }
    }
}

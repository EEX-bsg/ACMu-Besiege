using UnityEngine;

namespace ACMu.Adapter
{
    // BlockDamage / EntityDamage を適用する。Bootstrap から DamageRegistry へ静的メソッドを渡す。
    // BlockBehaviour / EnemyAISimple / KillingHandler は本体デコンパイル型のため Adapter 内のみ。
    public static class DamageApplierAdapter
    {
        public static void ApplyDamage(GameObject hitObject, float blockDamage, float entityDamage)
        {
            if (hitObject == null) return;

            // ブロックか判定: コライダーGOまたはその親に BlockBehaviour
            var bb = hitObject.GetComponent<BlockBehaviour>()
                  ?? hitObject.GetComponentInParent<BlockBehaviour>();
            if (bb != null)
            {
                if (blockDamage > 0f && bb.Prefab != null && bb.Prefab.hasHealthBar && bb.BlockHealth != null)
                    bb.BlockHealth.DamageBlock(blockDamage);
                // ブロックなのでエンティティチェックは不要
                return;
            }

            if (entityDamage <= 0f) return;

            // EnemyAISimple (ゾンビ・歩兵など)
            var simpleAi = hitObject.GetComponent<EnemyAISimple>();
            if (simpleAi != null)
            {
                simpleAi.TakeDamage(entityDamage, InjuryType.Sharp);
                return;
            }

            // KillingHandler (馬・特殊エンティティなど)
            var kh = hitObject.GetComponent<KillingHandler>();
            if (kh != null)
                kh.TakeDamage(entityDamage, InjuryType.Sharp);
        }
    }
}

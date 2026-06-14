using UnityEngine;

namespace ACMu.Core.Game
{
    /// <summary>
    /// Besiege ブロックの GameObject から <see cref="IBlockAccessor"/> を生成するファクトリ。
    /// 本体 BlockBehaviour への依存を Adapter 実装へ完全に閉じ込めるための seam。
    /// シミュレーション中のブロック GameObject(sim インスタンス)を渡すこと。
    /// <para>不変条件: 有効な Besiege ブロックの GameObject に対し非 null を返す。
    /// ブロックでない GameObject を渡した場合の結果は未定義(null もしくは無効アクセサ)。
    /// 同一 GameObject に対する返り値はキャッシュしてよい(アロケーション削減)。</para>
    /// <para>呼び出しタイミング: シミュレーション開始後、対象ブロックの sim インスタンス確定以降。
    /// ビルドフェーズの GameObject を渡してはならない(build/sim 二重性)。</para>
    /// <para>スレッド制約: Unity メインスレッドのみ。</para>
    /// </summary>
    public interface IBlockAccessorFactory
    {
        IBlockAccessor FromGameObject(GameObject blockObject);
    }
}

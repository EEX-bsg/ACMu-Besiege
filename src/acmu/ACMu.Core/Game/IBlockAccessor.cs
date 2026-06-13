using System;
using UnityEngine;

namespace ACMu.Core.Game
{
    /// <summary>
    /// ブロック1個への読み取りアクセス契約。本体 BlockBehaviour の public 署名依存を Adapter 実装に閉じ込める。
    /// <para>不変条件: 本契約経由で本体の状態を直接書き換えない(書き換えが必要な操作は専用契約として追加する)。
    /// IsDestroyed = true 以降、Position / Rotation 以外のメンバーへのアクセス結果は未定義。</para>
    /// <para>呼び出しタイミング: シミュレーション中はブロック生存期間内で任意。Mapper 系(TryGetSlider 等)は
    /// ブロック初期化完了(IGameEventSource.BlockInitialized 通知)以降に呼ぶこと。</para>
    /// <para>スレッド制約: Unity メインスレッドのみ。</para>
    /// </summary>
    public interface IBlockAccessor
    {
        Guid Guid { get; }
        GameObject GameObject { get; }

        /// <summary>剛体。noRigidbody のブロックでは null。</summary>
        Rigidbody Rigidbody { get; }

        bool IsSimulating { get; }
        bool IsDestroyed { get; }
        Vector3 Position { get; }
        Quaternion Rotation { get; }

        bool TryGetSlider(string key, out float value);
        bool TryGetToggle(string key, out bool value);
        bool IsKeyHeld(string key);
        bool IsKeyPressed(string key);
    }
}

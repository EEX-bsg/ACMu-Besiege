using System;
using ACMu.Core.Weapons;

namespace ACMu.PluginApi
{
    /// <summary>
    /// 武装1種の登録情報。
    /// <para>不変条件: ModuleName は XML の XmlRoot 文字列と一致し、Mod 横断で一意であること。
    /// WeaponFactory はブロックのシミュレーションインスタンスごとに1回呼ばれ、毎回新規インスタンスを返すこと。</para>
    /// <para>スレッド制約: Unity メインスレッドのみ。</para>
    /// </summary>
    public class WeaponRegistration
    {
        /// <summary>CustomModules.AddBlockModule へ渡す識別子(= XmlRoot 名)。セーブデータ互換の対象。</summary>
        public string ModuleName;

        /// <summary>マルチプレイヤー対応フラグ。既存互換モジュールは true 必須。</summary>
        public bool MultiplayerCompatible = true;

        /// <summary>武装の既定仕様。XML モジュール側の値で上書きされた後、IWeaponHost.BaseSpec となる。</summary>
        public WeaponSpec Defaults;

        /// <summary>武装ロジックの生成関数。</summary>
        public Func<WeaponComponentBase> WeaponFactory;
    }
}

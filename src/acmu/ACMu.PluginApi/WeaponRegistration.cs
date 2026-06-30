using System;
using ACMu.Core.Weapons;

namespace ACMu.PluginApi
{
    /// <summary>
    /// 武装1種の登録情報。
    /// <para>不変条件: ModuleName は XML の XmlRoot 文字列と一致し、<b>自 Mod 内で</b>一意であること。
    /// Mod 横断での同名は許容される(セーブ XML の modid 属性で曖昧解消されるため)。
    /// WeaponFactory はブロックのシミュレーションインスタンスごとに1回呼ばれ、毎回新規インスタンスを返すこと。</para>
    /// <para>スレッド制約: Unity メインスレッドのみ。</para>
    /// </summary>
    public class WeaponRegistration
    {
        /// <summary>CustomModules.AddBlockModule へ渡す識別子(= XmlRoot 名)。セーブデータ互換の対象。
        /// ACMu 内部では診断ログとデバッグ表示にのみ使う(Besiege への登録は ModuleRegistrar 側で行うため)。</summary>
        public string ModuleName;

        /// <summary>マルチプレイヤー対応フラグ。既存互換モジュールは true 必須。
        /// 注意: これは CustomModules.AddBlockModule の canReload(ホットリロード)とは別概念。
        /// canReload はプラグインが ModuleRegistrar 内の AddBlockModule 呼び出しで直接指定する。</summary>
        public bool MultiplayerCompatible = true;

        /// <summary>武装の既定仕様。XML モジュール側の値で上書きされた後、IWeaponHost.BaseSpec となる。</summary>
        public WeaponSpec Defaults;

        /// <summary>武装ロジックの生成関数。</summary>
        public Func<WeaponComponentBase> WeaponFactory;

        /// <summary>
        /// Besiege へのモジュール登録(CustomModules.AddBlockModule の呼び出し)を行うデリゲート。
        /// ACMu の Register 内から1回だけ invoke される。
        /// <para><b>重要(Mod 名義):</b> CustomModules.AddBlockModule は内部で GetCallingAssembly() を使い、
        /// <i>この呼び出し命令が物理的に焼かれたアセンブリ</i>を Mod 名義(セーブ XML の modid)とする。
        /// したがって本デリゲートの本体は<b>必ずプラグイン自身のコード</b>で書き、その中で
        /// <c>CustomModules.AddBlockModule&lt;TModule, TBehaviour&gt;(ModuleName, canReload)</c> を呼ぶこと。
        /// ACMu 提供のヘルパー経由にすると名義が ACMu に戻ってしまう。</para>
        /// <para>TBehaviour: <c>WeaponHostBehaviour&lt;TModule&gt;</c> の具象非汎用サブクラス。
        /// Unity 5.4 は AddComponent(Type) に汎用型を渡せないため、プラグインごとに
        /// <c>class MyBehaviour : WeaponHostBehaviour&lt;MyModule&gt; {}</c> を宣言して渡すこと。</para>
        /// </summary>
        public Action ModuleRegistrar;
    }
}

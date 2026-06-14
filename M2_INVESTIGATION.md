# M2 着手前調査: ACMu.Core / PluginApi の Besiege 実API整合性レビュー

調査日: 2026-06-14 / 調査者: Opus / 実装担当: Sonnet
目的: M2(武装の縦切り)着手前に、ACMu.Core / ACMu.PluginApi が Besiege の**実際の**カスタムモジュールAPIと
整合しているかを網羅検証し、必要な契約改訂を「今この時点で」確定する。都度変更を避けるための一括調査。

---

## 0. エグゼクティブサマリ(最初にこれだけ読めば足りる)

1. **前回の「契約変更が必要(`CustomModules` / `BlockModuleBehaviour` が存在しない)」報告は誤りだった。**
   - 原因: PowerShell の `Assembly.GetTypes()` が `ReflectionTypeLoadException` を投げ、その際 UnityEngine 依存型
     (= `MonoBehaviour` 派生型、まさに `BlockModuleBehaviour<T>`)を **null として全て脱落**させていた。
   - 対策: メタデータ専用の **Mono.Cecil**(`Besiege_Data/Managed/Mono.Cecil.dll`)で再調査。全2465型を正しく取得し訂正。
   - 結論: `Modding.Modules.CustomModules.AddBlockModule<TModule, TBehaviour>(string name, bool canReload)` と
     `Modding.Modules.BlockModuleBehaviour<TModule>` は**実在する**。ROADMAP M2 の登録設計はそのまま実装可能。

2. **ACMu.PluginApi: 変更不要(ゼロ)。**

3. **ACMu.Core: 追加が必要なのは1点のみ** — 「`GameObject` から `IBlockAccessor` を生成するファクトリ seam」。
   - 理由: Weapons層の `WeaponHostBehaviour` が `IWeaponHost.Block`(= `IBlockAccessor`)を組み立てる際、
     decompile由来型(`BlockBehaviour` / `MSlider` / `MKey` / `MapperType`)に触れずに済む経路が現契約に無い。
   - これは ARCHITECTURE §1 rule 2(decompile由来クラスは Adapter のみ)を守るために不可避な seam。

4. その他の武装系契約(`IWeaponHost` / `WeaponComponentBase` / `IProjectileService` / `FireContext` 等)は
   **すべて変更不要**であることを個別に確認済み(§4)。

> **CLAUDE.md ルール1(契約凍結)について**: 本改訂はユーザー(設計権限者)が明示的に承認した上での Core 改訂である。
> Sonnet は本書 §3 の差分のみを適用してよい。それ以外の Core/PluginApi への変更は依然禁止。

---

## 1. 確定した Besiege 実API(Mono.Cecil 検証済み・出典: Assembly-CSharp.dll)

### 1.1 モジュール登録
```
Modding.Modules.CustomModules
  public static void AddBlockModule<TModule, TBehaviour>(string name, bool canReload)
    // ジェネリック制約: なし(TModule/TBehaviour とも無制約)
    // name = XmlRoot 名。canReload = リロード対応フラグ。OnLoad 中に呼ぶ。
```

### 1.2 ホストBehaviour基底
```
Modding.Modules.BlockModuleBehaviour`1 : Modding.ModBlockBehaviour   // ← MonoBehaviour 派生
  public  TModule    Module        { get; }   // デシリアライズ済みモジュール
  public  MKey       GetKey(MKeyReference)          // ← 返り値 MKey は decompile型(使用注意)
  public  MSlider    GetSlider(MSliderReference)    // ← 返り値 MSlider は decompile型(使用注意)
  public  MToggle    GetToggle(MToggleReference)    // ← 同上
  public  MValue     GetValue(MValueReference)      // ← 同上
  public  MColourSlider GetColourSlider(MColourSliderReference)
  public  ModResource GetResource(ResourceReference)
  public  virtual void OnReload()
  public  object     RawModule { get; set; }
  public  string     ModuleGuid { get; set; }
```

### 1.3 ModBlockBehaviour(ホストが継承する全ライフサイクル)
```
Modding.ModBlockBehaviour : UnityEngine.MonoBehaviour
  -- ライフサイクル(全 virtual) --
  SafeAwake()                         // 初期化。AddKey/AddSlider 等はここで(モジュールでは GetKey 等)
  OnSimulateStart() / OnSimulateStop()
  SimulateUpdateAlways/Host/Client()
  SimulateFixedUpdateAlways/Host/Client()
  KeyEmulationUpdate()
  OnSimulateCollisionEnter(Collision) / ...Stay / ...Exit
  OnSimulateTriggerEnter(Collider) / ...
  OnSave(XDataHolder) / OnLoad(XDataHolder)
  -- 状態プロパティ(Weapons層から触れてよい = Unity/Modding型のみ) --
  bool IsSimulating, bool IsDestroyed, bool Flipped, bool SimPhysics
  UnityEngine.Rigidbody Rigidbody, bool HasRigidbody
  int BlockId
  Modding.Blocks.PlayerMachine Machine        // ← Modding API(許可)
  UnityEngine.Transform MainVis, DirectionArrow
  -- ⚠ 触れてはいけないプロパティ(decompile型を返す) --
  BlockBehaviour BlockBehaviour               // ← global名前空間。Weapons では使用禁止
```

### 1.4 mapper型 vs reference型の名前空間(依存規律の判定に直結)
| 型 | 名前空間 | 規律上の扱い |
|---|---|---|
| `MapperType`, `MSlider`, `MKey`, `MToggle`, `MValue` | **global(空)** | **decompile由来。Adapter のみ参照可(rule 2)** |
| `MSliderReference`, `MKeyReference`, `MToggleReference`, `MValueReference` | `Modding.Serialization` | **Modding API。Weapons から参照可(rule 5)** |
| `MapperTypeReference`(上記の基底) | `Modding.Serialization` | 公開 `string Key` フィールドを持つ |
| `BlockModule`(TModuleの基底) | `Modding.Modules` (基底 `Modding.Serialization.Element`) | Modding API。参照可。ctor は protected/0引数 |

### 1.5 ブロック→アクセサ変換に使えるファクトリ(Adapter 実装用)
```
Modding.Blocks.Block   // Modding API(全層から参照可)
  public static Block From(UnityEngine.GameObject obj)   // ← Unity型から取得可能(decompile非経由)
  public static Block From(System.Guid guid)
  public BlockBehaviour InternalObject { get; }          // ← decompile型。Adapter 内でのみ展開する
  public System.Guid Guid { get; }
  public UnityEngine.GameObject GameObject { get; }
```

---

## 2. 依存規律の緊張点(本調査の核心)と解決策

### 2.1 問題
`WeaponHostBehaviour<TModule>`(ACMu.Weapons)は `IWeaponHost` を実装する。
`IWeaponHost.Block` は `IBlockAccessor`(非null不変条件)を返さねばならない。
しかし `IBlockAccessor` の唯一の実装 `BlockAccessorAdapter` は **ACMu.Adapter** にあり、ctor は `BlockBehaviour`
(decompile型)を取る。Weapons から:
- `this.BlockBehaviour` を読む → **rule 2 違反**(decompile型 `BlockBehaviour` に触れる)
- `BlockAccessorAdapter` を直接 new → **rule 3 違反**(実装層の横参照)

さらに、スライダー/キー値を `GetSlider()/GetKey()` で読むと返り値 `MSlider`/`MKey` が decompile型 → **rule 2 違反**。

### 2.2 解決策(2段構え。これで decompile型に一切触れずに済む)

**(A) アクセサ取得**: Core に「`GameObject` → `IBlockAccessor`」ファクトリ契約を1つ追加する(§3)。
   - Weapons は `Modding.Blocks.Block.From(this.gameObject)` 相当を**直接は使わず**、Core ファクトリの
     `FromGameObject(this.gameObject)` を呼ぶだけ。引数も返り値も Unity/Core 型のみ。
   - Adapter がファクトリを実装し、内部で `Block.From(go).InternalObject`(decompile)→ `BlockAccessorAdapter` を行う。
   - Weapons はファクトリ実体を **ACMUcore seam 経由**(`GameObject.Find(WellKnownNames.CoreObjectName)
     .GetComponent<IAcmuPluginHost>().Services`)で取得する。

**(B) 値読み取り**: `BlockModuleBehaviour<T>.GetSlider/GetKey`(decompile返り値)を**使わない**。代わりに:
   - モジュール側 `TModule` の参照フィールド(`MSliderReference` 等, `Modding.Serialization`, 許可)から
     `.Key`(public string)を取り出し、
   - `IBlockAccessor.TryGetSlider(key, out v)` / `TryGetToggle` / `IsKeyHeld` / `IsKeyPressed`(すべて Core, string キー)
     で値を読む。
   - 例: 発射キー `if (Block.IsKeyPressed(Module.FireKey.Key)) Host.RequestFire();`
   - 例: 弾速  `if (Block.TryGetSlider(Module.SpeedSlider.Key, out var spd)) spec.MuzzleVelocity = spd;`
   - ⇒ `IBlockAccessor` が string キーで mapper 値を返す設計は、まさにこの用途を先取りしていた。Core は良設計。

> この (A)+(B) により、Weapons層は decompile型(`BlockBehaviour`/`MSlider`/`MKey`/`MapperType`)に
> **一度も触れずに** 武装ホストを実装できる。`BlockModuleBehaviour<T>` 自体は `Modding.Modules`(rule 5 許可)。

### 2.3 build/sim インスタンス二重性に関する注意(実装時の必須事項)
`BlockBehaviour` はビルド用とシミュ用で別インスタンス。よってアクセサ取得は**シミュ開始時に sim インスタンスの
GameObject から行う**こと。具体的には `WeaponHostBehaviour.OnSimulateStart()` 内で
`Block = factory.FromGameObject(this.gameObject)` を実行し、その後に武装の `AttachTo(this)` → `NotifySimulationStart()`
を呼ぶ。`SafeAwake()`(ビルド時)でアクセサを確定すると誤ったインスタンスを掴む。

---

## 3. 確定する Core 改訂(これが「修正版 ACMu.Core」の全差分。Sonnet はこの2点のみ適用)

### 3.1 新規ファイル: `src/acmu/ACMu.Core/Game/IBlockAccessorFactory.cs`

```csharp
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
```

### 3.2 改訂ファイル: `src/acmu/ACMu.Core/IAcmuServices.cs`

`IGameEventSource GameEvents { get; }` の直後に1プロパティを追加する。**他の行は変更しない。**

追加する行:
```csharp
        /// <summary>ブロック GameObject から IBlockAccessor を生成するファクトリ。</summary>
        ACMu.Core.Game.IBlockAccessorFactory Blocks { get; }
```

改訂後の全文(これで置換):
```csharp
using ACMu.Core.Config;
using ACMu.Core.Game;
using ACMu.Core.Logging;
using ACMu.Core.Net;
using ACMu.Core.Weapons;
using ACMu.Core.World;

namespace ACMu.Core
{
    /// <summary>
    /// ACMu が内部モジュールおよび他 Mod へ公開するサービス集約点。
    /// 実装は ACMUcore GameObject 上のコンポーネントが提供し、各サービスへの参照を保持する。
    /// <para>不変条件: 全プロパティは OnModLoad 完了後 null を返さない。シーン遷移をまたいで同一インスタンスを返す。</para>
    /// <para>呼び出しタイミング: 取得は OnModLoad 完了後(他 Mod からは Mods.OnModLoaded 以降)。</para>
    /// <para>スレッド制約: Unity メインスレッドのみ(ILog を除く)。</para>
    /// </summary>
    public interface IAcmuServices
    {
        ILog Log { get; }
        IGameSessionInfo Session { get; }
        IGameEventSource GameEvents { get; }

        /// <summary>ブロック GameObject から IBlockAccessor を生成するファクトリ。</summary>
        IBlockAccessorFactory Blocks { get; }

        IConfigStore Config { get; }
        INetworkTransport Network { get; }
        IWorldFrame World { get; }
        IProjectileService Projectiles { get; }
    }
}
```

### 3.3 連動して必要な実装(Core ではないが本改訂とセットで M2 に含める)

これらは凍結対象外なので Sonnet が自由に実装してよい。

1. **`src/ACMu.Adapter/BlockAccessorFactoryAdapter.cs`**(新規):
   `MonoBehaviour, IBlockAccessorFactory, ILifecycleParticipant`。InitOrder=0。
   `FromGameObject(go)` は `Modding.Blocks.Block.From(go).InternalObject` を `BlockAccessorAdapter` に包んで返す。
   軽量キャッシュ(`Dictionary<GameObject, IBlockAccessor>`)を持ち、毎フレーム呼び出しでもアロケーションしないこと。
   `Block.From(go)` が null/例外の可能性に備え try-catch → null 返却 + ILog.Warn。

2. **`src/ACMu.Host/AcmuServicesComponent.cs`**(改訂):
   フィールド `_blocks` と `IBlockAccessorFactory Blocks { get { return _blocks; } }` を追加。
   `Initialize(...)` のシグネチャに `IBlockAccessorFactory blocks` を足して受け取る。

3. **`src/ACMu.Host/AcmuCoreBootstrap.cs`**(改訂):
   `var blocks = go.AddComponent<BlockAccessorFactoryAdapter>();` を追加し、
   `services.Initialize(...)` に渡し、`coordinator.AddParticipant(blocks);` を追加。

### 3.4 ARCHITECTURE.md への追記(設計の同期。ユーザー承認済み変更の記録)

§2 決定表に1行追加することを推奨(Sonnet ではなくユーザー/Opus が確定):
```
| ブロックアクセサ取得 | Core に IBlockAccessorFactory(GameObject→IBlockAccessor)を追加し ACMUcore で公開 |
  Weapons が decompile型(BlockBehaviour/MSlider/MKey)に触れず IWeaponHost.Block を構築するため |
  Weaponsで Block.From を直呼び(Modding依存がWeaponsに漏れる) | sim時にFromGameObjectで解決 |
```

---

## 4. 変更不要と確認した契約(網羅リスト・各々の根拠)

| 契約 | 判定 | 根拠 |
|---|---|---|
| `IWeaponRegistry.Register<TModule>(WeaponRegistration) where TModule:BlockModule,new()` | 変更不要 | `BlockModule`(`Modding.Modules`)実在。内部で `CustomModules.AddBlockModule<TModule, WeaponHostBehaviour<TModule>>(reg.ModuleName, reg.MultiplayerCompatible)` を呼べる。`new()` 制約は Besiege より厳しいだけで無害(モジュールは public 0引数 ctor を持つ) |
| `WeaponRegistration` | 変更不要 | ModuleName/MultiplayerCompatible/Defaults/WeaponFactory すべて実装で消費可能 |
| `IWeaponHost` | 変更不要 | 全メンバーが Core/Unity 型のみ(§2.2 で Block 取得経路を確立)。decompile漏れなし |
| `WeaponComponentBase` | 変更不要 | 純粋抽象。Besiege 非依存 |
| `IProjectileService` / `ProjectileSpawnRequest` / `ProjectileHandle` / `DespawnReason` | 変更不要 | 弾体プールは純 Unity(GameObject/Rigidbody/Collision)で実装可。本体API不要 |
| `FireContext` / `ImpactContext` / `FireDecision` / `WeaponSpec` | 変更不要 | 純データ。Unity 型のみ |
| `IGuidanceStrategy` / `GuidanceContext` | 変更不要 | M2 では未使用(null=無誘導)。契約は将来用に温存 |
| `IAcmuPluginHost` | 変更不要 | ApiVersion/Services/Weapons。seam 取得は `GetComponent<IAcmuPluginHost>()` で成立 |
| `IBlockAccessor` | 変更不要 | string キーの TryGetSlider/TryGetToggle/IsKeyHeld/IsKeyPressed が §2.2(B) を完全に満たす |
| `IGameEventSource` | 変更不要 | M2 は `OnBlockPrefabCreation` 不要(ブロックは mod.json+XML+モジュール名で構成。前回の懸念は消滅) |
| `IGameSessionInfo` | 変更不要 | `IsHost`/`IsAuthority` 判定に十分。M2 シングルプレイは IsHost=true |

---

## 5. M2 実装青写真(Sonnet 向け・契約改訂を前提とした手順)

> 前提読み込み: 本書 / 改訂後 IAcmuServices+IBlockAccessorFactory / WeaponComponentBase / IWeaponHost /
> FireContext / WeaponSpec / WeaponRegistration / docs/csharp_mono4_constraints.md / docs/unity54_constraints.md

### 5.1 ファイル一覧と責務
| ファイル(層) | 責務 |
|---|---|
| `ACMu.Weapons/ProjectileService.cs` | `IProjectileService`+`MonoBehaviour`+`ILifecycleParticipant`(InitOrder=300)。`Queue<GameObject>` プール、int連番ハンドル、寿命コルーチン、`Spawned/Despawned` 発火 |
| `ACMu.Weapons/ProjectileBody.cs` | 弾に付ける `MonoBehaviour`。`OnCollisionEnter` で ProjectileService へ着弾通知 |
| `ACMu.Weapons/WeaponHostBehaviour.cs` | `BlockModuleBehaviour<TModule> : IWeaponHost`。薄いホスト。seam解決・武装生成・ライフサイクル転送・発射キー監視 |
| `ACMu.Weapons/FirePipeline.cs` | RequestFire→クールダウン→Validate→BeforeFire→(遅延)→Spawn→AfterFire、着弾→Impact→Explosion→Despawn |
| `ACMu.Weapons/WeaponRegistryImpl.cs` | `IWeaponRegistry`。Register で AddBlockModule + static ファクトリ辞書登録。二重登録は例外 |
| `ACMu.Weapons/WeaponHostRegistry.cs`(内部) | `static Dictionary<Type, WeaponRegistration>`。Host が `typeof(TModule)` で引く。Weapons内 internal |
| `ACMu.Compat/TestCannon/TestCannonModule.cs` | `BlockModule`、`[XmlRoot("AcmuTestCannon")]`、`MKeyReference FireKey` + `MSliderReference SpeedSlider` |
| `ACMu.Compat/TestCannon/TestCannonWeapon.cs` | `WeaponComponentBase`。OnBeforeFire で `Shot.MuzzleVelocity` をスライダー値で上書き |
| `Blocks/`(データ) | ブロック定義 XML 雛形(メッシュは Cube 流用、`<Modules><AcmuTestCannon>...` を含む) |
| Host 配線 | §3.3 の通り。加えて TestCannon の登録呼び出しを ACMu ロード後に行う(Compat 初期化点) |

### 5.2 decompile型を回避する確定パターン(Weapons層で必ずこう書く)
```csharp
// ACMUcore seam の取得(SafeAwake で1回)
var core = GameObject.Find(WellKnownNames.CoreObjectName);
var host = core.GetComponent<IAcmuPluginHost>();   // GetComponent はインターフェース取得可
_services = host.Services;                          // ILog/IProjectileService/IBlockAccessorFactory 等

// OnSimulateStart(sim インスタンスで確定)
_block = _services.Blocks.FromGameObject(this.gameObject);   // ← IBlockAccessor 取得(decompile非経由)
var reg = WeaponHostRegistry.Get(typeof(TModule));           // 武装登録情報
_weapon = reg.WeaponFactory();                               // WeaponComponentBase 生成
_baseSpec = (reg.Defaults != null ? reg.Defaults.Clone() : new WeaponSpec());
_weapon.AttachTo(this);                                      // OnAttached → BaseSpec 調整
_weapon.NotifySimulationStart();

// 値読み取り(decompile型に触れない)
if (_block.IsKeyPressed(Module.FireKey.Key)) RequestFire();              // 発射
if (_block.TryGetSlider(Module.SpeedSlider.Key, out var spd)) ...;       // スライダー
```

### 5.3 ライフサイクル対応表(ModBlockBehaviour → 武装)
| ModBlockBehaviour フック | 武装側の処理 |
|---|---|
| `SafeAwake()` | seam 解決のみ(シーン非依存の参照取得)。`GetKey/GetSlider`(decompile)は呼ばない |
| `OnSimulateStart()` | アクセサ確定 → 武装生成 → AttachTo → NotifySimulationStart |
| `SimulateUpdateHost()` | 権威側のみ。発射キー監視→RequestFire。`_weapon.NotifyUpdate(Time.deltaTime)` |
| `OnSimulateStop()` | NotifySimulationStop → 参照解放(弾の後始末は ProjectileService 側) |

### 5.4 実装上の落とし穴(PROGRESS.md にも記載済み、再掲)
- `dotnet` は PATH 外。`& "C:\Program Files\dotnet\dotnet.exe" build ACMu.csproj -c Release`。
- DLL検査は Mono.Cecil を使う(通常リフレクションは UnityEngine 依存型を脱落させる。本調査の教訓)。
- `Modding.ModIO` は **完全修飾**で書く(`ModIO` という別 namespace と衝突)。
- `BlockBehaviour.Sliders/Toggles/Keys` は `IEnumerable<T>`(`List<T>` ではない)。Adapter 内のみ。
- 閉じたジェネリック MonoBehaviour(`WeaponHostBehaviour<具体Module>`)の AddComponent は、型が具体確定済みなら Unity 5.4 で動作する(Besiege のモジュール機構が内部で行う既知パターン)。

---

## 6. 残る設計選択(1点・既定を採用済み、覆すならユーザー裁定)

**ファクトリの置き場所**: 本書は `IAcmuServices.Blocks`(新 `IBlockAccessorFactory` 型のプロパティ)を**既定採用**した。
- 採用理由: 既存の「能力ごとに1インターフェース」パターンに一致し、他 Mod からも `Services.Blocks` で利用可能。
- 却下した代替: `IAcmuServices` に `IBlockAccessor GetBlockAccessor(GameObject)` メソッドを直付け
  (新型不要でより軽量だが、"サービス集約点はプロパティ getter のみ" という現状の体裁から外れる)。
- この選択を覆す場合のみ §3.1/§3.2 を差し替える。覆さないならこのまま実装可。

---

## 付録: 調査に使った検証コマンド(再現用)

```powershell
$managed = "D:\steam\steamapps\common\Besiege\Besiege_Data\Managed"
Add-Type -Path (Join-Path $managed "Mono.Cecil.dll")
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $managed "Assembly-CSharp.dll"))
$types = $asm.MainModule.Types
# 型検索: $types | Where-Object { $_.Name -like "Block*" }
# メンバ列挙: ($types | ? {$_.Name -eq 'ModBlockBehaviour'}).Methods | ? IsPublic
```
通常リフレクション(`[Reflection.Assembly]::LoadFrom` / `Add-Type`)は UnityEngine 解決に失敗し
`ReflectionTypeLoadException` で MonoBehaviour 派生型を脱落させるため、DLL 構造調査には Cecil を使うこと。

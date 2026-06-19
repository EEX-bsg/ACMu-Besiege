# ROADMAP — マイルストーン一覧

> 着手順: M0→M1→M2→M3→M4→M5(完了) → M5.1→M4.1→M5.2→D1→M6→D2
> 大きなタスクの詳細仕様は GitHub Issue で管理する。

---

## 完了済み(実装のみ。実機確認・是正が残っているものは「残課題」列を参照)

| マイルストーン | 内容 | 実装完了日 | 残課題 |
|---|---|---|---|
| M0 | ビルド基盤 | — | — |
| M1 | Host骨格 + Adapter | — | — |
| M2 | 武装縦切り1本(シングルプレイ) | — | — |
| M3 | INetworkTransport 実装 | — | — |
| M4 | 弾体 MP 同期 | 2026-06-15 | ⚠ 実機確認未実施・同期是正残 → **M4.1** |
| M5 | AdShootingProp 互換実装 + 追加機能 | 2026-06-17 | ⚠ 実機確認未完・互換補完残 → **M5.1 / M5.2** |

---

## 着手順の変更背景(2026-06-19 設計セッション)

当初の着手順 M5→M6 を変更し、M5.1→M4.1→M5.2→D1→M6 を経由してから M6 へ入る。

**目的**: 実装量の最小化ではなく**判断不能な状態を減らす**こと。
現状は「MP同期バグなのか未実装機能なのかCore/API設計の不足なのか」が混在して判断しにくい。
まず観測面を安定させることで、以降の設計判断(D1・M6・D2)が安くなる。

また Core / PluginApi は凍結契約(CLAUDE.md §0-1)であり、オーナー同意なしに変更できない。
観測前に Core/API 設計から入ると、何が共通で何が MP 上不足しているかを知る前に
不可逆な契約を確定することになる(早すぎる抽象化)。
Core を良く設計するために必要な情報そのものが、観測フェーズの産物である。

> **Opus レビュー(2026-06-19)**: この順序は合理的。ただし M4.1 で FireReq payload を拡張する前に、
> 「共通武装状態エンベロープ vs OldCannon 固有拡張」という最小の境界判断だけを先出しすること。
> 境界を決めずに payload を増やすと WeaponSpec への旧ACM固有値混入が wire format 経由で固定化されるリスクがある。

---

## 未着手

### M5.1: M5 実機確認の完了 + MPバグ再現条件の固定

> **M5 の未完了分**。M5 実装は終わっているが実機確認が済んでいない。ここで閉じてから M4.1 へ。

**概要**: `PROGRESS.md` の M5 残り実機確認を消化し、MP バグの再現条件を特定・文書化する。
設計変更は行わない。「何が壊れているか」を観測可能にすることが目的。

Issue #2(MP描画同期)・Issue #4(弾体の回転が大きい)の再現条件もここで固定する。
再現条件が固定できないまま設計変更へ入ると、後続の修正が正しかったか判断できなくなる。

**受け入れ基準**:
- [ ] `PROGRESS.md` の M5 実機確認が全項目消化される
- [ ] Issue #2 / #4 の再現条件が文書化される(修正は M4.1 で行う)

---

### M4.1: M4 弾体MP同期の是正 + 実機確認の完了

> **M4 の未完了分**。M4 実装は終わっているが実機確認未実施・既知ギャップが残っている。

**概要**: M4 実装の既知ギャップを修正し、FireReq を「暫定フォールバック」ではなく仕様として整える。
実機確認チェックリストは `PROGRESS.md` §M4 を参照。

**着手前に確認するギャップ(現行実装の注意点)**:
- `ProjectileSyncTransport.SendFireRequest` が key / position / velocity / lifetime / explosionRadius のみ送信。
  owner / shooter / visual key / effect key / fuse / booster / damage class の MP 表示と検証に必要な最小 payload を決める。
- FireReq 受信側の `senderId` が OwnerPlayerId・ダメージ帰属に不十分。
- ワイヤ座標は `Vector3d` 化されているが `IWorldFrame.ToWorld/ToScene` を経由していない。
- Spawn/Despawn/AliveList/Snapshot の構成はあるが、8KB 超過時の分割は未確認。
- `NetDelivery.Reliable/Unreliable` の下位配送品質切り替えが未整理 → `docs/open-design-questions.md`
- `NetTarget.All` の自己配送差分(SendToAll が送信者自身に届かない)の吸収場所が未整理 → `docs/open-design-questions.md`

**⚠️ 先行判断(D1 から先出し)**: payload 拡張前に「共通武装状態エンベロープ vs OldCannon 固有拡張」の
境界を決めること(Opus 指摘。WeaponSpec への旧ACM固有値混入を wire format 経由で固定化しないため)。

**受け入れ基準**:
- [ ] MP で基本弾体がホスト・クライアント双方に同じものとして見える
- [ ] フューズ爆発が双方で見える
- [ ] orphan proxy が AliveList で掃除される
- [ ] クライアント発射時の所有者・発射者が最低限破綻しない
- [ ] M6 へ進んでも座標同期の境界を後から差し替えられる

---

### M5.2: AdShooting互換の同期検証妨害分を補完

**概要**: AdShooting互換の全機能実装ではなく、MP同期・OldCannon検証で
「未実装なのか同期バグなのか」の判断を誤らせる機能を優先的に埋める。

**短期優先候補**:
- XMLキー互換ズレの修正: `useDelayTimer` / `DelayTimerSlider` / `TimefuseSlider` と `useDelay` の扱い
- `ProjectileSounds` の扱いを整理
- `ShowPlaceholderProjectile` / `PlaceholderProjectileUseCollider` の扱いを決定
- `ShootingState.Projectile` の bool 解釈を決定
- `useExplodeRotation` の最低限対応または明示的未対応化
- `AmmoType` の短期スコープ要否判断

**後回し(スコープ外)**:
`useBeacon` / `GuidRatio` / `GuidType` / ミサイル誘導本体 / 燃料消費 / ドローン連携 / チャフ /
`ShootingState.Buoyancy` / 水面・浮力絡み弾体挙動
→ これらは Core 契約 / World / 水面 / 誘導戦略 / 外部 API と接続しやすく、
  先に実装すると MP 安定化より広い設計問題へ拡散する。

---

### D1: Weapon API / Core 設計チェックポイント

> 詳細評価メモ → `docs/future-weapon-plugin-restructure.md`

**概要**: 実装タスクではなく**設計判断タスク**。Core / PluginApi 変更は契約凍結のためオーナー同意必須。

**主な論点**:
- `WeaponSpec` を薄い共通スペックとして維持するか / 拡張スロット契約を追加するか
  (`docs/future-weapon-plugin-restructure.md` §2)
- `ACMu.Compat/Shooting` を `Common/` と `OldCannon/` に物理分割するか(§3 B-2 → 必要なら B-1)
- `IGuidanceStrategy` の ProjectileService 側での扱い(ミサイル実装前の準備)
- 「Compat は PluginApi 利用者」方針と「Compat が ACMu.Weapons へ依存」の現状整理

**現時点の仮推奨**(設計セッションで確定前):
- `WeaponSpec` は薄く保つ。旧ACM固有値は `OldCannonModule` / Compat 側に置く。
- 先に `ACMu.Compat/Shooting` を Common / OldCannon に物理分割し、その後必要な部品だけ PluginApi へ昇格。
- この方針なら契約変更を最小化しつつ、将来の外部 API 設計を壊しにくい。

---

### M6: World 拡張EX

> 詳細仕様・設計メモ → [Issue #5](https://github.com/EEX-bsg/ACMu-Besiege/issues/5)

**概要**: Vector3d ワイヤ座標 + IWorldFrame 実装で 4km 境界を越える。
着手前に「既存ACMの黒魔術の仕組み」をユーザーから聞き取ること(ROADMAP旧§M6 参照)。

**着手前に整理が必要な論点**:
- 既存ACMがどのように4km境界を突破していたか、ユーザーから聞き取る。
- `NetworkCompression.SetWorldBounds` 相当の扱いを確認する。
- `IWorldFrame` の実装を `ACMu.World` として追加するか決める。
- 弾体同期・将来の水面同期・ブロック同期が `IWorldFrame.ToWorld/ToScene` を通るよう整理する。
- origin shift が必要か、単に巨大 Bounds / 拡張座標圧縮だけで足りるかを判断する
  → 詳細: `docs/open-design-questions.md` §World拡張の実装方式

**現状の制約**:
`IAcmuServices.World` は `NullWorldFrame`(Origin=Zero、変換=恒等)。
弾体同期は `Vector3d` を使っているが `IWorldFrame` を経由していない。
M4.1 で「弾体同期の座標変換境界」を先に確立しておくことで、
M6 で WorldFrame の中身を差し替えた時に弾体同期側の再設計が最小になる。

**受け入れ基準**:
- [ ] MP で 4km 境界を越えて相互に座標が正しく見える(実機)

---

### AdBlockProp: 汎用ブロックモジュール契約設計

> 詳細 → [Issue #6](https://github.com/EEX-bsg/ACMu-Besiege/issues/6)

**概要**: ConfigurableJoint / SpringMotion / RotateMotion 等を持つ汎用ブロック物理モジュール。
現在の Core/PluginApi には IWeaponHost/IWeaponRegistry 以外の BlockModule ホスト契約が存在しない。
**実装着手は設計セッションで契約が固まってから。**

**設計選択肢**:
- A) 新しい `IBlockModuleHost` / `IBlockModuleRegistry` 系契約を Core / PluginApi に追加する。**現時点の推奨寄り。**
- B) `IWeaponHost` を広げて汎用ブロックにも使う。「武器」と「汎用ブロック物理」が混ざり API 利用者に分かりにくい。
- C) Compat 内で例外的に Adapter へ近づける。依存規律(CLAUDE.md §0-2)を崩し、他モジュールへの拡張が困難になる。

A は Core / PluginApi 契約変更を伴うため、MP安定化(M5.1→M4.1)と API チェックポイント(D1)の後に設計セッションとして扱う。

---

## 現在避けたい進め方

- M5 実機確認を飛ばして M6 へ入る。
- FireReq の仕様を固めないまま AdShooting の未実装機能を大量に足す。
- `WeaponSpec` に旧ACM固有値を便利に追加していく。
- `IsHost` を武装実行権威として扱う(武装・物理・ローカル弾体は `IGameSessionInfo.IsSimulating` を使う)。
- `IWorldFrame` を使わないまま、World 拡張前提の通信を増やす。
- Compat から Adapter や本体デコンパイル由来クラスへ直接依存する。
- AdBlockProp を契約なしで局所実装する。

---

## 運用ルール

- 各M完了時: 人間が実機確認 → 余裕があれば Opus に差分レビュー1回 → 次へ
- M内で契約の不備が見つかったら: 実装を止めて報告(CLAUDE.md ルール1)
- M2 と M3 は依存が薄いので、並行セッションで走らせてもよい
- コミットメッセージ: `M<番号>: <変更内容1行>`

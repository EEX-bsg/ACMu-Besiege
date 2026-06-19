# ROADMAP — マイルストーン一覧

> 着手順は M0 → M1 → M2 → M3 → M4 → M5 → M6。
> 大きなタスクの詳細仕様は GitHub Issue で管理する。

---

## 完了済み

| マイルストーン | 内容 | 完了日 |
|---|---|---|
| M0 | ビルド基盤 | — |
| M1 | Host骨格 + Adapter | — |
| M2 | 武装縦切り1本(シングルプレイ) | — |
| M3 | INetworkTransport 実装 | — |
| M4 | 弾体 MP 同期 | 2026-06-15 |
| M5 | AdShootingProp 互換実装 + 追加機能 | 2026-06-17 |

---

## 未着手

### M6: World 拡張EX

> 詳細仕様・設計メモ → [Issue #5](https://github.com/EEX-bsg/ACMu-Besiege/issues/5)

**概要**: Vector3d ワイヤ座標 + IWorldFrame 実装で 4km 境界を越える。
着手前に「既存ACMの黒魔術の仕組み」をユーザーから聞き取ること(ROADMAP旧§M6 参照)。

**受け入れ基準**:
- [ ] MP で 4km 境界を越えて相互に座標が正しく見える(実機)

---

### AdBlockProp: 汎用ブロックモジュール契約設計

> 詳細 → [Issue #6](https://github.com/EEX-bsg/ACMu-Besiege/issues/6)

**概要**: ConfigurableJoint / SpringMotion / RotateMotion 等を持つ汎用ブロック物理モジュール。
現在の Core/PluginApi には IWeaponHost/IWeaponRegistry 以外の BlockModule ホスト契約が存在しない。
**実装着手は設計セッションで契約が固まってから。**

---

## 運用ルール

- 各M完了時: 人間が実機確認 → 余裕があれば Opus に差分レビュー1回 → 次へ
- M内で契約の不備が見つかったら: 実装を止めて報告(CLAUDE.md ルール1)
- M2 と M3 は依存が薄いので、並行セッションで走らせてもよい
- コミットメッセージ: `M<番号>: <変更内容1行>`

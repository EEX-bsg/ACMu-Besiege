# ACMu 実装進捗

最終更新: 2026-06-18

---

## 実機確認: 残り項目

### M4 実機確認(未実施 — M4.1 で消化)

> M4 は実装完了とされているが実機確認が行われていない。
> 詳細設計: `docs/network/m4-projectile-sync-design.md` §8

- [ ] `Modding.Game.IsSimulatingLocal` が「観戦中=false / ローカルシミュ中=true」で動く(§6 前提)
- [ ] クライアントの発射キーがホスト側ブロックで `IsPressed` になる(§5 案A の前提。ならなければ FireReq 方式へフォールバック)
- [ ] ラグ模擬で連射してもクライアントに弾が残留しない(AliveList 孤児掃除の確認)
- [ ] ホストとクライアントで同時刻の弾数が一致に収束する(ログ比較)
- [ ] 高速弾の見た目が着弾位置とズレない(速度外挿の効果確認)

### M5 本体

- [ ] カプセルコライダー(弾頭)が壁に当たって衝突判定が取れる
- [ ] MP: 弾体スポーン・フューズ爆発がホスト・クライアント双方で見える

### M5 追加機能(2026-06-16)

- [ ] ECannon.xml: PowerSlider が連続推力の大きさを兼ねていることを確認
- [ ] 発射直後の 0.02 秒間、弾頭が発射元ブロックに自爆判定しない(衝突有効化遅延)
- [ ] `ShootingState.Attaches=true` で、命中対象に弾が刺さって止まり、シミュ終了時にクラッシュしない
- [ ] `useFreezingAttack=true` で、命中したブロック(と子ブロック)が凍結する

### M5.1: Issue 再現条件固定(追加 2026-06-19)

M4.1 修正の前に再現条件を文書化する。修正はしない。

- [ ] Issue #2 MP描画同期: どの条件で再現するか特定する(ホスト/クライアント別、弾種別など)
- [ ] Issue #4 弾体の回転が大きい: どの条件で再現するか特定する(弾速・距離・弾種など)

---

## ビルドメモ

### ビルドコマンド(VS MSBuild 推奨)

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" src/acmu/acmu.csproj /p:Configuration=Release /nologo /v:minimal
```

### git コミット(PowerShell)

```powershell
git commit -m @'
コミットメッセージ
'@
```

---

## 既知の落とし穴

- **ModIO 名前空間の衝突**: `Modding.ModIO.ExistsFile()` — 完全修飾必須
- **`CollisionTypes` の語尾**: `<CollisionTypes>ContinuousDynamic</CollisionTypes>` — XML 値は末尾が小文字 s。C# フィールド名 `CollisionTypes` と異なるので混同注意

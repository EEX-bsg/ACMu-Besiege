# ACMu 実装進捗

最終更新: 2026-06-18

---

## 実機確認: 残り項目

### M5 本体

- [ ] カプセルコライダー(弾頭)が壁に当たって衝突判定が取れる
- [ ] MP: 弾体スポーン・フューズ爆発がホスト・クライアント双方で見える

### M5 追加機能(2026-06-16)

- [ ] ECannon.xml: PowerSlider が連続推力の大きさを兼ねていることを確認
- [ ] 発射直後の 0.02 秒間、弾頭が発射元ブロックに自爆判定しない(衝突有効化遅延)
- [ ] `ShootingState.Attaches=true` で、命中対象に弾が刺さって止まり、シミュ終了時にクラッシュしない
- [ ] `useFreezingAttack=true` で、命中したブロック(と子ブロック)が凍結する

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

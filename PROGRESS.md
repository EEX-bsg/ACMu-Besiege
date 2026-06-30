# ACMu 実装進捗

最終更新: 2026-06-30

---

## 実機確認: 残り項目

### M4 実機確認(未実施 — U1 対応後に M4.1 で消化)

> M4 は実装完了とされているが実機確認が行われていない。
> 詳細設計: `docs/network/m4-projectile-sync-design.md` §8
> **⚠ 2026-06-30: Besiege アップデートで MP が完全に壊れているため以下は全面ブロック中。U1 で修正してから再確認する。**

- [ ] `Modding.Game.IsSimulatingLocal` が「観戦中=false / ローカルシミュ中=true」で動く(§6 前提) ← MP ブロック
- [ ] クライアントの発射キーがホスト側ブロックで `IsPressed` になる(§5 案A の前提。ならなければ FireReq 方式へフォールバック) ← MP ブロック
- [ ] ラグ模擬で連射してもクライアントに弾が残留しない(AliveList 孤児掃除の確認) ← MP ブロック
- [ ] ホストとクライアントで同時刻の弾数が一致に収束する(ログ比較) ← MP ブロック
- [ ] 高速弾の見た目が着弾位置とズレない(速度外挿の効果確認) ← SP では問題なさそう。MP は未確認

### M5 本体

- [x] カプセルコライダー(弾頭)が壁に当たって衝突判定が取れる — **確認済み**
- [ ] MP: 弾体スポーン・フューズ爆発がホスト・クライアント双方で見える ← MP ブロック

### M5 追加機能

- [x] ECannon.xml: PowerSlider が連続推力の大きさを兼ねていることを確認 — **確認済み**
- [x] 発射直後の 0.02 秒間、弾頭が発射元ブロックに自爆判定しない(衝突有効化遅延) — **それらしき挙動あり・確認済み**
- [x] `ShootingState.Attaches=true` で、命中対象に弾が刺さって止まり、シミュ終了時にクラッシュしない — **確認済み**
- [ ] `useFreezingAttack=true` で、命中したブロック(と子ブロック)が凍結する — 未確認(優先度低)

### M5.1: Issue 再現条件固定(U1 対応後に着手)

M4.1 修正の前に再現条件を文書化する。修正はしない。
**⚠ MP ブロック中のため U1 修正後に着手する。**

- [ ] Issue #2 MP描画同期: どの条件で再現するか特定する(ホスト/クライアント別、弾種別など) ← MP ブロック
- [ ] Issue #4 弾体の回転が大きい: どの条件で再現するか特定する(弾速・距離・弾種など)

### 武装登録 Mod 名義(案C:ModuleRegistrar デリゲート登録)

> 設計根拠: ARCHITECTURE.md §3-1 / メモ weapon-registry-mod-identity。
> `AddBlockModule` をプラグインのデリゲート本体から呼ぶことで、武装ブロックが提供プラグイン自身の Mod 名義で登録されるようにした実装。

- [ ] **【最重要】**外部プラグインから登録した武装ブロックを置いたマシンを保存し、セーブ XML の `modid` がそのプラグインの GUID になる(= `GetCallingAssembly()` がデリゲート経由でプラグイン .dll を返す)。ACMu 名義のままなら案A(2分割: プラグインが AddBlockModule を直接呼ぶ)へフォールバックが必要
- [ ] 既存 Compat ブロック(AdShootingProp / AcmuTestCannon)が従来どおり配置・発射・MP 同期できる(名義は ACMu のままなので回帰しない想定)
- [ ] 別 Mod 名義ブロックを含むマシンの MP Mod 不一致ダイアログ挙動が正しい(プラグイン未所持ピアで適切に検出される)

### オーナー確認(マイルストーン境界でチェック)

- [ ] **canReload の適正値**: 旧コードが `MultiplayerCompatible` を `AddBlockModule` の `canReload` 引数に誤渡ししていた件。現状は挙動を変えないため旧来の実効値(OldCannon=`true` / TestCannon=`false`)を明示踏襲しているだけ。本来の hot-reload 可否として正しい値に直すか、オーナー判断が必要(該当: `AcmuCoreBootstrap.cs` の各 `ModuleRegistrar`)

---

## Besiege アップデート影響確認(2026-06-30)

### 🔴 MP クラッシュ(最優先)

**症状**: ホスト側で `NullReferenceException` が `Modding.ModNetworking.FragmentMessage` 内で発生。クライアント側はエラーなし。

**スタックトレース(代表)**:
```
ProjectileSyncTransport.Update()
  → SendAliveList()
  → ModNetTransport.Send()
  → ModNetworking.SendTo(Player, Message)
  → ModNetworking.SendMessage(Message)
  → ModNetworking.FragmentMessage(byte[], RPCDestination)  ← NullRef ここ
```

および

```
ProjectileSyncTransport.OnProjectileSpawn(ProjectileHandle)
  → (ProjectileSpawnRequest)
  → FirePipeline+<DelayedFire>d__11.MoveNext()
  → ModNetTransport.Send()
  → ModNetworking.SendTo(Player, Message)
  → ModNetworking.FragmentMessage(byte[], RPCDestination)  ← NullRef ここ
```

**推定原因**: Besiege アップデートで `ModNetworking.FragmentMessage` の内部実装または `RPCDestination` の扱いが変わった。送信先プレイヤーのコネクション状態が null になっている可能性。

**対応**: → U1 で調査・修正。Cecil で新 DLL を検査すること。

---

### 🟡 エフェクト残留バグ

**症状**: 弾が衝突した後もエフェクト描画が消えずに残り続ける。アップデート前は発生していなかった。

**推定原因**: Besiege アップデートでパーティクルシステムの破棄タイミングまたは `GameObject.Destroy` の挙動が変わった可能性。ACMu のエフェクト管理コードとの齟齬。

**対応**: → U1 または M5.2 で調査。MP 修正より優先度はやや低いが、可視性に関わるため早期に見る。

---

### ⚪ 起動時エラー(ACMu 起因か不明)

**症状**:
```
System.UInt64.Parse(string)
InternalModding.Mods.ModList+Mod.FromString(string)
InternalModding.Loading.Maintenance.Initialize()
```

**評価**: Besiege 内部の `InternalModding` 内エラーで ACMu のコードは含まれない。
Besiege アップデートで Mod リスト形式が変わった可能性。ACMu が原因でない可能性が高い。
ACMu 以外の他 Mod を無効化して再現するか確認できれば切り分け可能。

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

# ACMu (AddCustomModule 非公式)

ACMu は Besiege Mod「AddCustomModule」の非公式フォークModです。
武装プラグインAPIや、ボーダーレスワールド基盤等を開発しています。

本プロジェクトはオリジナルの開発者であるsukepin氏に最大限の敬意を払って開発しています。
本プロジェクトはオリジナルのACMプロジェクトと一切関係がありません。
本プロジェクトについて、オリジナル作者・関係者へ問い合わせることはお控えください。

現在はα版です。通常のゲームプレイで使える状態ではありません。
本プロジェクトはオリジナルとの完全な後方互換を目指すものではありません。
ソースコード公開にあたり、オリジナル資産は流用しません。誤って含めないように注意してください。

## 開発におけるルール

実装作業の前に、少なくとも次を読んでください。

- `CLAUDE.md`
- `ARCHITECTURE.md`
- `ROADMAP.md`
- `PROGRESS.md`

`CLAUDE.md` は現行の主要な AI エージェント運用ルールです。
別の AI エージェントを使う場合は、そのエージェント向けの常駐ルール文書も同じ内容に揃えてください。
AI エージェント向け常駐ルール文書を追加・変更する場合は、既存の主要ルール文書と内容が食い違わないようにしてください。
個人環境だけで使うエージェント設定や一時メモは、原則として PR から除外してください。

## 重要な制約

- 明示的な設計判断なしに `src/acmu/ACMu.Core/` と `src/acmu/ACMu.PluginApi/` を変更しない。
- `BlockBehaviour`、`StatMaster`、`Machine`、`NetworkCompression` などの Besiege 本体/デコンパイル由来クラス参照は `src/acmu/ACMu.Adapter/` に隔離する。
- C# は C# 6 / .NET 3.5 / Unity 5.4 世代の API に制限する。
- `System.Reflection`、禁止ファイル/ネットワーク API、Harmony 的パッチは禁止。
- 許諾の範囲外でBesiegeやオリジナルACMの著作物を含めたり流用しないよう注意。

## ライセンス
All Rights Reserved
※将来的にオープンソースライセンスに移行予定

---
<div align="center">
<sub>開発: EEX-slime</sub>
<br>
<sub>BESIEGE © Spiderling Studios</sub>
</div>

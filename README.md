# PDFハンドラ

[![GitHub release](https://img.shields.io/github/v/release/6EFB0D/pdf-handler?style=flat-square)](https://github.com/6EFB0D/pdf-handler/releases/latest)
[![GitHub all releases](https://img.shields.io/github/downloads/6EFB0D/pdf-handler/total?style=flat-square&label=累計ダウンロード)](https://github.com/6EFB0D/pdf-handler/releases)
[![License](https://img.shields.io/github/license/6EFB0D/pdf-handler?style=flat-square)](LICENSE)

ファイルサーバ上の PDF ファイルを効率よく管理・編集できる Windows デスクトップアプリです。

---

## 主な機能

- **PDF プレビュー＆サムネイル** — フォルダツリーで PDF を選ぶだけで即プレビュー
- **PDF 結合** — 複数ファイルをドラッグ順で結合
- **PDF 分割** — ページ範囲・1 ページずつ・等分割
- **ファイル名変更** — ロックを回避してその場でリネーム
- **お気に入りフォルダ** — よく使うフォルダを登録してすぐアクセス

---

## ダウンロード

**[最新版インストーラをダウンロード](https://github.com/6EFB0D/pdf-handler/releases/latest)**

| 項目 | 内容 |
|---|---|
| 対応 OS | Windows 10 / 11 (64 bit) |
| ランタイム | .NET 8（インストーラに同梱・別途不要） |
| インストール先 | `%LocalAppData%\Office Go Plan\PDFハンドラ\`（管理者権限不要） |

---

## インストール手順

1. **[Releases ページ](https://github.com/6EFB0D/pdf-handler/releases/latest)** から `PdfHandler-*-prod-setup.exe` をダウンロード
2. ダウンロードした `setup.exe` をダブルクリック
3. 画面の指示に従ってインストール完了
4. スタートメニュー「Office Go Plan → PDFハンドラ」から起動

> Windows Defender のスマートスクリーンが表示された場合は「詳細情報」→「実行」で続行できます。

---

## チェックサム検証（オプション）

リリースページに掲載の SHA-256 と照合することで、ダウンロードファイルの完全性を確認できます。

```powershell
Get-FileHash -Path .\PdfHandler-*-prod-setup.exe -Algorithm SHA256
```

---

## ライセンス認証について

本アプリは **無料試用期間**（14 日間）のあと、Standard 版ライセンスキーが必要になります。

- ライセンスは **アプリ内の購入フォーム**から購入できます（クレジットカード・銀行振込対応）
- 購入後、ライセンスキーがご登録メールアドレスへ自動送付されます
- 1 ライセンスで最大 3 台まで利用可能

---

## サポート・お問い合わせ

| 種別 | 連絡先 |
|---|---|
| 不具合報告・機能要望 | [GitHub Issues](https://github.com/6EFB0D/pdf-handler/issues) |
| ライセンス・購入・返金 | support@office-goplan.com |
| その他お問い合わせ | support@office-goplan.com |

---

## 使用ライブラリ（オープンソース）

| ライブラリ | ライセンス |
|---|---|
| PdfSharp 6.1.1 | MIT |
| Docnet.Core 2.6.0 | MIT |
| CommunityToolkit.Mvvm 8.2.2 | MIT |
| Microsoft.Extensions.DependencyInjection 8.0.0 | MIT |

---

## バージョン履歴

[CHANGELOG.md](CHANGELOG.md) を参照してください。

---

**開発・販売**: [Office Go Plan](mailto:support@office-goplan.com)

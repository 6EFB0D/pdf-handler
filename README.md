# PDFハンドラ

[![GitHub release](https://img.shields.io/github/v/release/6EFB0D/pdf-handler?style=flat-square)](https://github.com/6EFB0D/pdf-handler/releases/latest)
[![License](https://img.shields.io/github/license/6EFB0D/pdf-handler?style=flat-square)](LICENSE)

**PDFハンドラ**は、ファイルサーバーやローカルフォルダ上の PDF を、Windows デスクトップから効率よく閲覧・整理・編集するためのアプリケーションです。

| | |
|---|---|
| **製品紹介・購入** | [Office Go Plan](https://office-goplan.com) |
| **ダウンロード** | [GitHub Releases](https://github.com/6EFB0D/pdf-handler/releases/latest)（インストーラのみ） |
| **お問い合わせ** | [Google フォーム](https://docs.google.com/forms/d/1NpXzk1kyUn2LhUzQhhMHq_tnT1oOGAsv561L-7nMfos/viewform) |

## できること

| 機能 | 説明 |
|------|------|
| PDF プレビュー | フォルダを開き、一覧から PDF を選んで右ペインに表示 |
| ファイル名変更 | プレビュー中でも F2 でリネーム（ファイルロックを回避） |
| PDF 結合・分割 | 複数 PDF の結合、ページ指定での分割 |
| フォルダツリー | 階層表示・ドラッグ＆ドロップでのコピー／移動 |
| ライセンス | 14 日間の試用のあと、アプリ内から買い切りライセンスを購入可能 |

## インストール

1. [Releases ページ](https://github.com/6EFB0D/pdf-handler/releases/latest) を開く
2. Assets から **`PdfHandler-<version>-prod-setup.exe`** をダウンロード（例: `PdfHandler-1.1.3-prod-setup.exe`）
3. ダウンロードしたファイルを実行し、画面の指示に従ってインストール（per-user・管理者権限不要）
4. スタートメニューまたはデスクトップのショートカットから「PDFハンドラ」を起動

必要に応じて、同梱の `PdfHandler-<version>-prod-setup-checksum.txt` で SHA-256 を確認できます。

> **配布物はインストーラ（setup.exe）のみです。** 展開して使う ZIP 版は公開していません。

### システム要件

- Windows 10 / 11（64bit）
- .NET 8.0 Runtime（インストーラに同梱のため、通常は別途インストール不要）

### ソフトウェアの更新

- 起動時に新しいバージョンがあればお知らせします（自動ダウンロード・インストールは行いません）
- **ヘルプ → バージョン情報** で更新を確認できます

## ライセンス・試用

- **Standard 版（買い切り）**: ¥4,980（消費税不課税）— 詳細は [製品ページ](https://office-goplan.com) またはアプリ内「購入」
- **試用期間**: 初回起動から 14 日間、全機能を無料でお試しいただけます
- 購入ガイド: [docs/user-guide/payment-guide.md](docs/user-guide/payment-guide.md)

## 変更履歴

[CHANGELOG.md](CHANGELOG.md) を参照してください。

## サポート

不具合報告・機能要望・お問い合わせは **GitHub Issues では受け付けていません**。

- **[お問い合わせフォーム（Google フォーム）](https://docs.google.com/forms/d/1NpXzk1kyUn2LhUzQhhMHq_tnT1oOGAsv561L-7nMfos/viewform)**
- メール: [support@office-goplan.com](mailto:support@office-goplan.com)

アプリ内の **ヘルプ → お問い合わせフォーム** からも同じフォームを開けます。

---

<details>
<summary>開発者向け（ソースからビルドする場合）</summary>

```powershell
dotnet build PdfHandler.sln
.\scripts\build-release.ps1 -TargetEnvironment PROD
.\tools\build-release.ps1 -TargetEnvironment PROD
```

詳細は Private リポジトリ内の `docs/release-artifact-layout.md` を参照してください。

</details>

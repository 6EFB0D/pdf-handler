# PDFハンドラ

[![GitHub release](https://img.shields.io/github/v/release/6EFB0D/pdf-handler?style=flat-square)](https://github.com/6EFB0D/pdf-handler/releases/latest)
[![License](https://img.shields.io/github/license/6EFB0D/pdf-handler?style=flat-square)](LICENSE)

**PDFハンドラ**は、ファイルサーバーやローカルフォルダ上の PDF を、Windows デスクトップから効率よく閲覧・整理・編集するためのアプリケーションです。

- **製品紹介・購入**: [Office Go Plan（製品ページ）](https://office-goplan.com)
- **ダウンロード（最新版）**: [GitHub Releases](https://github.com/6EFB0D/pdf-handler/releases/latest)
- **お問い合わせ**: [support@office-goplan.com](mailto:support@office-goplan.com)

## できること

| 機能 | 説明 |
|------|------|
| PDF プレビュー | フォルダを開き、一覧から PDF を選んで右ペインに表示 |
| ファイル名変更 | プレビュー中でも F2 でリネーム（ファイルロックを回避） |
| PDF 結合・分割 | 複数 PDF の結合、ページ指定での分割 |
| フォルダツリー | 階層表示・ドラッグ＆ドロップでのコピー／移動 |
| ライセンス | 14 日間の試用のあと、アプリ内から買い切りライセンスを購入可能 |

## ダウンロードとインストール

**推奨**: [Releases ページ](https://github.com/6EFB0D/pdf-handler/releases/latest) の Assets から **インストーラ** を取得してください。

| ファイル | 用途 |
|----------|------|
| `PdfHandler-<version>-prod-setup.exe` | **通常はこちら**（例: `PdfHandler-1.1.3-prod-setup.exe`） |
| `PdfHandler-<version>-prod-setup-checksum.txt` | インストーラの SHA-256（任意） |
| `PdfHandler-<version>-win-x64.zip` | **インストーラが SmartScreen 等でブロックされる場合のみ** |
| `PdfHandler-<version>-win-x64-checksum.txt` | ZIP の SHA-256（任意） |

### インストール手順（setup.exe）

1. `PdfHandler-<version>-prod-setup.exe` をダウンロード
2. 実行し、画面の指示に従ってインストール（per-user・管理者権限不要）
3. スタートメニューまたはデスクトップのショートカットから「PDFハンドラ」を起動

### インストーラがブロックされる場合（ZIP 版）

セットアップ EXE の実行が Windows Defender や SmartScreen で止まる場合のみ、同じ Release の **ZIP 版** を使います。

1. `PdfHandler-<version>-win-x64.zip` をダウンロード
2. 任意のフォルダに展開（例: `C:\Tools\PdfHandler`）
3. 展開フォルダ内の `PdfHandler.UI.exe` を起動

> ZIP 版はインストール（ショートカット作成等）を行いません。通常の利用は **setup.exe** をお使いください。

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

- [GitHub Issues](https://github.com/6EFB0D/pdf-handler/issues) — 不具合報告・機能要望
- [support@office-goplan.com](mailto:support@office-goplan.com)

---

## 開発者向け（ソースからビルドする場合）

<details>
<summary>クリックして展開</summary>

### プロジェクト構成

```
pdf-handler/
├── PdfHandler.sln
├── src/
│   ├── PdfHandler.UI/          # WPF UI
│   ├── PdfHandler.Core/        # ビジネスロジック
│   └── PdfHandler.Infrastructure/
└── scripts/                    # ビルド・デプロイ用
```

### 前提条件

- Visual Studio 2022 以上、または .NET 8.0 SDK

### ビルド

```powershell
dotnet build PdfHandler.sln
```

### リリースビルド（PROD）

```powershell
.\scripts\build-release.ps1 -TargetEnvironment PROD
.\tools\build-release.ps1 -TargetEnvironment PROD
```

成果物の配置は [docs/release-artifact-layout.md](docs/release-artifact-layout.md) を参照。

</details>

# PDFハンドラ デスクトップアプリケーション

[![GitHub release](https://img.shields.io/github/v/release/6EFB0D/pdf-handler?style=flat-square)](https://github.com/6EFB0D/pdf-handler/releases/latest)
[![GitHub all releases](https://img.shields.io/github/downloads/6EFB0D/pdf-handler/total?style=flat-square&label=total%20downloads)](https://github.com/6EFB0D/pdf-handler/releases)
[![GitHub release downloads](https://img.shields.io/github/downloads/6EFB0D/pdf-handler/latest/total?style=flat-square&label=latest%20downloads)](https://github.com/6EFB0D/pdf-handler/releases/latest)
[![License](https://img.shields.io/github/license/6EFB0D/pdf-handler?style=flat-square)](LICENSE)

ファイルサーバ上のPDFファイルを効率的に管理・編集するためのWPFデスクトップアプリケーション

## プロジェクト構成

```
pdf-handler/
├── PdfHandler.sln              # ソリューションファイル
├── src/
│   ├── PdfHandler.UI/          # WPF UIプロジェクト
│   │   ├── Views/              # XAMLビュー
│   │   ├── ViewModels/         # ViewModelクラス
│   │   └── Converters/         # 値コンバーター
│   ├── PdfHandler.Core/        # ビジネスロジック
│   │   ├── Models/             # ドメインモデル
│   │   └── Interfaces/         # サービスインターフェース
│   └── PdfHandler.Infrastructure/  # インフラストラクチャ
│       └── Services/           # サービス実装
└── README.md
```

## 技術スタック

- **開発言語**: C# 10
- **フレームワーク**: .NET 8.0
- **UI**: WPF (Windows Presentation Foundation)
- **アーキテクチャ**: MVVM (Model-View-ViewModel)
- **DIコンテナ**: Microsoft.Extensions.DependencyInjection
- **MVVMツールキット**: CommunityToolkit.Mvvm
- **PDFライブラリ**:
  - **PdfSharp 6.1.1** (PDF操作・結合・分割) - MIT License
  - **Docnet.Core 2.6.0** (PDF表示・サムネイル生成) - MIT License
  - System.Drawing.Common (画像処理)

すべてMITライセンスのライブラリを使用しており、**完全無償・商用利用可能**です。

## 主要機能

### 1. PDFプレビュー＆ファイル名変更
- フォルダ階層のツリー表示
- PDFファイルのサムネイル/リスト表示
- PDFプレビュー表示（ON/OFF切替可能）
- ファイルロックを回避したファイル名変更

### 2. PDF結合
- 複数PDFファイルの結合
- 結合順序の調整
- 進捗表示

### 3. PDF分割
- ページ範囲指定分割
- 1ページずつ分割
- 等分割

## ビルド方法

### 前提条件
- Visual Studio 2022以上
- .NET 8.0 SDK以上

### ビルド手順

1. ソリューションを開く
```bash
cd pdf-handler
start PdfHandler.sln
```

2. Visual Studioでビルド
- メニューから「ビルド」→「ソリューションのビルド」を選択
- またはCtrl+Shift+B

3. コマンドラインからビルド
```bash
dotnet build PdfHandler.sln
```

## 実行方法

### Visual Studioから実行
1. スタートアッププロジェクトを `PdfHandler.UI` に設定
2. F5キーで実行

### コマンドラインから実行
```bash
cd src/PdfHandler.UI
dotnet run
```

## アーキテクチャ概要

### レイヤー構成

```
┌─────────────────────────────┐
│  Presentation Layer (UI)    │  WPF Views + ViewModels
├─────────────────────────────┤
│  Application Layer (Core)   │  Business Logic + Interfaces
├─────────────────────────────┤
│  Infrastructure Layer       │  File I/O + PDF Operations
└─────────────────────────────┘
```

### 主要クラス

#### Core Layer
- `PdfFileInfo`: PDFファイル情報モデル
- `FolderNode`: フォルダツリーノードモデル
- `IFileService`: ファイル操作サービスインターフェース
- `IPdfService`: PDF操作サービスインターフェース
- `IPdfMergeService`: PDF結合サービスインターフェース
- `IPdfSplitService`: PDF分割サービスインターフェース

#### Infrastructure Layer
- `FileService`: ファイル操作の実装
- `PdfService`: PDF基本操作の実装
- `PdfMergeService`: PDF結合の実装
- `PdfSplitService`: PDF分割の実装

#### UI Layer
- `MainWindowViewModel`: メインウィンドウのViewModel
- `MainWindow`: メインウィンドウのView

## 開発状況

### 実装済み機能（v4.0.0）
- ✅ プロジェクト構造の確立（3層アーキテクチャ）
- ✅ 基本UI (3ペイン構成)
- ✅ フォルダツリー表示
- ✅ サムネイル/リスト表示切替
- ✅ プレビューON/OFF切替
- ✅ DIコンテナによる依存性注入
- ✅ MVVMパターンの実装
- ✅ PdfSharpによるPDF結合機能
- ✅ PdfSharpによるPDF分割機能
- ✅ Docnet.CoreによるPDFレンダリング（実際のPDF表示）
- ✅ サムネイル生成（第1ページ）
- ✅ ファイル名変更（F2キー、インライン編集）
- ✅ ファイルロック回避（メモリストリーム方式）
- ✅ お気に入りフォルダ管理
- ✅ PDF結合・分割ダイアログUI

### 実装予定機能
- 🔲 ページ抽出・回転・削除機能
- 🔲 注釈機能（ハイライト、テキストボックス、手書き）
- 🔲 ドラッグ&ドロップ対応
- 🔲 ページサムネイル一覧表示

## ライセンス情報

### 使用ライブラリ
- **CommunityToolkit.Mvvm**: MIT License
- **PdfSharp 6.1.1**: MIT License（完全無償・商用利用可能）
- **Docnet.Core 2.6.0**: MIT License
- **System.Drawing.Common**: MIT License

## 注意事項

### PDFライブラリについて
- **PdfSharp 6.1.1**: PDF操作（結合、分割）に使用。MITライセンスのため商用利用も完全無償
- **Docnet.Core 2.6.0**: PDFレンダリング・サムネイル生成に使用。Google PDFiumベース
- **System.Drawing.Common**: 画像処理に使用

### ファイルロック対策
- PDFをメモリに読み込むことでファイルロックを回避
- 大容量PDFの場合はメモリ使用量に注意

## トラブルシューティング

### ビルドエラーが発生する場合
1. NuGetパッケージの復元を実行
```bash
dotnet restore
```

2. .NET SDKのバージョンを確認
```bash
dotnet --version
```

### 実行時エラーが発生する場合
- フォルダへのアクセス権限を確認
- PDFファイルが他のアプリケーションで開かれていないか確認

## 参考資料

- [WPF公式ドキュメント](https://docs.microsoft.com/wpf/)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/windows/communitytoolkit/mvvm/introduction)
- [PdfSharp Documentation](https://www.pdfsharp.net/)
- [Docnet.Core GitHub](https://github.com/GowenGit/docnet)
- [PDFium](https://pdfium.googlesource.com/pdfium/)

## 貢献

プロジェクトへの貢献を歓迎します。Issue報告やPull Requestをお待ちしています。

## サポート・お問い合わせ

バグ報告や機能要望は、[GitHub Issues](https://github.com/6EFB0D/pdf-handler/issues) にてお願いします。

---

**バージョン**: 4.0.0
**最終更新**: 2026年1月1日

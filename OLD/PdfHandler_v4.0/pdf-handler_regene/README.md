# PDFハンドラ デスクトップアプリケーション

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
  - **iText 7.2.5** (PDF操作・結合・分割) ⚠️ **重要: 8.x系は使用不可**
  - Docnet.Core 2.6.0 (PDF表示・サムネイル生成)
  - System.Drawing.Common (画像処理)

**注意:** iText 8.x系はSmartModeが強制的に有効化されるため使用できません。必ず7.2.x系を使用してください。

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
- .NET 6.0 SDK以上

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

### 実装済み機能
- ✅ プロジェクト構造の確立
- ✅ 基本UI (3ペイン構成)
- ✅ フォルダツリー表示
- ✅ サムネイル/リスト表示切替
- ✅ プレビューON/OFF切替
- ✅ DIコンテナによる依存性注入
- ✅ MVVMパターンの実装
- ✅ iText7によるPDFページ数取得
- ✅ iText7によるPDF結合機能
- ✅ iText7によるPDF分割機能
- ✅ サムネイル生成（プレースホルダー）

### 実装予定機能
- 🔲 実際のPDFレンダリング（SkiaSharp等の統合）
- 🔲 ファイル名変更ダイアログ
- 🔲 PDF結合ダイアログUI
- 🔲 PDF分割ダイアログUI
- 🔲 プレビューのズーム機能
- 🔲 プレビューのページめくり機能
- 🔲 ドラッグ&ドロップ対応

## ライセンス情報

### 使用ライブラリ
- **CommunityToolkit.Mvvm**: MIT License
- **iText 7**: AGPL / Commercial License (商用利用の場合はライセンス購入が必要)
- **PdfSharpCore**: MIT License
- **System.Drawing.Common**: MIT License

## 注意事項

### PDFライブラリについて
- **iText 7**: PDF操作（ページ数取得、結合、分割）に使用（商用利用の場合はライセンスに注意）
- **System.Drawing.Common**: サムネイル生成に使用
- **PDFレンダリング**: 現在はプレースホルダー実装。実際のレンダリングにはSkiaSharp等の追加ライブラリが必要です

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

## 今後の開発予定

### Phase 1: PDF操作の実装
- PDFiumSharpの統合
- iText 7の統合
- 実際のPDFレンダリング

### Phase 2: ダイアログの実装
- カスタムダイアログウィンドウ
- PDF結合ダイアログ
- PDF分割ダイアログ

### Phase 3: 追加機能
- ページ抽出機能
- ページ順序変更機能
- バッチ処理機能

## 参考資料

- [WPF公式ドキュメント](https://docs.microsoft.com/wpf/)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/windows/communitytoolkit/mvvm/introduction)
- [iText 7 Documentation](https://itextpdf.com/products/itext-7)
- [PDFium](https://pdfium.googlesource.com/pdfium/)

## 貢献

プロジェクトへの貢献を歓迎します。Issue報告やPull Requestをお待ちしています。

## 連絡先

- プロジェクト管理者: PDFハンドラ開発チーム
- バージョン: 1.0
- 最終更新: 2024年12月26日

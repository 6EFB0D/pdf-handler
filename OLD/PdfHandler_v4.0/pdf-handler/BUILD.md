# PDFハンドラ ビルド・実行手順書

## 前提条件

### 必須環境
- **Windows 10/11**
- **Visual Studio 2022** (Community Edition以上)
  - ワークロード: `.NET デスクトップ開発`
- **.NET 6.0 SDK** 以上

### 環境確認

```bash
# .NET SDKバージョン確認
dotnet --version
# 6.0.x 以上であることを確認
```

## ビルド手順

### 方法1: Visual Studioを使用

1. **ソリューションを開く**
   ```
   PdfHandler.sln をダブルクリック
   ```

2. **NuGetパッケージの復元**
   - Visual Studioが自動的に復元を開始
   - または、メニューから `ツール` → `NuGetパッケージマネージャー` → `ソリューションのNuGetパッケージの管理` → `復元`

3. **ビルド**
   - メニューから `ビルド` → `ソリューションのビルド`
   - または `Ctrl + Shift + B`

4. **実行**
   - `F5` キーを押す
   - または `Ctrl + F5`（デバッグなし実行）

### 方法2: コマンドラインを使用

1. **ディレクトリに移動**
   ```bash
   cd pdf-handler
   ```

2. **NuGetパッケージの復元**
   ```bash
   dotnet restore
   ```

3. **ビルド**
   ```bash
   # Debugビルド
   dotnet build

   # Releaseビルド
   dotnet build --configuration Release
   ```

4. **実行**
   ```bash
   cd src/PdfHandler.UI
   dotnet run
   ```

## トラブルシューティング

### エラー: NuGetパッケージが復元できない

**症状:**
```
error NU1101: パッケージが見つかりません
```

**解決方法:**
```bash
# NuGetキャッシュをクリア
dotnet nuget locals all --clear

# 再度復元
dotnet restore
```

### エラー: .NET SDKが見つからない

**症状:**
```
error MSB4236: 指定された SDK "Microsoft.NET.Sdk" が見つかりませんでした
```

**解決方法:**
1. [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)をダウンロード
2. インストール
3. 再度ビルド

### エラー: System.Drawing.Commonが動作しない

**症状:**
```
System.TypeInitializationException: The type initializer for 'System.Drawing.Common' threw an exception
```

**解決方法:**
これはWindowsでは通常発生しませんが、もし発生した場合：

プロジェクトファイルに以下を追加:
```xml
<PropertyGroup>
  <EnableWindowsTargeting>true</EnableWindowsTargeting>
</PropertyGroup>
```

### 警告: NU1701（互換性警告）

**症状:**
```
warning NU1701: パッケージが異なるフレームワークで復元されました
```

**対処:**
これは警告であり、ビルドは成功します。無視して問題ありません。

## ビルド成果物の場所

### Debugビルド
```
src/PdfHandler.UI/bin/Debug/net6.0-windows/
```

### Releaseビルド
```
src/PdfHandler.UI/bin/Release/net6.0-windows/
```

## 実行ファイル

ビルド後、以下の実行ファイルが生成されます：
```
PdfHandler.UI.exe
```

## 配布用パッケージの作成

### 単一ファイルとして発行

```bash
dotnet publish src/PdfHandler.UI/PdfHandler.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

発行先:
```
src/PdfHandler.UI/bin/Release/net6.0-windows/win-x64/publish/
```

### フォルダとして発行

```bash
dotnet publish src/PdfHandler.UI/PdfHandler.UI.csproj -c Release -r win-x64 --self-contained true
```

## プロジェクト構成の確認

正しく展開されているか確認：

```
pdf-handler/
├── PdfHandler.sln
├── src/
│   ├── PdfHandler.Core/
│   │   └── PdfHandler.Core.csproj
│   ├── PdfHandler.Infrastructure/
│   │   └── PdfHandler.Infrastructure.csproj
│   └── PdfHandler.UI/
│       └── PdfHandler.UI.csproj
└── README.md
```

## 使用しているNuGetパッケージ

| パッケージ | バージョン | プロジェクト |
|-----------|-----------|-------------|
| itext7 | 8.0.2 | Infrastructure |
| PdfSharpCore | 1.3.65 | Infrastructure |
| System.Drawing.Common | 8.0.0 | Infrastructure |
| CommunityToolkit.Mvvm | 8.2.2 | UI |
| Microsoft.Extensions.DependencyInjection | 8.0.0 | UI |

## パフォーマンス

### ビルド時間（目安）
- 初回ビルド: 30秒〜1分
- インクリメンタルビルド: 5秒〜10秒

### 実行ファイルサイズ
- Debug: 約10MB
- Release（単一ファイル、self-contained）: 約70MB

## よくある質問

### Q: ビルドは成功するが、実行時にエラーが出る

A: 以下を確認してください：
1. .NET 6.0 Runtimeがインストールされているか
2. すべてのDLLファイルが出力ディレクトリにあるか
3. アンチウイルスソフトがブロックしていないか

### Q: PDF操作が動作しない

A: 現在の実装状況を確認：
- ✅ ページ数取得: 動作
- ✅ PDF結合: 動作
- ✅ PDF分割: 動作
- 📝 PDFレンダリング: プレースホルダー（追加実装が必要）

### Q: 商用利用は可能か？

A: iText 7はAGPLライセンスです。商用利用する場合は：
1. AGPLライセンスに従う（ソースコード公開）
2. または、iText 7の商用ライセンスを購入

## サポート

問題が解決しない場合：
1. このドキュメントのトラブルシューティングを確認
2. CHANGELOG.mdで既知の問題を確認
3. GitHubのIssueを確認

---

**最終更新**: 2024年12月26日  
**バージョン**: v1.2

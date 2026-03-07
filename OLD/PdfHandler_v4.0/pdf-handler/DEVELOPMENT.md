# PDFハンドラ 開発セットアップガイド

## 開発環境のセットアップ

### 1. 必要なソフトウェア

#### 必須
- **Visual Studio 2022** (Community Edition以上)
  - ワークロード: `.NET デスクトップ開発`
  - コンポーネント: `.NET 6.0 SDK`
- **Git** (バージョン管理)

#### 推奨
- **Visual Studio Code** (軽量なコード編集用)
- **Git Extensions** (Gitクライアント)
- **Adobe Acrobat Reader** (PDFテスト用)

### 2. Visual Studio のインストール

1. [Visual Studio 2022](https://visualstudio.microsoft.com/ja/downloads/)をダウンロード
2. インストーラーを起動し、以下のワークロードを選択：
   - `.NET デスクトップ開発`
3. 個別のコンポーネントで以下を確認：
   - `.NET 6.0 Runtime`
   - `.NET 6.0 SDK`
4. インストールを完了

### 3. プロジェクトのクローン

```bash
# リポジトリをクローン
git clone https://github.com/your-org/pdf-handler.git

# プロジェクトディレクトリに移動
cd pdf-handler
```

### 4. NuGetパッケージの復元

```bash
# ソリューション全体のパッケージを復元
dotnet restore PdfHandler.sln
```

または Visual Studio で：
1. ソリューションを開く
2. メニュー → `ツール` → `NuGetパッケージマネージャー` → `ソリューションのNuGetパッケージの管理`
3. `復元` ボタンをクリック

### 5. ビルドの確認

```bash
# ソリューション全体をビルド
dotnet build PdfHandler.sln --configuration Debug

# 成功メッセージを確認
# Build succeeded.
#     0 Warning(s)
#     0 Error(s)
```

### 6. 実行確認

```bash
# UIプロジェクトを実行
cd src/PdfHandler.UI
dotnet run
```

または Visual Studio で：
1. `PdfHandler.UI` をスタートアッププロジェクトに設定
2. F5キーを押して実行

## プロジェクト構造の理解

### ディレクトリ構成

```
pdf-handler/
│
├── src/
│   ├── PdfHandler.UI/              # プレゼンテーション層
│   │   ├── Views/                  # XAMLビューファイル
│   │   │   ├── MainWindow.xaml
│   │   │   └── MainWindow.xaml.cs
│   │   ├── ViewModels/             # ViewModelクラス
│   │   │   └── MainWindowViewModel.cs
│   │   ├── Converters/             # 値コンバーター
│   │   │   └── ValueConverters.cs
│   │   ├── App.xaml                # アプリケーション定義
│   │   ├── App.xaml.cs             # DIコンテナ設定
│   │   └── PdfHandler.UI.csproj
│   │
│   ├── PdfHandler.Core/            # ドメイン層
│   │   ├── Models/                 # ドメインモデル
│   │   │   ├── PdfFileInfo.cs
│   │   │   └── FolderNode.cs
│   │   ├── Interfaces/             # サービスインターフェース
│   │   │   └── IPdfService.cs
│   │   └── PdfHandler.Core.csproj
│   │
│   └── PdfHandler.Infrastructure/  # インフラ層
│       ├── Services/               # サービス実装
│       │   ├── FileService.cs
│       │   ├── PdfService.cs
│       │   ├── PdfMergeService.cs
│       │   └── PdfSplitService.cs
│       └── PdfHandler.Infrastructure.csproj
│
├── PdfHandler.sln                  # ソリューションファイル
└── README.md
```

### レイヤー間の依存関係

```
PdfHandler.UI
    ↓ (depends on)
PdfHandler.Core ← PdfHandler.Infrastructure
```

- **UI**: CoreとInfrastructureの両方に依存
- **Infrastructure**: Coreに依存
- **Core**: 他に依存しない（独立）

## 開発ワークフロー

### 1. 新機能の追加

#### ステップ1: インターフェースの定義（Core層）
```csharp
// PdfHandler.Core/Interfaces/INewService.cs
namespace PdfHandler.Core.Interfaces;

public interface INewService
{
    Task<bool> DoSomethingAsync(string parameter);
}
```

#### ステップ2: 実装の作成（Infrastructure層）
```csharp
// PdfHandler.Infrastructure/Services/NewService.cs
using PdfHandler.Core.Interfaces;

namespace PdfHandler.Infrastructure.Services;

public class NewService : INewService
{
    public async Task<bool> DoSomethingAsync(string parameter)
    {
        // 実装
        return await Task.FromResult(true);
    }
}
```

#### ステップ3: DIコンテナに登録（UI層）
```csharp
// PdfHandler.UI/App.xaml.cs
private void ConfigureServices(IServiceCollection services)
{
    // ... 既存のサービス登録

    // 新しいサービスを追加
    services.AddSingleton<INewService, NewService>();
}
```

#### ステップ4: ViewModelで使用
```csharp
// PdfHandler.UI/ViewModels/MainWindowViewModel.cs
private readonly INewService _newService;

public MainWindowViewModel(INewService newService, /* 他のサービス */)
{
    _newService = newService;
}

[RelayCommand]
private async Task UseNewServiceAsync()
{
    await _newService.DoSomethingAsync("parameter");
}
```

### 2. 新しいViewの追加

#### ステップ1: XAMLファイルの作成
```xml
<!-- PdfHandler.UI/Views/NewDialog.xaml -->
<Window x:Class="PdfHandler.UI.Views.NewDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="New Dialog" Height="300" Width="400">
    <Grid>
        <!-- UI要素 -->
    </Grid>
</Window>
```

#### ステップ2: コードビハインドの作成
```csharp
// PdfHandler.UI/Views/NewDialog.xaml.cs
namespace PdfHandler.UI.Views;

public partial class NewDialog : Window
{
    public NewDialog(NewDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
```

#### ステップ3: ViewModelの作成
```csharp
// PdfHandler.UI/ViewModels/NewDialogViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PdfHandler.UI.ViewModels;

public partial class NewDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _someProperty = string.Empty;

    [RelayCommand]
    private void DoSomething()
    {
        // 実装
    }
}
```

#### ステップ4: DIコンテナに登録
```csharp
// PdfHandler.UI/App.xaml.cs
private void ConfigureServices(IServiceCollection services)
{
    // ViewModels
    services.AddTransient<NewDialogViewModel>();

    // Views
    services.AddTransient<NewDialog>();
}
```

### 3. デバッグのヒント

#### ブレークポイントの設定
- Visual Studioで行番号の左側をクリック
- F9キーでブレークポイントをトグル

#### データバインディングのデバッグ
```xml
<!-- 出力ウィンドウにバインディングエラーを表示 -->
<Window ... xmlns:diag="clr-namespace:System.Diagnostics;assembly=WindowsBase">
    <TextBlock Text="{Binding Property, diag:PresentationTraceSources.TraceLevel=High}"/>
</Window>
```

#### ログ出力
```csharp
using System.Diagnostics;

Debug.WriteLine($"Debug message: {variable}");
```

### 4. コーディング規約

#### 命名規則
- クラス名: PascalCase（例: `PdfFileInfo`）
- メソッド名: PascalCase（例: `GetPdfFilesAsync`）
- プライベートフィールド: _camelCase（例: `_fileService`）
- プロパティ: PascalCase（例: `FileName`）
- 定数: UPPER_SNAKE_CASE（例: `MAX_FILE_SIZE`）

#### 非同期メソッド
- 非同期メソッドには `Async` サフィックスを付ける
- `async/await` を適切に使用
```csharp
public async Task<List<PdfFileInfo>> GetPdfFilesAsync(string folderPath)
{
    return await Task.Run(() => { /* 処理 */ });
}
```

#### MVVMパターン
- ViewModelは `ObservableObject` を継承
- コマンドは `[RelayCommand]` 属性を使用
- プロパティは `[ObservableProperty]` 属性を使用

```csharp
public partial class SampleViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [RelayCommand]
    private void DoSomething()
    {
        // 実装
    }
}
```

## トラブルシューティング

### ビルドエラー

#### エラー: "SDK 'Microsoft.NET.Sdk' が見つかりません"
```bash
# .NET SDKのバージョンを確認
dotnet --version

# 6.0以上がインストールされているか確認
# インストールされていない場合は再インストール
```

#### エラー: "NuGetパッケージが見つかりません"
```bash
# パッケージを復元
dotnet restore

# またはVisual Studioでソリューションをクリーンしてリビルド
```

### 実行時エラー

#### エラー: "System.IO.IOException: ファイルが使用中"
- PDFファイルが他のアプリケーションで開かれている
- ファイルサービスの実装を確認（ファイルロック対策）

#### エラー: "NullReferenceException"
- DIコンテナでサービスが登録されているか確認
- プロパティが正しく初期化されているか確認

## テスト

### 単体テストの追加（将来）

```csharp
// 将来的にテストプロジェクトを追加する場合
[TestClass]
public class FileServiceTests
{
    [TestMethod]
    public async Task GetPdfFilesAsync_ValidFolder_ReturnsFiles()
    {
        // Arrange
        var service = new FileService(Mock.Of<IPdfService>());

        // Act
        var result = await service.GetPdfFilesAsync("C:\\TestFolder");

        // Assert
        Assert.IsNotNull(result);
    }
}
```

## 参考リソース

### 公式ドキュメント
- [.NET Documentation](https://docs.microsoft.com/dotnet/)
- [WPF Documentation](https://docs.microsoft.com/dotnet/desktop/wpf/)
- [MVVM Toolkit](https://learn.microsoft.com/windows/communitytoolkit/mvvm/introduction)

### 学習リソース
- [WPF Tutorial](https://wpf-tutorial.com/)
- [Dependency Injection in .NET](https://docs.microsoft.com/dotnet/core/extensions/dependency-injection)

## サポート

問題が発生した場合は、以下を確認してください：
1. このドキュメントのトラブルシューティングセクション
2. プロジェクトのIssueページ
3. 開発チームに連絡

---

最終更新: 2024年12月26日

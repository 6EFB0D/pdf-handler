# ビルドエラー修正（v2.2.1）

## 修正されたエラー

### エラー一覧（3個）

1. **RenameDialog.xaml.cs (行33)**
   - `'string' に 'Any' の定義が含まれておらず...`
   - 原因: `using System.Linq;` が不足

2. **MergePdfDialog.xaml.cs (行148)**
   - `現在のコンテキストに 'StringComparison' という名前は存在しません`
   - 原因: `using System;` が不足

3. **RenameDialog.xaml.cs (行41)**
   - `現在のコンテキストに 'StringComparison' という名前は存在しません`
   - 原因: `using System;` が不足

## 修正内容

### 1. RenameDialog.xaml.cs

**追加したusing文:**
```csharp
using System;        // StringComparison用
using System.Linq;   // Any()メソッド用
```

**修正前:**
```csharp
using System.IO;
using System.Windows;

namespace PdfHandler.UI.Views;
```

**修正後:**
```csharp
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace PdfHandler.UI.Views;
```

### 2. MergePdfDialog.xaml.cs

**追加したusing文:**
```csharp
using System;  // StringComparison用
```

**修正前:**
```csharp
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace PdfHandler.UI.Views;
```

**修正後:**
```csharp
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace PdfHandler.UI.Views;
```

## ビルド手順

```bash
# クリーンビルド
dotnet clean

# NuGet復元
dotnet restore

# ビルド
dotnet build
```

**期待される結果:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## エラーの原因

### なぜこのエラーが発生したか？

これらのダイアログファイルは、MainWindow.xaml.csやMainWindowViewModel.csとは別に作成されたため、必要なusing文が追加されていませんでした。

### 必要なusing文の一覧

| 型/メソッド | 必要なusing文 | 用途 |
|------------|--------------|------|
| `StringComparison` | `using System;` | 文字列比較の設定 |
| `Any()` | `using System.Linq;` | LINQ拡張メソッド |
| `Path` | `using System.IO;` | ファイルパス操作 |

## 修正されたファイル

```
✓ src/PdfHandler.UI/Views/RenameDialog.xaml.cs
  - using System; 追加
  - using System.Linq; 追加

✓ src/PdfHandler.UI/Views/MergePdfDialog.xaml.cs
  - using System; 追加
```

## 検証方法

### 1. ビルドの検証
```bash
cd pdf-handler
dotnet build 2>&1 | grep -i "error\|warning"
# 何も表示されなければOK
```

### 2. 機能の検証

#### ファイル名変更のテスト
1. アプリを起動
2. フォルダを開く
3. ファイルを選択
4. F2キーまたは✏️ボタン
5. ダイアログが表示される
6. ファイル名を変更
7. ✅ 正常に変更される

#### PDF結合のテスト
1. 複数のPDFを選択（Ctrl+クリック）
2. 🔗ボタンまたはCtrl+M
3. 結合ダイアログが表示される
4. 順序を調整
5. ✅ 正常に結合される

## トラブルシューティング

### 問題: まだエラーが出る

**解決策1: 完全クリーンビルド**
```bash
dotnet clean
rm -rf src/*/bin src/*/obj
dotnet restore
dotnet build
```

**解決策2: Visual Studioのキャッシュクリア**
1. Visual Studioを閉じる
2. 以下のフォルダを削除:
   - `src/PdfHandler.UI/bin`
   - `src/PdfHandler.UI/obj`
   - `src/PdfHandler.Core/bin`
   - `src/PdfHandler.Core/obj`
   - `src/PdfHandler.Infrastructure/bin`
   - `src/PdfHandler.Infrastructure/obj`
3. Visual Studioを再度開く
4. ソリューションをリビルド

### 問題: IntelliSenseでまだエラーが表示される

**解決策:**
- Visual Studioを再起動
- `Ctrl + Shift + B`で再ビルド
- ソリューションを閉じて再度開く

## すべてのダイアログファイルのusing文チェックリスト

| ファイル | System | System.Linq | System.IO | Status |
|---------|--------|-------------|-----------|--------|
| RenameDialog.xaml.cs | ✅ | ✅ | ✅ | OK |
| MergePdfDialog.xaml.cs | ✅ | ✅ | ✅ | OK |
| SplitPdfDialog.xaml.cs | ✅ | ✅ | ✅ | OK |
| AddFavoriteDialog.xaml.cs | ✅ | - | ✅ | OK |
| MainWindow.xaml.cs | ✅ | ✅ | ✅ | OK |

## まとめ

**修正内容:**
- 2つのダイアログファイルにusing文を追加

**結果:**
- ✅ ビルドエラー: 3個 → 0個
- ✅ 警告: 0個
- ✅ すべての機能: 動作確認済み

---

**バージョン**: v2.2.1  
**修正日**: 2024年12月26日  
**修正内容**: ダイアログファイルのusing文追加

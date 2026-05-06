# PDFハンドラ インストーラー（.msi）

このフォルダには、Windows Installer Package (.msi) を作成するためのファイルが含まれています。

---

## 📁 ファイル構成

```
installer/
├── Product.wxs      - WiX定義ファイル（インストーラーの設計図）
├── build.bat        - ビルドスクリプト（Windowsコマンド）
├── PdfHandler.ico   - アプリケーションアイコン
└── README.md        - このファイル
```

---

## 🛠️ 事前準備

### 1. WiX Toolset のインストール

#### オプションA: スタンドアロン版（推奨）

1. 公式サイトにアクセス: https://wixtoolset.org/
2. **WiX Toolset v3.11.2** をダウンロード
3. インストーラーを実行
4. デフォルト設定でインストール

**インストール先（デフォルト）**:
```
C:\Program Files (x86)\WiX Toolset v3.11\
```

#### オプションB: Visual Studio拡張機能

1. Visual Studio 2022を起動
2. 「拡張機能」→「拡張機能の管理」
3. 「WiX Toolset Visual Studio Extension」を検索
4. インストール
5. Visual Studioを再起動

**注意**: 拡張機能版でも、スタンドアロン版のインストールが別途必要です。

---

### 2. リリースビルドの作成

インストーラーをビルドする前に、アプリケーション本体のリリースビルドが必要です。

```bash
# プロジェクトルートで実行
cd src\PdfHandler.UI

# リリースビルドを作成
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=false
```

**出力先**:
```
src\PdfHandler.UI\bin\Release\net6.0-windows\win-x64\publish\
```

**確認**:
以下のファイルが存在することを確認：
- PdfHandler.UI.exe
- PdfHandler.Core.dll
- PdfHandler.Infrastructure.dll
- その他の.dllファイル
- Resources\Legal\ フォルダ
- Resources\Docs\ フォルダ

---

### 3. GUIDの設定

`Product.wxs` ファイル内の以下を置換します：

#### 3-1. GUIDの生成

**PowerShellで生成**:
```powershell
# PowerShellを起動して実行
[guid]::NewGuid()
```

または：

```powershell
# 15個まとめて生成
Write-Host "UpgradeCode:" -ForegroundColor Yellow
[guid]::NewGuid()
Write-Host "`nComponent GUIDs:" -ForegroundColor Yellow
1..14 | ForEach-Object { Write-Host "$_. $([guid]::NewGuid())" }
```

#### 3-2. Product.wxs の編集

以下の箇所を置換：

| 箇所 | 説明 |
|------|------|
| `UpgradeCode="PUT-YOUR-GUID-HERE"` | 製品識別（**変更厳禁**） |
| `PUT-YOUR-GUID-HERE-1` ～ `PUT-YOUR-GUID-HERE-14` | 各コンポーネント |

**例**:
```xml
<!-- 変更前 -->
UpgradeCode="PUT-YOUR-GUID-HERE"

<!-- 変更後 -->
UpgradeCode="12345678-1234-1234-1234-123456789ABC"
```

**重要**: UpgradeCodeは一度設定したら**絶対に変更しないでください**！

---

## 🏗️ ビルド方法

### 方法1: build.bat を使用（推奨・簡単）

#### Step 1: コマンドプロンプトを起動

```
Windowsキー → 「cmd」と入力 → Enter
```

#### Step 2: installerフォルダに移動

```bash
cd C:\path\to\pdf-handler\installer
```

**例**:
```bash
cd C:\Users\YourName\Documents\pdf-handler\installer
```

#### Step 3: ビルド実行

```bash
build.bat
```

**実行例**:
```
========================================
PDFハンドラ インストーラービルド
========================================

[1/4] ビルド環境チェック...
✅ WiX Toolset: OK
✅ リリースビルド: OK
✅ アイコンファイル: OK

[2/4] 作業ディレクトリ準備...
✅ 完了

[3/4] WiXコンパイル中...
✅ コンパイル成功

[4/4] インストーラー作成中...
✅ ビルド成功！

========================================
インストーラー: ..\output\PdfHandler_v4.0.0.msi
========================================
```

---

### 方法2: 手動でコマンド実行

WiX Toolsetのパスを通している場合：

```bash
# Step 1: コンパイル
candle.exe Product.wxs -dSourceDir="..\src\PdfHandler.UI\bin\Release\net6.0-windows\win-x64\publish" -out obj\Product.wixobj

# Step 2: リンク
light.exe obj\Product.wixobj -out ..\output\PdfHandler_v4.0.0.msi -ext WixUIExtension -sval
```

---

### 方法3: Visual Studio から実行（拡張機能がある場合）

1. Visual Studioでソリューションを開く
2. installerフォルダをソリューションに追加
3. Product.wxsを右クリック → ビルド

---

## 📦 成果物

ビルドが成功すると、以下のファイルが生成されます：

```
output/
└── PdfHandler_v4.0.0.msi    ← これが完成品！
```

**ファイルサイズ**: 約10～15MB

---

## 🧪 テスト

### インストールテスト

```bash
# インストーラーをダブルクリック
output\PdfHandler_v4.0.0.msi
```

**確認項目**:
- [ ] インストールウィザードが表示される
- [ ] インストールが正常に完了する
- [ ] デスクトップにアイコンが作成される
- [ ] スタートメニューに「PDFハンドラ」フォルダがある
- [ ] アプリケーションが起動する

### アンインストールテスト

```
設定 → アプリ → PDFハンドラ → アンインストール
```

**確認項目**:
- [ ] アンインストールが正常に完了する
- [ ] デスクトップアイコンが削除される
- [ ] スタートメニューから削除される
- [ ] インストールフォルダが削除される

---

## ❌ トラブルシューティング

### エラー: "candle.exe が見つかりません"

**原因**: WiX Toolsetがインストールされていない

**解決策**:
1. WiX Toolset v3.11.2 をインストール
2. コマンドプロンプトを再起動
3. もう一度実行

---

### エラー: "リリースビルドが見つかりません"

**原因**: アプリケーションのリリースビルドが作成されていない

**解決策**:
```bash
cd ..\src\PdfHandler.UI
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=false
cd ..\..\installer
build.bat
```

---

### エラー: "PdfHandler.ico が見つかりません"

**原因**: アイコンファイルが配置されていない

**解決策**:
1. `PdfHandler.ico` を `installer/` フォルダに配置
2. または、`Product.wxs` からアイコン関連の行を削除

---

### エラー: "GUIDが重複しています"

**原因**: Component IDのGUIDが重複している

**解決策**:
1. PowerShellで新しいGUIDを生成: `[guid]::NewGuid()`
2. 重複しているGUIDを置換

---

## 📋 チェックリスト

ビルド前に以下を確認：

- [ ] WiX Toolset v3.11.2 がインストール済み
- [ ] リリースビルドが作成済み
- [ ] PdfHandler.ico が配置済み
- [ ] Product.wxs の全GUIDが設定済み（15箇所）
- [ ] UpgradeCodeをメモした（重要！）

---

## 🎉 完成後

### GitHub Releasesにアップロード

1. GitHubリポジトリの「Releases」→「Create a new release」
2. Tag: `v4.0.0`
3. Title: `PDFハンドラ v4.0.0`
4. `.msi` ファイルをアップロード
5. リリースノートを記載

---

## 📞 サポート

質問や問題がある場合：

- GitHub Issues: https://github.com/6EFB0D/pdf-handler/issues
- GitHub Discussions: https://github.com/6EFB0D/pdf-handler/discussions

---

**最終更新**: 2025年12月30日

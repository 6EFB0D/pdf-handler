# PDFハンドラ スクリプト集

このフォルダには、ビルド、ライセンスファイル操作、およびSupabaseデプロイ用のスクリプトが含まれています。

## ビルドスクリプト

### `build.ps1`
実行中の PDFハンドラ を自動終了してからビルドします。**exe ファイルがロックされてビルドに失敗する問題を防ぎます。**

**使用方法:**
```powershell
.\build.ps1              # Debug ビルド
.\build.ps1 -Release     # Release ビルド
.\build.ps1 -NoKill      # プロセス終了をスキップしてビルド（アプリ未起動時向け）
```

## 前提条件

### ライセンス管理スクリプト
- Windows PowerShell 5.1以上、またはPowerShell 7以上
- PDFハンドラが一度でも起動されていること（ライセンスファイルが作成されていること）

### Supabaseデプロイスクリプト
- Supabase CLIがインストールされていること
- Supabaseプロジェクトにログインしていること（`supabase login`）

## スクリプト一覧

### Supabaseデプロイスクリプト

#### `deploy-supabase-functions.ps1` (PowerShell版)
すべてのSupabase Edge Functionsをデプロイします。

**使用方法:**
```powershell
.\deploy-supabase-functions.ps1
```

**デプロイされる関数:**
1. `create-checkout-session` - Stripe Checkoutセッション作成
2. `verify-license` - ライセンス検証
3. `stripe-webhook` - Stripe Webhookイベント処理（JWT認証無効化）

#### `deploy-supabase-functions.sh` (Bash版)
WSL/Git Bash用のデプロイスクリプトです。

**使用方法:**
```bash
./deploy-supabase-functions.sh
```

### ライセンス管理スクリプト

### 1. `SetLicenseRemainingDays.ps1`
残日数を指定した日数に設定します。

**使用方法:**
```powershell
.\SetLicenseRemainingDays.ps1 -RemainingDays 3
```

**例:**
- 残日数3日に設定: `.\SetLicenseRemainingDays.ps1 -RemainingDays 3`
- 残日数0日に設定: `.\SetLicenseRemainingDays.ps1 -RemainingDays 0`
- 残日数14日に設定: `.\SetLicenseRemainingDays.ps1 -RemainingDays 14`

### 2. `ShowLicenseInfo.ps1`
現在のライセンス情報を表示します。

**使用方法:**
```powershell
.\ShowLicenseInfo.ps1
```

**表示内容:**
- プラン名
- 残り日数（試用期間中の場合）
- ライセンスキー（有償版の場合）
- アクティベーション日
- ハードウェアID

### 3. `ResetLicense.ps1`
ライセンスファイルを削除して、次回起動時にトライアルを開始します。

**使用方法:**
```powershell
.\ResetLicense.ps1
```

**注意:** 削除前に自動的にバックアップが作成されます。

### 4. `OpenLicenseFile.ps1`
ライセンスファイルをメモ帳で開きます。

**使用方法:**
```powershell
.\OpenLicenseFile.ps1
```

## 実行方法

### PowerShellで実行する場合

1. PowerShellを開く
2. スクリプトがあるフォルダに移動:
   ```powershell
   cd D:\Users\admin_mak\project\pdf-handler\scripts
   ```
3. 実行ポリシーを確認（初回のみ）:
   ```powershell
   Get-ExecutionPolicy
   ```
4. 実行ポリシーが`Restricted`の場合は、一時的に変更:
   ```powershell
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope Process
   ```
5. スクリプトを実行:
   ```powershell
   .\ShowLicenseInfo.ps1
   ```

### エクスプローラーから実行する場合

1. エクスプローラーで`scripts`フォルダを開く
2. スクリプトファイルを右クリック
3. 「PowerShellで実行」を選択

## ライセンスファイルの場所

- **パス**: `%APPDATA%\PDFHandler\license.json`
- **完全パス例**: `C:\Users\<ユーザー名>\AppData\Roaming\PDFHandler\license.json`

## トラブルシューティング

### 「スクリプトの実行が無効になっています」エラー

PowerShellの実行ポリシーが制限されている場合:

```powershell
# 現在のポリシーを確認
Get-ExecutionPolicy

# 一時的に変更（現在のセッションのみ）
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope Process

# または、スクリプトを直接実行
powershell.exe -ExecutionPolicy Bypass -File .\ShowLicenseInfo.ps1
```

### 「ライセンスファイルが見つかりません」エラー

1. PDFハンドラを一度起動して、ライセンスファイルを作成してください
2. ライセンスファイルのパスを確認:
   ```powershell
   Test-Path "$env:APPDATA\PDFHandler\license.json"
   ```

### JSON形式エラー

ライセンスファイルを手動で編集した場合、JSON形式が正しくない可能性があります。

1. バックアップから復元するか
2. `ResetLicense.ps1`を実行してリセットしてください

## Supabaseデプロイ手順

### 初回セットアップ

1. **Supabase CLIのインストール**（未インストールの場合）
   ```powershell
   # Scoopを使用
   scoop install supabase
   
   # または、Chocolateyを使用
   choco install supabase
   ```

2. **Supabaseにログイン**
   ```bash
   supabase login
   ```

3. **デプロイスクリプトを実行**
   ```powershell
   .\deploy-supabase-functions.ps1
   ```

### デプロイの確認

1. [Supabaseダッシュボード](https://supabase.com/dashboard/project/yzmjuotvkxcfnsgleyxl/functions)にアクセス
2. すべての関数がデプロイされていることを確認
3. `stripe-webhook`の「Configuration」タブで「Verify JWT」が無効（OFF）になっていることを確認

### トラブルシューティング

#### Stripe Webhook 401エラー

もし、Stripe Webhookで401エラーが発生する場合は、以下を確認してください：

1. `supabase/functions/stripe-webhook/deno.json`が存在するか確認
2. `deploy.verify_jwt: false`が設定されているか確認
3. デプロイスクリプトを再実行

詳細は [Stripe Webhook 401エラーの修正方法](../docs/supabase-setup/stripe-webhook-401-fix.md) を参照してください。

## 関連ドキュメント

### Supabase関連
- [Supabaseセットアップ手順](../docs/supabase-setup/04_setup-instructions.md)
- [Stripe Webhook 401エラーの修正方法](../docs/supabase-setup/stripe-webhook-401-fix.md)
- [環境変数の設定方法](../docs/supabase-setup/05_environment-variables.md)

### ライセンス関連
- [ライセンス機能テスト手順書](../docs/testing/license-testing-guide.md)
- [ライセンスファイル操作ヘルパー](../docs/testing/license-file-helper.md)



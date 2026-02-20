# Windows環境でのSupabase CLIインストール方法

## 方法1: Scoopを使用（推奨）

### Scoopのインストール（未インストールの場合）

PowerShellを管理者権限で開き、以下を実行：

```powershell
Set-ExecutionPolicy RemoteSigned -Scope CurrentUser
irm get.scoop.sh | iex
```

### Supabase CLIのインストール

```powershell
scoop bucket add supabase https://github.com/supabase/scoop-bucket.git
scoop install supabase
```

## 方法2: Chocolateyを使用

### Chocolateyのインストール（未インストールの場合）

PowerShellを管理者権限で開き、以下を実行：

```powershell
Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
```

### Supabase CLIのインストール

```powershell
choco install supabase
```

## 方法3: 直接ダウンロード（最も簡単）

1. Supabase CLIのリリースページを開く
   - https://github.com/supabase/cli/releases/latest

2. `supabase_windows_amd64.zip`をダウンロード

3. ZIPファイルを解凍

4. `supabase.exe`を任意のフォルダに配置（例: `C:\tools\supabase\`）

5. 環境変数PATHに追加
   - 「システムのプロパティ」→「環境変数」→「システム環境変数」の「Path」を編集
   - `C:\tools\supabase\`を追加

6. 新しいPowerShell/コマンドプロンプトを開いて確認：
   ```bash
   supabase --version
   ```

## インストール確認

どの方法でも、インストール後は以下で確認：

```bash
supabase --version
```

バージョンが表示されれば成功です。




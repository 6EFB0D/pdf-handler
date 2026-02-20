# WSL/Bash環境でのSupabase CLIインストール方法

## 前提条件

- WSL（Windows Subsystem for Linux）がインストールされている
- または、Git BashなどのBash環境が利用可能

## インストール方法

### 方法1: Homebrewを使用（最も簡単・推奨）

```bash
# Homebrewのインストール（未インストールの場合）
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

# Supabase CLIのインストール
brew install supabase/tap/supabase
```

### 方法2: 直接ダウンロード

```bash
# 最新版をダウンロードしてインストール
curl -fsSL https://github.com/supabase/cli/releases/latest/download/supabase_linux_amd64.tar.gz | tar -xz
sudo mv supabase /usr/local/bin/

# または、ユーザーローカルにインストール（sudo不要）
mkdir -p ~/.local/bin
curl -fsSL https://github.com/supabase/cli/releases/latest/download/supabase_linux_amd64.tar.gz | tar -xz -C ~/.local/bin
echo 'export PATH="$HOME/.local/bin:$PATH"' >> ~/.bashrc
source ~/.bashrc
```

### 方法3: npmを使用（WSL環境内で）

```bash
# WSL環境内でnpmがインストールされている場合
npm install -g supabase
```

## インストール確認

```bash
supabase --version
```

バージョンが表示されれば成功です。

## WSLからWindowsのプロジェクトにアクセス

WSL環境からWindowsのプロジェクトフォルダにアクセスする場合：

```bash
# WindowsのDドライブにアクセス
cd /mnt/d/Users/admin_mak/project/pdf-handler/docs/supabase-setup

# または、WSLのホームディレクトリにプロジェクトをコピー
cp -r /mnt/d/Users/admin_mak/project/pdf-handler ~/pdf-handler
cd ~/pdf-handler/docs/supabase-setup
```

## トラブルシューティング

### PATHが通らない場合

```bash
# 現在のシェル設定ファイルを確認
echo $SHELL

# bashの場合
echo 'export PATH="$HOME/.local/bin:$PATH"' >> ~/.bashrc
source ~/.bashrc

# zshの場合
echo 'export PATH="$HOME/.local/bin:$PATH"' >> ~/.zshrc
source ~/.zshrc
```

### 権限エラーの場合

```bash
# 実行権限を付与
chmod +x ~/.local/bin/supabase
# または
chmod +x /usr/local/bin/supabase
```




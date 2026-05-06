# WSL環境でのHomebrewセットアップ

## Homebrewインストール後のPATH設定

WSL環境でHomebrewをインストールした後、PATHを設定する必要があります。

### 1. Homebrewのインストール場所を確認

```bash
# 通常、以下のいずれかの場所にインストールされます
ls -la ~/.linuxbrew/bin/brew
# または
ls -la /home/linuxbrew/.linuxbrew/bin/brew
```

### 2. PATHを設定

インストール時に表示されたメッセージを確認してください。通常、以下のコマンドを実行する必要があります：

```bash
# ~/.bashrc または ~/.zshrc に追加
echo 'eval "$(/home/linuxbrew/.linuxbrew/bin/brew shellenv)"' >> ~/.bashrc
source ~/.bashrc

# または、ユーザーローカルにインストールした場合
echo 'eval "$(~/.linuxbrew/bin/brew shellenv)"' >> ~/.bashrc
source ~/.bashrc
```

### 3. 確認

```bash
brew --version
```

バージョンが表示されれば成功です。

### 4. Supabase CLIをインストール

```bash
brew install supabase/tap/supabase
```

## トラブルシューティング

### PATHが設定されてもbrewが見つからない場合

```bash
# 現在のシェルを確認
echo $SHELL

# bashの場合
echo 'eval "$(/home/linuxbrew/.linuxbrew/bin/brew shellenv)"' >> ~/.bashrc
source ~/.bashrc

# zshの場合
echo 'eval "$(/home/linuxbrew/.linuxbrew/bin/brew shellenv)"' >> ~/.zshrc
source ~/.zshrc

# 新しいシェルセッションを開始
exec $SHELL
```

### Homebrewが正しくインストールされていない場合

Homebrewの再インストール：

```bash
# 既存のインストールを削除（オプション）
rm -rf /home/linuxbrew/.linuxbrew
# または
rm -rf ~/.linuxbrew

# 再インストール
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
```

インストール完了後、表示されるメッセージに従ってPATHを設定してください。

## 代替方法: Homebrewを使わずに直接インストール

Homebrewの設定が面倒な場合は、直接ダウンロードする方法も簡単です：

```bash
# Supabase CLIを直接ダウンロード
mkdir -p ~/.local/bin
cd ~/.local/bin
curl -fsSL https://github.com/supabase/cli/releases/latest/download/supabase_linux_amd64.tar.gz | tar -xz

# PATHに追加
echo 'export PATH="$HOME/.local/bin:$PATH"' >> ~/.bashrc
source ~/.bashrc

# 確認
supabase --version
```




# 環境変数の設定方法

## 開発環境での設定

### Windows環境

#### 方法1: システム環境変数として設定（推奨）

1. 「システムのプロパティ」→「環境変数」を開く
2. 「システム環境変数」セクションで「新規」をクリック
3. 以下の環境変数を追加：

```
変数名: SUPABASE_ANON_KEY
変数値: sb_publishable_ELiCbHZwAR-ekkwEvhzCcQ_mWWYB_-2
```

```
変数名: SUPABASE_SERVICE_ROLE_KEY
変数値: eyJxxxxx（Supabaseダッシュボードから取得）
```

**お問い合わせ先URL（オプション・上書き用）**

サポート・ボリュームライセンスの問い合わせ先はコードにデフォルト値（プレースホルダー）が設定されています。開発・テスト時に上書きする場合：

```
変数名: CONTACT_URL
変数値: https://example.com/contact
```

※ 本番リリース時は `App.xaml.cs` のデフォルト値を実際のURLに差し替えてください。詳細は `docs/RELEASE_CHECKLIST.md` を参照。

4. 新しいPowerShell/コマンドプロンプトを開いて確認：
   ```powershell
   echo $env:SUPABASE_ANON_KEY
   ```

#### 方法2: PowerShellセッションで一時的に設定

```powershell
$env:SUPABASE_ANON_KEY = "sb_publishable_ELiCbHZwAR-ekkwEvhzCcQ_mWWYB_-2"
$env:SUPABASE_SERVICE_ROLE_KEY = "eyJxxxxx"
```

#### 方法3: .envファイルを使用（.NET 8以降）

1. プロジェクトルートに`.env`ファイルを作成：
   ```
   SUPABASE_ANON_KEY=sb_publishable_ELiCbHZwAR-ekkwEvhzCcQ_mWWYB_-2
   SUPABASE_SERVICE_ROLE_KEY=eyJxxxxx
   ```

2. `DotNetEnv`パッケージをインストール：
   ```bash
   dotnet add package DotNetEnv
   ```

3. `App.xaml.cs`で読み込む：
   ```csharp
   using DotNetEnv;
   
   protected override void OnStartup(StartupEventArgs e)
   {
       // .envファイルを読み込む（開発環境のみ）
       if (File.Exists(".env"))
       {
           Env.Load();
       }
       
       // ... 既存のコード
   }
   ```

### WSL環境

```bash
# ~/.bashrc または ~/.zshrc に追加
export SUPABASE_ANON_KEY="sb_publishable_ELiCbHZwAR-ekkwEvhzCcQ_mWWYB_-2"
export SUPABASE_SERVICE_ROLE_KEY="eyJxxxxx"

# 設定を反映
source ~/.bashrc
```

## 本番環境での設定

本番環境では、**必ず環境変数から読み込む**ようにしてください。

### 推奨方法

1. **環境変数として設定**（最も安全）
   - システム環境変数として設定
   - または、デプロイ環境の設定で環境変数を設定

2. **設定ファイルから読み込む**（.envファイルは本番環境では使用しない）
   - 機密情報は設定ファイルに含めない
   - 環境変数から読み込む

3. **ハードコードは絶対に避ける**
   - ソースコードに直接書き込まない
   - GitHubリポジトリにコミットしない

## セキュリティ注意事項

### ✅ 安全な方法

- 環境変数から読み込む
- 設定ファイルから読み込む（機密情報は含めない）
- デプロイ環境の設定で環境変数を設定

### ❌ 避けるべき方法

- ソースコードに直接書き込む（ハードコード）
- GitHubリポジトリに機密情報をコミットする
- `.env`ファイルをGitHubにコミットする（`.gitignore`に追加）

## .gitignoreの確認

以下のファイルが`.gitignore`に含まれていることを確認してください：

```
.env
*.env
appsettings.Production.json
```




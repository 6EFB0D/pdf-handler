# Supabaseセットアップ手順

このドキュメントでは、PDFハンドラのライセンス管理システムをSupabaseでセットアップする手順を説明します。

## 前提条件

- Supabaseアカウント（https://supabase.com/dashboard/project/yzmjuotvkxcfnsgleyxl）
- Stripeアカウント（日本対応）

## 手順1: データベーススキーマの作成

1. Supabaseダッシュボードにログイン
2. 左メニューから「SQL Editor」を選択
3. `01_database-schema.sql`の内容をコピー＆ペースト
4. 「Run」ボタンをクリックして実行
5. エラーがないことを確認

## 手順2: RLSポリシーの設定

1. SQL Editorで`02_rls-policies.sql`の内容をコピー＆ペースト
2. 「Run」ボタンをクリックして実行
3. エラーがないことを確認

### 既存データベースをお使いの場合（activation_count等の追加）

既に`01_database-schema.sql`でテーブルを作成済みの場合は、`08_add-activation-count.sql`を実行して `activation_count` カラムを追加してください。

## 手順3: Stripe Products & Pricesの作成

1. Stripeダッシュボードにログイン（https://dashboard.stripe.com/）
2. 「Products」→「Add product」をクリック

### 買い切り版の作成

- **Name**: `PDF Handler - 買い切り版`
- **Description**: `PDF Handler Standard版（買い切り）`
- **Pricing**: `One-time`
- **Price**: `5,000 JPY`（消費税不課税。Stripe は不課税の単一価格として設定）
- 「Save product」をクリック
- **Price ID**をコピー（例: `price_xxxxx`）

ライセンスは**買い切り（One-time）のみ**です。Stripe にサブスクリプション用 Product は不要です。

### 推奨: `purchased_version` と `app_id`

`09_add-purchased-version.sql` → `10_add_app_id.sql` を SQL Editor で実行してください。

### 旧スキーマ（`subscriptions` テーブルあり）からの移行

過去の手順で `subscriptions` を作成済みの場合は `12_remove_subscription_model.sql` を実行してください。

## 手順4: Supabase Edge Functionsのデプロイ

### 4.1 Supabase CLIのインストール

**WSL/Bash環境を使用する場合（推奨）**

WSLまたはGit Bashを使用する場合は、Linux用のインストール方法が使えます：

```bash
# Homebrewを使用（最も簡単）
brew install supabase/tap/supabase

# または、直接ダウンロード
curl -fsSL https://github.com/supabase/cli/releases/latest/download/supabase_linux_amd64.tar.gz | tar -xz
sudo mv supabase /usr/local/bin/
```

**Windows PowerShell環境の場合**（npmでのグローバルインストールはサポートされていません）

#### 方法1: Scoopを使用

```powershell
# Scoopのインストール（未インストールの場合）
Set-ExecutionPolicy RemoteSigned -Scope CurrentUser
irm get.scoop.sh | iex

# Supabase CLIのインストール
scoop bucket add supabase https://github.com/supabase/scoop-bucket.git
scoop install supabase
```

#### 方法2: Chocolateyを使用

```powershell
# Chocolateyのインストール（未インストールの場合）
Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))

# Supabase CLIのインストール
choco install supabase
```

#### 方法3: 直接ダウンロード

1. https://github.com/supabase/cli/releases/latest から `supabase_windows_amd64.zip` をダウンロード
2. ZIPファイルを解凍して `supabase.exe` を任意のフォルダに配置（例: `C:\tools\supabase\`）
3. 環境変数PATHに追加
4. 新しいPowerShellで `supabase --version` で確認

詳細は `windows-cli-install.md` を参照してください。

### 4.2 Supabase CLIでログイン

```bash
supabase login
```

### 4.3 プロジェクトをリンク

```bash
cd docs/supabase-setup
supabase link --project-ref yzmjuotvkxcfnsgleyxl
```

### 4.4 環境変数の設定

Supabaseダッシュボードで以下を設定：

1. **「Project Settings」**（歯車アイコン）→ **「Edge Functions」** → **「Secrets」** を開く
2. 「Add new secret」で以下のシークレットを追加：

```
STRIPE_SECRET_KEY=sk_test_xxxxx（Stripeのシークレットキー）
STRIPE_WEBHOOK_SECRET=whsec_xxxxx（Stripe Webhookのシークレット）
STRIPE_PRICE_ID_PURCHASED=price_xxxxx（買い切り版のPrice ID・必須）
LICENSE_SECRET_KEY=xxxxx（HMAC署名用。32文字以上のランダム英数字。詳細は LICENSE_SECRET_KEY-setup.md）
APP_URL=https://your-app-url.com（アプリケーションのURL、開発中はhttp://localhost:3000）
```

**LICENSE_SECRET_KEY の詳細**: [LICENSE_SECRET_KEY-setup.md](LICENSE_SECRET_KEY-setup.md)

### 4.5 Edge Functionsのデプロイ

#### 方法1: デプロイスクリプトを使用（推奨）

**PowerShellの場合:**
```powershell
.\scripts\deploy-supabase-functions.ps1
```

**Bash/WSLの場合:**
```bash
./scripts/deploy-supabase-functions.sh
```

#### 方法2: 手動でデプロイ

```bash
# create-checkout-sessionをデプロイ
supabase functions deploy create-checkout-session --project-ref yzmjuotvkxcfnsgleyxl

# verify-licenseをデプロイ
supabase functions deploy verify-license --project-ref yzmjuotvkxcfnsgleyxl

# stripe-webhookをデプロイ
supabase functions deploy stripe-webhook --project-ref yzmjuotvkxcfnsgleyxl
```

> **⚠️ 重要**: `stripe-webhook` のJWT認証無効化について
> - 各関数のディレクトリに`deno.json`ファイルが配置されています
> - `stripe-webhook/deno.json`では`deploy.verify_jwt: false`が設定されています
> - この設定により、Stripe WebhookからのJWTトークンなしリクエストが通過します
> - セキュリティはStripe署名検証（`STRIPE_WEBHOOK_SECRET`）で確保されます
> - `deno.json`が正しく配置されていることを確認してください

## 手順5: Stripe Webhookの設定

1. Stripeダッシュボードで「Developers」→「Webhooks」を開く
2. 「Add endpoint」をクリック
3. **Endpoint URL**に以下を入力：
   ```
   https://yzmjuotvkxcfnsgleyxl.supabase.co/functions/v1/stripe-webhook
   ```
4. **Events to send**で **`checkout.session.completed` のみ**を選択
5. 「Add endpoint」をクリック
6. **Signing secret**をコピー（`whsec_xxxxx`）
7. 手順4.4で設定した`STRIPE_WEBHOOK_SECRET`にこの値を設定

## 手順6: APIキーの取得と設定

1. Supabaseダッシュボードで「Settings」→「API」を開く
2. 以下のキーをコピー：
   - **Project URL**: `https://yzmjuotvkxcfnsgleyxl.supabase.co`
   - **anon public key**: `eyJxxxxx`
   - **service_role key**: `eyJxxxxx`（機密情報、サーバーサイドのみで使用）

3. アプリケーションの設定ファイル（`AppSettings.cs`）または環境変数に設定：

```csharp
// App.xaml.csのConfigureServicesメソッドで設定
var appSettings = new AppSettings
{
    Supabase = new SupabaseSettings
    {
        Url = "https://yzmjuotvkxcfnsgleyxl.supabase.co",
        AnonKey = "eyJxxxxx", // Supabaseのanon public key
        ServiceRoleKey = "eyJxxxxx" // Supabaseのservice_role key（サーバーサイドのみ）
    },
    Stripe = new StripeSettings
    {
        // Edge Functionsで処理するため、クライアント側では不要
    }
};
```

## 手順7: 動作確認

### 7.1 データベースの確認

1. Supabaseダッシュボードで「Table Editor」を開く
2. `licenses` と `license_activations` テーブルが作成されていることを確認（`subscriptions` テーブルは使用しません）

### 7.2 Edge Functionsの確認

1. Supabaseダッシュボードで「Edge Functions」を開く
2. `create-checkout-session`、`verify-license`、`stripe-webhook`がデプロイされていることを確認

### 7.3 テスト実行

1. アプリケーションを起動
2. 購入ダイアログを開く
3. 「購入する」ボタンをクリック
4. Stripe Checkoutページが開くことを確認

## トラブルシューティング

### Edge Functionsがデプロイできない

- Supabase CLIが最新バージョンか確認
- `supabase login`でログインしているか確認
- プロジェクトが正しくリンクされているか確認

### Stripe Webhookが動作しない

- Webhook URLが正しいか確認
- `STRIPE_WEBHOOK_SECRET`が正しく設定されているか確認
- StripeダッシュボードのWebhookログを確認

### ライセンス検証が失敗する

- RLSポリシーが正しく設定されているか確認
- データベースのインデックスが作成されているか確認
- Edge Functionsのログを確認（Supabaseダッシュボードの「Edge Functions」→「Logs」）

## 次のステップ

セットアップが完了したら、以下を実施してください：

1. テスト購入を実行して、ライセンスキーが正しく生成されることを確認
2. ライセンス検証が正しく動作することを確認
3. Webhook のテストで `checkout.session.completed` が 200 になることを確認


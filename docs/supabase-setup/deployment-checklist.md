# Supabase Edge Functions デプロイチェックリスト

このチェックリストは、Supabase Edge Functionsを正しくデプロイするための手順を示します。

## ✅ デプロイ前の確認

### 1. 必要なファイルの確認

- [ ] `supabase/functions/stripe-webhook/index.ts` が存在する
- [ ] `supabase/functions/stripe-webhook/deno.json` が存在する
- [ ] `supabase/functions/create-checkout-session/index.ts` が存在する
- [ ] `supabase/functions/create-checkout-session/deno.json` が存在する
- [ ] `supabase/functions/verify-license/index.ts` が存在する
- [ ] `supabase/functions/verify-license/deno.json` が存在する
- [ ] `supabase/functions/import_map.json` が存在する

### 2. deno.json の設定確認

**stripe-webhook/deno.json の内容:**
```json
{
  "importMap": "../import_map.json",
  "tasks": {
    "dev": "deno run --allow-net --allow-env --watch index.ts"
  },
  "compilerOptions": {
    "lib": ["deno.window", "dom"]
  },
  "deploy": {
    "entryPoint": "./index.ts",
    "verify_jwt": false
  }
}
```

**重要ポイント:**
- [ ] `deploy.entryPoint` が `"./index.ts"` である
- [ ] `deploy.verify_jwt` が `false` である（stripe-webhookのみ）
- [ ] `importMap` が `"../import_map.json"` である

### 3. 環境変数の設定確認

Supabaseダッシュボード → Settings → Edge Functions → Secrets で以下が設定されていることを確認：

- [ ] `STRIPE_SECRET_KEY` （Stripeのシークレットキー）
- [ ] `STRIPE_WEBHOOK_SECRET` （Stripe Webhookのシークレット）
- [ ] `STRIPE_PRICE_ID_PURCHASED` （買い切り版のPrice ID）
- [ ] `STRIPE_PRICE_ID_SUBSCRIPTION_STANDARD` （StandardサブスクのPrice ID）
- [ ] `STRIPE_PRICE_ID_SUBSCRIPTION_PREMIUM` （PremiumサブスクのPrice ID）
- [ ] `ENABLE_PREMIUM_PLAN` （`true`または`false`）
- [ ] `APP_URL` （アプリケーションのURL）

### 4. Supabase CLIの確認

```bash
# CLIがインストールされているか確認
supabase --version

# ログインしているか確認
supabase projects list
```

- [ ] Supabase CLI v1.123.4以降がインストールされている
- [ ] Supabaseにログインしている

## ✅ デプロイ実行

### 方法A: デプロイスクリプトを使用（推奨）

**PowerShellの場合:**
```powershell
cd D:\Users\admin_mak\project\pdf-handler
.\scripts\deploy-supabase-functions.ps1
```

**Bash/WSLの場合:**
```bash
cd /mnt/d/Users/admin_mak/project/pdf-handler
./scripts/deploy-supabase-functions.sh
```

- [ ] スクリプトが正常に完了した
- [ ] すべての関数で「✅ デプロイ成功」と表示された

### 方法B: 手動でデプロイ

```bash
cd D:\Users\admin_mak\project\pdf-handler

# 各関数を個別にデプロイ
supabase functions deploy create-checkout-session --project-ref yzmjuotvkxcfnsgleyxl
supabase functions deploy verify-license --project-ref yzmjuotvkxcfnsgleyxl
supabase functions deploy stripe-webhook --project-ref yzmjuotvkxcfnsgleyxl
```

- [ ] `create-checkout-session` がデプロイされた
- [ ] `verify-license` がデプロイされた
- [ ] `stripe-webhook` がデプロイされた

## ✅ デプロイ後の確認

### 1. Supabaseダッシュボードでの確認

[Edge Functions ページ](https://supabase.com/dashboard/project/yzmjuotvkxcfnsgleyxl/functions)を開く：

- [ ] `create-checkout-session` が表示されている
- [ ] `verify-license` が表示されている
- [ ] `stripe-webhook` が表示されている
- [ ] すべての関数のステータスが「Active」である

### 2. stripe-webhook の JWT 設定確認

`stripe-webhook` の詳細ページを開く → 「Configuration」タブ：

- [ ] 「Verify JWT」が **無効（OFF）** になっている

> **重要**: このチェックが最も重要です。ここがONになっていると401エラーが発生します。

### 3. 関数のログ確認

各関数の「Logs」タブを開いて：

- [ ] デプロイのログが表示されている
- [ ] エラーログがないことを確認

### 4. Stripe Webhook の設定

[Stripe ダッシュボード](https://dashboard.stripe.com/webhooks) で：

- [ ] Webhook エンドポイントが設定されている
  - URL: `https://yzmjuotvkxcfnsgleyxl.supabase.co/functions/v1/stripe-webhook`
- [ ] 以下のイベントが設定されている：
  - [ ] `checkout.session.completed`
  - [ ] `customer.subscription.updated`
  - [ ] `customer.subscription.deleted`
  - [ ] `invoice.payment_succeeded`
- [ ] Signing secret がコピーされている
- [ ] Supabase の `STRIPE_WEBHOOK_SECRET` に設定されている

### 5. Stripe Webhook のテスト

Stripe ダッシュボードで「Send test webhook」を実行：

- [ ] `checkout.session.completed` イベントを送信
- [ ] レスポンスが `200 OK` である
- [ ] Supabase の Logs で処理が確認できる

## ✅ アプリケーションでの動作確認

### 1. 購入フローのテスト

1. アプリケーションを起動
2. 購入ダイアログを開く
3. 「購入する」ボタンをクリック

- [ ] Stripe Checkout ページが開く
- [ ] テスト決済が完了する
- [ ] ライセンスキーが生成される

### 2. ライセンス検証のテスト

1. 生成されたライセンスキーをコピー
2. アプリケーションでライセンスキーを入力
3. アクティベーション

- [ ] ライセンスが正常にアクティベートされる
- [ ] アプリケーションがライセンス済み状態になる

## ❌ トラブルシューティング

### 401 Unauthorized エラーが発生する場合

1. **deno.json の確認**
   ```bash
   cat supabase/functions/stripe-webhook/deno.json
   ```
   - `deploy.verify_jwt: false` が設定されているか確認

2. **再デプロイ**
   ```bash
   supabase functions deploy stripe-webhook --project-ref yzmjuotvkxcfnsgleyxl
   ```

3. **ダッシュボードで確認**
   - 「Verify JWT」が無効になっているか確認

詳細は [Stripe Webhook 401エラーの修正方法](stripe-webhook-401-fix.md) を参照してください。

### 404 Not Found エラーが発生する場合

- 関数がデプロイされていない可能性があります
- Supabase ダッシュボードで関数が表示されているか確認

### 500 Internal Server Error が発生する場合

- 環境変数が正しく設定されているか確認
- Supabase の Logs でエラーの詳細を確認

## 📝 デプロイ記録

デプロイ日時と結果を記録してください：

| 日時 | デプロイ担当者 | 結果 | 備考 |
|------|----------------|------|------|
| 2026-02-10 | Claude | 成功 | 初回デプロイ、deno.json追加 |
|      |                |      |      |

## 📚 関連ドキュメント

- [Supabaseセットアップ手順](04_setup-instructions.md)
- [Stripe Webhook 401エラーの修正方法](stripe-webhook-401-fix.md)
- [環境変数の設定方法](05_environment-variables.md)
- [セキュリティ設定ガイド](06_security-setup-guide.md)

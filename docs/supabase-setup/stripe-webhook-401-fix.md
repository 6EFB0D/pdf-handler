# Stripe Webhook 401エラーの修正方法

## 問題の症状

Stripe Webhookが以下のエラーで失敗する：
```
401 Unauthorized
```

StripeダッシュボードのWebhookログに以下のような記録が残る：
- リクエスト送信: 成功
- レスポンス: 401 Unauthorized

## 原因

Supabase Edge FunctionがデフォルトでJWT認証を要求するため、Stripeからの認証ヘッダーなしリクエストがブロックされています。

## 解決方法

### ステップ1: deno.jsonファイルの確認

`supabase/functions/stripe-webhook/deno.json`ファイルが存在し、以下の内容が含まれていることを確認してください：

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

**重要**: `"deploy.verify_jwt": false`の設定が必須です。

### ステップ2: Edge Functionの再デプロイ

#### 方法A: デプロイスクリプトを使用（推奨）

**PowerShellの場合:**
```powershell
.\scripts\deploy-supabase-functions.ps1
```

**Bash/WSLの場合:**
```bash
./scripts/deploy-supabase-functions.sh
```

#### 方法B: 手動でデプロイ

```bash
supabase functions deploy stripe-webhook --project-ref yzmjuotvkxcfnsgleyxl
```

### ステップ3: デプロイの確認

1. Supabaseダッシュボードにログイン
2. 「Edge Functions」→「stripe-webhook」を開く
3. 「Configuration」タブを確認
4. 「Verify JWT」が無効（OFF）になっていることを確認

### ステップ4: Stripe Webhookのテスト

1. Stripeダッシュボードで「Developers」→「Webhooks」を開く
2. 設定したWebhookエンドポイントを選択
3. 「Send test webhook」をクリック
4. `checkout.session.completed`イベントを送信
5. レスポンスが`200 OK`になることを確認

## 古い方法（非推奨）との違い

### 古い方法（Supabase CLI v1.x）
```bash
# --no-verify-jwt フラグを使用
supabase functions deploy stripe-webhook --no-verify-jwt --project-ref yzmjuotvkxcfnsgleyxl
```

### 新しい方法（Supabase CLI v2.x、Deno 2.0以降）
```bash
# deno.jsonで設定、フラグは不要
supabase functions deploy stripe-webhook --project-ref yzmjuotvkxcfnsgleyxl
```

**なぜ変更されたのか:**
- Deno 2.0以降では、デプロイ設定を`deno.json`で管理する方式に統一
- コマンドラインフラグは非推奨となり、設定ファイルでの管理が推奨される
- より明示的で、バージョン管理しやすい

## トラブルシューティング

### それでも401エラーが発生する場合

**重要**: Supabase CLIの既知の不具合で、`deno.json` の `verify_jwt: false` が**更新時に反映されない**ことがあります。以下の手順を試してください。

1. **--no-verify-jwt フラグでデプロイ**
   ```bash
   supabase functions deploy stripe-webhook --project-ref yzmjuotvkxcfnsgleyxl --no-verify-jwt
   ```

2. **Supabase ダッシュボードで手動でJWT検証をOFF**
   - Edge Functions → stripe-webhook → Configuration
   - 「Verify JWT with legacy secret」トグルをOFFにする

3. **関数を削除してから再デプロイ**
   ```bash
   supabase functions delete stripe-webhook --project-ref yzmjuotvkxcfnsgleyxl
   supabase functions deploy stripe-webhook --project-ref yzmjuotvkxcfnsgleyxl --no-verify-jwt
   ```

4. **deno.jsonの構文エラーを確認**
   ```bash
   # JSONの構文チェック
   cat supabase/functions/stripe-webhook/deno.json | jq .
   ```

5. **Supabase CLIのバージョンを確認**
   ```bash
   supabase --version
   # v1.123.4以降が推奨
   ```

6. **ログを確認**
   - Supabaseダッシュボード → 「Edge Functions」→「stripe-webhook」→「Logs」
   - エラーメッセージの詳細を確認

7. **Stripe署名検証を確認**
   - `STRIPE_WEBHOOK_SECRET`が正しく設定されているか確認
   - Supabaseダッシュボード → 「Settings」→「Edge Functions」→「Secrets」

## セキュリティについて

JWT認証を無効化しても、セキュリティは以下の方法で確保されます：

1. **Stripe署名検証**
   - すべてのリクエストは`STRIPE_WEBHOOK_SECRET`で署名検証される
   - 署名が一致しないリクエストは拒否される

2. **HTTPSのみ**
   - Supabase Edge FunctionsはHTTPSのみをサポート
   - 通信は暗号化される

3. **Stripeのみがアクセス可能**
   - Webhook URLを知っている人のみがアクセス可能
   - 署名検証により、Stripe以外からのリクエストは拒否される

## 参考資料

- [Supabase Edge Functions - Configuration](https://supabase.com/docs/guides/functions/deploy)
- [Stripe Webhooks - Best Practices](https://stripe.com/docs/webhooks/best-practices)
- [Deno Configuration File](https://deno.land/manual/getting_started/configuration_file)

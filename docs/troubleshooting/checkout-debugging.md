# 決済ページが開かない場合のデバッグ方法

## 問題の症状

「サブスクリプションを開始」や「購入する」ボタンをクリックしても、ブラウザが起動しない、または「Checkout URLが取得できませんでした」というエラーが表示される。

## デバッグ出力の確認方法

### Visual Studioの場合

1. Visual Studioでアプリをデバッグ実行（F5）
2. 「表示」→「出力」を開く（または `Ctrl+Alt+O`）
3. 「出力元の表示」を「デバッグ」に設定
4. 購入ボタンをクリック
5. 出力ウィンドウに以下のようなログが表示されます：
   ```
   === サブスクリプション開始 ===
   Supabase URL: https://yzmjuotvkxcfnsgleyxl.supabase.co
   AnonKey設定: True
   リクエストURL: https://yzmjuotvkxcfnsgleyxl.supabase.co/functions/v1/create-checkout-session
   リクエストボディ: {"plan":"StandardSubscription","isSubscription":true}
   レスポンスステータス: 200 OK
   レスポンス内容: {"checkoutUrl":"https://checkout.stripe.com/..."}
   Checkout URL取得: True
   Checkout URL: https://checkout.stripe.com/...
   ブラウザを起動します...
   ```

### Cursorの場合

1. Cursorでアプリをデバッグ実行
2. ターミナルまたはデバッグコンソールを開く
3. 購入ボタンをクリック
4. デバッグ出力を確認

### コマンドラインから実行する場合

```powershell
# デバッグモードで実行
dotnet run --project src/PdfHandler.UI/PdfHandler.UI.csproj
```

## よくあるエラーと対処法

### エラー1: "Checkout URLが取得できませんでした"

**原因**: Supabase Edge Functionが正しく動作していない、またはレスポンスが空

**確認事項**:
1. Supabase Edge Functionがデプロイされているか確認
   - Supabaseダッシュボード → 「Edge Functions」→「create-checkout-session」
2. 環境変数が正しく設定されているか確認
   - `STRIPE_SECRET_KEY`
   - `STRIPE_PRICE_ID_SUBSCRIPTION_STANDARD`
   - `STRIPE_PRICE_ID_PURCHASED`
3. Edge Functionのログを確認
   - Supabaseダッシュボード → 「Edge Functions」→「create-checkout-session」→「Logs」

**対処法**:
- Edge Functionを再デプロイ
- 環境変数を再設定
- StripeのPrice IDが正しいか確認

### エラー2: "customer_creation' can only be used in `payment` mode"

**原因**: Supabase にデプロイ済みの `create-checkout-session` が古いバージョンのまま。サブスクリプション（subscription モード）では `customer_creation` を指定できず、payment モードのみで有効。

**対処法**: Edge Function を再デプロイする。

```powershell
cd プロジェクトディレクトリ
npx supabase functions deploy create-checkout-session
```

※ 最新の `create-checkout-session` では、`customer_creation` を payment モード（買い切り）のときのみ付与するよう修正済み。

### エラー3: "Checkoutセッション作成エラー: 401 Unauthorized"

**原因A**: SupabaseのAPIキーが正しく設定されていない

**確認事項**:
1. 環境変数`SUPABASE_ANON_KEY`が設定されているか確認
2. アプリを再起動して環境変数を読み込む

**対処法**:
```powershell
# 環境変数を確認
echo $env:SUPABASE_ANON_KEY

# 環境変数を設定（PowerShell）
$env:SUPABASE_ANON_KEY = "sb_publishable_..."
```

**原因B**: Edge FunctionのJWT検証で401が返っている

`create-checkout-session`、`stripe-webhook`、`verify-license` では JWT 検証をスキップする必要があります（アノンキーや外部からのリクエストを受け付けるため）。

**重要**: Supabase CLI の既知の不具合により、`deno.json` や `config.toml` の `verify_jwt: false` が **更新時に反映されない** ことがあります（[GitHub Issue #4059](https://github.com/supabase/cli/issues/4059)）。以下の手順を順に試してください。

**対処法1: --no-verify-jwt フラグで再デプロイ（推奨）**
```powershell
cd プロジェクトディレクトリ
npx supabase functions deploy create-checkout-session --project-ref yzmjuotvkxcfnsgleyxl --no-verify-jwt
npx supabase functions deploy stripe-webhook --project-ref yzmjuotvkxcfnsgleyxl --no-verify-jwt
npx supabase functions deploy verify-license --project-ref yzmjuotvkxcfnsgleyxl --no-verify-jwt
```

またはデプロイスクリプトを使用（フラグ付きで実行）:
```powershell
.\scripts\deploy-supabase-functions.ps1
```

**対処法2: Supabase ダッシュボードで手動でJWT検証をOFFにする（最も確実）**

CLIのフラグが効かない場合は、ダッシュボードから直接設定を変更してください。

1. [Supabase ダッシュボード](https://supabase.com/dashboard) にログイン
2. プロジェクトを選択
3. 左メニュー「Edge Functions」を開く
4. 各関数（`create-checkout-session`、`stripe-webhook`、`verify-license`）をクリック
5. 「Configuration」タブを開く
6. **「Verify JWT with legacy secret」** のトグルを **OFF（グレー）** にする
7. 保存

**対処法3: 関数を削除してから再デプロイ**

初期デプロイ時にのみ設定が正しく適用される場合があります。

```powershell
# 注意: 関数が一時的に利用不可になります
npx supabase functions delete create-checkout-session --project-ref yzmjuotvkxcfnsgleyxl
npx supabase functions deploy create-checkout-session --project-ref yzmjuotvkxcfnsgleyxl --no-verify-jwt
# stripe-webhook、verify-license も同様
```

### エラー4: "Checkoutセッション作成エラー: 404 Not Found"

**原因**: Edge FunctionのURLが間違っている、またはEdge Functionがデプロイされていない

**確認事項**:
1. Supabase URLが正しいか確認
   - `https://yzmjuotvkxcfnsgleyxl.supabase.co`
2. Edge Functionがデプロイされているか確認
   - Supabaseダッシュボード → 「Edge Functions」

**対処法**:
- Edge Functionをデプロイ
- `AppSettings.cs`のSupabase URLを確認

### エラー5: "Cannot start process because a file name has not been provided"

**原因**: `checkoutUrl`が`null`または空文字列

**確認事項**:
1. デバッグ出力で`Checkout URL取得: False`と表示されていないか確認
2. `レスポンス内容`を確認して、正しいJSONが返されているか確認

**対処法**:
- Edge Functionのレスポンス形式を確認
- `CheckoutSessionResponse`クラスのプロパティ名が正しいか確認（`checkoutUrl`）

## 手動でテストする方法

### 1. Supabase Edge Functionを直接テスト

```powershell
# PowerShellでテスト
$body = @{
    plan = "StandardSubscription"
    isSubscription = $true
} | ConvertTo-Json

$headers = @{
    "apikey" = "sb_publishable_..."
    "Authorization" = "Bearer sb_publishable_..."
    "Content-Type" = "application/json"
}

$response = Invoke-RestMethod -Uri "https://yzmjuotvkxcfnsgleyxl.supabase.co/functions/v1/create-checkout-session" `
    -Method Post `
    -Headers $headers `
    -Body $body

Write-Host $response.checkoutUrl
```

### 2. ブラウザで直接開く

デバッグ出力で取得した`Checkout URL`をコピーして、ブラウザのアドレスバーに貼り付けて開く。

## トラブルシューティングチェックリスト

- [ ] Supabase Edge Functionがデプロイされている
- [ ] 環境変数（`SUPABASE_ANON_KEY`、`STRIPE_SECRET_KEY`など）が設定されている
- [ ] StripeのPrice IDが正しい
- [ ] アプリを再起動して環境変数を読み込んだ
- [ ] インターネット接続が正常
- [ ] ファイアウォールがEdge Functionへのアクセスをブロックしていない
- [ ] デバッグ出力でエラーの詳細を確認した

## サポート

上記の方法で解決しない場合は、以下の情報を含めてサポートまでお問い合わせください：

1. デバッグ出力の内容（エラーメッセージ全体）
2. Supabase Edge Functionのログ
3. 実行環境（OS、.NETバージョンなど）



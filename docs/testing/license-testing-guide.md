# ライセンス機能テスト手順書

## 前提条件

### ライセンスファイルの場所
- Windows: `%APPDATA%\PDFHandler\license.json`
- パス例: `C:\Users\<ユーザー名>\AppData\Roaming\PDFHandler\license.json`

### Supabaseダッシュボード
- URL: https://supabase.com/dashboard/project/yzmjuotvkxcfnsgleyxl
- 確認箇所:
  - Table Editor → `licenses`テーブル
  - Table Editor → `subscriptions`テーブル
  - Table Editor → `license_activations`テーブル

### Stripeダッシュボード
- URL: https://dashboard.stripe.com/test/dashboard
- 確認箇所:
  - Customers（顧客）
  - Subscriptions（サブスクリプション）
  - Payments（支払い）

---

## テスト1: Standardサブスク契約を開始

### アプリ側の操作
1. アプリを起動
2. 「ヘルプ」→「購入」をクリック
3. 「Standard版（サブスクリプション）」セクションの「サブスクリプションを開始」ボタンをクリック
4. ブラウザでStripe Checkoutページが開くことを確認

### Stripe側の操作（テストモード）
1. Stripe Checkoutページで以下のテストカード情報を入力:
   - カード番号: `4242 4242 4242 4242`
   - 有効期限: 任意の未来の日付（例: `12/34`）
   - CVC: 任意の3桁（例: `123`）
   - 郵便番号: 任意（例: `12345`）
2. 「購入を完了」ボタンをクリック
3. 成功ページが表示されることを確認

### Supabase側の確認
1. Supabaseダッシュボードを開く
2. 「Table Editor」→「licenses」テーブルを開く
3. 以下の内容を確認:
   - `license_key`: `PDFH-`で始まる32文字のキーが生成されている
   - `plan`: `subscription_standard`
   - `user_email`: Stripeで入力したメールアドレス
   - `stripe_customer_id`: Stripeの顧客ID
   - `stripe_subscription_id`: StripeのサブスクリプションID
   - `is_active`: `true`
4. 「subscriptions」テーブルを開く
5. 以下の内容を確認:
   - `stripe_subscription_id`: licensesテーブルと同じID
   - `status`: `active`
   - `current_period_start`: 現在の日時
   - `current_period_end`: 1年後の日時
   - `cancel_at_period_end`: `false`

### アプリ側の確認
1. アプリに戻る
2. 「ヘルプ」→「バージョン情報」→「ライセンス」をクリック
3. ライセンス情報ダイアログで以下を確認:
   - プラン: 「Standard版（サブスクリプション）」
   - ステータス: 「ライセンス有効」
   - サブスクリプション更新日: 1年後の日付が表示される

---

## テスト2: サブスクを解除

### Stripe側の操作
1. Stripeダッシュボードを開く
2. 「Customers」をクリック
3. テスト1で作成した顧客を選択
4. 「Subscriptions」タブを開く
5. アクティブなサブスクリプションの「...」メニューをクリック
6. 「Cancel subscription」を選択
7. キャンセル理由を選択（任意）
8. 「Cancel subscription」ボタンをクリック

### Supabase側の確認
1. Supabaseダッシュボードを開く
2. 「Table Editor」→「subscriptions」テーブルを開く
3. 該当するサブスクリプションの`status`が`canceled`になっていることを確認
4. `cancel_at_period_end`が`true`になっている場合、期間終了時にキャンセルされる

### アプリ側の確認
1. アプリを再起動
2. 「ヘルプ」→「バージョン情報」→「ライセンス」をクリック
3. ライセンス検証が実行される（30日ごと）
4. サブスクリプションがキャンセルされている場合、ライセンスが無効になる可能性がある

### 注意事項
- 現在の実装では、Stripe Webhookでサブスクリプションのキャンセルを検知する機能が未実装の可能性があります
- 手動でSupabaseの`subscriptions`テーブルの`status`を`canceled`に変更してテストすることも可能です

---

## テスト3: 買い切り版を購入

### アプリ側の操作
1. アプリを起動
2. 「ヘルプ」→「購入」をクリック
3. 「Standard版（買い切り）」セクションの「購入する」ボタンをクリック
4. ブラウザでStripe Checkoutページが開くことを確認

### Stripe側の操作（テストモード）
1. Stripe Checkoutページで以下のテストカード情報を入力:
   - カード番号: `4242 4242 4242 4242`
   - 有効期限: 任意の未来の日付（例: `12/34`）
   - CVC: 任意の3桁（例: `123`）
   - 郵便番号: 任意（例: `12345`）
2. 「購入を完了」ボタンをクリック
3. 成功ページが表示されることを確認

### Supabase側の確認
1. Supabaseダッシュボードを開く
2. 「Table Editor」→「licenses」テーブルを開く
3. 以下の内容を確認:
   - `license_key`: `PDFH-`で始まる32文字のキーが生成されている
   - `plan`: `purchased`
   - `user_email`: Stripeで入力したメールアドレス
   - `stripe_customer_id`: Stripeの顧客ID
   - `stripe_payment_intent_id`: Stripeの支払いインテントID
   - `expiration_date`: `null`（買い切り版は有効期限なし）
   - `is_active`: `true`

### アプリ側の確認
1. アプリに戻る
2. 「ヘルプ」→「バージョン情報」→「ライセンス」をクリック
3. ライセンス情報ダイアログで以下を確認:
   - プラン: 「Standard版（買い切り）」
   - ステータス: 「ライセンス有効」

---

## テスト4: ライセンスを削除

### アプリ側の操作
1. アプリを終了
2. エクスプローラーで `%APPDATA%\PDFHandler\` フォルダを開く
3. `license.json`ファイルを削除またはリネーム

### Supabase側の操作（オプション）
1. Supabaseダッシュボードを開く
2. 「Table Editor」→「licenses」テーブルを開く
3. 該当するライセンスの行を選択
4. 「Delete」ボタンをクリック（またはSQL Editorで削除）

### アプリ側の確認
1. アプリを再起動
2. アプリが自動的に14日間のトライアルを開始することを確認
3. 「ヘルプ」→「バージョン情報」→「ライセンス」をクリック
4. プランが「試用期間中」になっていることを確認

---

## テスト5: 14日間トライアルに戻る（残日数保持）

### アプリ側の操作
1. アプリを終了
2. エクスプローラーで `%APPDATA%\PDFHandler\license.json` を開く（テキストエディタで）
3. 以下のように編集:
   ```json
   {
     "Plan": 0,
     "LicenseKey": null,
     "FirstLaunchDate": "2025-01-01T00:00:00",
     "HardwareId": "<既存のHardwareIdを保持>"
   }
   ```
   - `Plan`: `0`（Trial）
   - `FirstLaunchDate`: 残日数を保持したい日付（例: 10日前なら `DateTime.Now.AddDays(-4)` の日付）
   - `LicenseKey`: `null`に設定
4. ファイルを保存

### アプリ側の確認
1. アプリを起動
2. 「ヘルプ」→「バージョン情報」→「ライセンス」をクリック
3. プランが「試用期間中」になっていることを確認
4. 残り日数が正しく表示されることを確認（例: 10日前なら残り4日）

---

## テスト6: 開発用 - 残日数0設定と残日数14日へ戻す方法

### 残日数0に設定する方法

#### 方法1: FirstLaunchDateを14日前に設定
1. アプリを終了
2. `%APPDATA%\PDFHandler\license.json` を開く
3. 以下のように編集:
   ```json
   {
     "Plan": 0,
     "LicenseKey": null,
     "FirstLaunchDate": "2025-01-01T00:00:00",
     "HardwareId": "<既存のHardwareIdを保持>"
   }
   ```
   - `FirstLaunchDate`: 現在の日時から14日前の日時を設定
   - 例: 今日が2025年1月15日なら、`2025-01-01T00:00:00`（14日前）

#### 方法2: FirstLaunchDateを15日前以降に設定
- 15日前以降に設定すると、残日数は0日になる

### 残日数14日に戻す方法

1. アプリを終了
2. `%APPDATA%\PDFHandler\license.json` を開く
3. 以下のように編集:
   ```json
   {
     "Plan": 0,
     "LicenseKey": null,
     "FirstLaunchDate": "2025-01-15T00:00:00",
     "HardwareId": "<既存のHardwareIdを保持>"
   }
   ```
   - `FirstLaunchDate`: 現在の日時を設定
   - 例: 今日が2025年1月15日なら、`2025-01-15T00:00:00`

### アプリ側の確認
1. アプリを起動
2. 「ヘルプ」→「バージョン情報」→「ライセンス」をクリック
3. 残り日数が正しく表示されることを確認

---

## 便利なSQLクエリ（Supabase）

### ライセンス一覧を確認
```sql
SELECT 
    license_key,
    plan,
    user_email,
    is_active,
    activation_date,
    created_at
FROM licenses
ORDER BY created_at DESC;
```

### サブスクリプション一覧を確認
```sql
SELECT 
    s.stripe_subscription_id,
    s.status,
    s.current_period_start,
    s.current_period_end,
    s.cancel_at_period_end,
    l.license_key,
    l.plan,
    l.user_email
FROM subscriptions s
JOIN licenses l ON s.license_id = l.id
ORDER BY s.created_at DESC;
```

### ライセンスを削除（テスト用）
```sql
-- 注意: 関連するlicense_activationsとsubscriptionsも自動的に削除されます（CASCADE）
DELETE FROM licenses WHERE license_key = 'PDFH-XXXXXXXX';
```

### サブスクリプションをキャンセル状態に変更（テスト用）
```sql
UPDATE subscriptions 
SET status = 'canceled', cancel_at_period_end = true
WHERE stripe_subscription_id = 'sub_xxxxxxxxxxxxx';
```

### ライセンスを無効化（テスト用）
```sql
UPDATE licenses 
SET is_active = false
WHERE license_key = 'PDFH-XXXXXXXX';
```

---

## トラブルシューティング

### ライセンスファイルが見つからない場合
- パス: `%APPDATA%\PDFHandler\license.json`
- ファイルが存在しない場合は、アプリを起動すると自動的に作成されます

### Stripe Webhookが動作しない場合
1. Stripeダッシュボードで「Developers」→「Webhooks」を開く
2. Webhookエンドポイントのログを確認
3. Supabase Edge Functionのログを確認:
   - Supabaseダッシュボード → 「Edge Functions」→「stripe-webhook」→「Logs」

### ライセンス検証が失敗する場合
1. Supabase Edge Functionのログを確認:
   - Supabaseダッシュボード → 「Edge Functions」→「verify-license」→「Logs」
2. アプリのデバッグ出力を確認（Visual Studioの出力ウィンドウ）

### ハードウェアIDが変更された場合
- ハードウェアIDはマシン固有のIDです
- ハードウェアIDが変更されると、ライセンスは自動的に試用期間にリセットされます
- これは意図的な動作です（1ライセンスにつき最大3デバイスまで）

---

## テスト用ライセンスキーの形式

- 形式: `PDFH-` + 32文字の英数字（大文字）
- 例: `PDFH-A1B2C3D4E5F6G7H8I9J0K1L2M3N4O5P6`

### テスト用ライセンスキーを手動で生成（Supabase SQL Editor）
```sql
SELECT 'PDFH-' || UPPER(REPLACE(gen_random_uuid()::TEXT, '-', ''));
```

---

## 注意事項

1. **テストモード**: Stripeのテストモードを使用してください（本番モードでは実際に課金されます）
2. **データ削除**: テスト後は不要なライセンスデータを削除してください
3. **ハードウェアID**: ハードウェアIDはマシン固有のため、別のPCでは異なるIDになります
4. **ライセンスファイル**: ライセンスファイルを直接編集する場合は、JSON形式を正しく保つ必要があります



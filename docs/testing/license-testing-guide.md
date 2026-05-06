# ライセンス機能テスト手順書

## 前提条件

### ライセンスファイルの場所
- Windows: `%APPDATA%\PDFHandler\license.json`
- パス例: `C:\Users\<ユーザー名>\AppData\Roaming\PDFHandler\license.json`

### Supabase ダッシュボード
- URL: https://supabase.com/dashboard/project/yzmjuotvkxcfnsgleyxl
- 確認箇所:
  - Table Editor → `licenses`
  - Table Editor → `license_activations`

### Stripe ダッシュボード（テストモード）
- URL: https://dashboard.stripe.com/test/dashboard
- 確認箇所:
  - Customers（顧客）
  - Payments（支払い）

本プロダクトは **買い切り（一回払い）のみ** です。Webhook は `checkout.session.completed` のみを利用し、`subscriptions` テーブルは用いません。

---

## テスト1: 買い切り版を購入

### アプリ側の操作
1. アプリを起動
2. 「ヘルプ」→「購入」をクリック
3. 「Standard版（買い切り）」の「購入する」ボタンをクリック
4. ブラウザで Stripe Checkout が開くことを確認

### Stripe 側の操作（テストモード）
1. テストカード例:
   - カード番号: `4242 4242 4242 4242`
   - 有効期限: 任意の未来の日付（例: `12/34`）
   - CVC: 任意の3桁（例: `123`）
   - 郵便番号: 任意（例: `12345`）
2. 「購入を完了」をクリックし、成功ページを確認

### Supabase 側の確認
1. 「Table Editor」→ `licenses` を開く
2. 次を確認:
   - `license_key`: `PDFH-P101-` で始まる **新形式（28文字シリアル + HMAC）** または旧 32 文字形式
   - `plan`: `purchased`
   - `user_email`: Checkout で入力したメール
   - `stripe_customer_id` / `stripe_payment_intent_id`: 値が入っていること
   - `expiration_date`: `null`（買い切りは期限なし）
   - `is_active`: `true`

### アプリ側の確認
1. メールに届いたキー、または Supabase の `license_key` をコピー
2. 「ヘルプ」→「ライセンス」でキーを入力しアクティベート
3. 「ヘルプ」→「バージョン情報」→「ライセンス」で **Standard版（買い切り）** と有効であることを確認

---

## テスト2: ライセンスファイルを削除（トライアルへ戻す）

### アプリ側の操作
1. アプリを終了
2. `%APPDATA%\PDFHandler\` の `license.json` を削除またはリネーム

### アプリ側の確認
1. アプリを再起動
2. 14 日トライアルが開始されること
3. 「ライセンス」でプランが「試用期間中」であること

---

## テスト3: 14日トライアルに戻る（残日数を編集）

1. アプリを終了し、`license.json` をテキストで開く
2. 例:
   ```json
   {
     "Plan": 0,
     "LicenseKey": null,
     "FirstLaunchDate": "2025-01-01T00:00:00",
     "HardwareId": "<既存の HardwareId を保持>"
   }
   ```
3. `Plan`: `0`（Trial）。`FirstLaunchDate` で残日数を調整
4. 保存後、アプリを起動して表示を確認

---

## テスト4: 開発用 — 残日数 0 / 14 への切り替え

- スクリプト: `docs/testing/` 配下の `SetRemainingDays.ps1` 等があれば利用
- またはテスト3と同様に `FirstLaunchDate` を現在から 14 日前（残り0）／当日（残り14）に設定

---

## 便利な SQL（Supabase）

### ライセンス一覧
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

### ライセンス削除（テスト環境のみ）
```sql
-- 関連する license_activations は FK で連動削除される想定
DELETE FROM licenses WHERE license_key = 'PDFH-P101-...';
```

### ライセンス無効化（テスト用）
```sql
UPDATE licenses
SET is_active = false
WHERE license_key = 'PDFH-P101-...';
```

---

## トラブルシューティング

### Webhook が動作しない
1. Stripe → Developers → Webhooks で `checkout.session.completed` のみ登録されているか
2. Supabase Edge Function `stripe-webhook` のログを確認

### ライセンス検証が失敗する
1. `verify-license` のログを確認
2. キーが `licenses.license_key` と一致（正規化後）しているか
3. `is_active` が `true` か

### 決済ページが開かない
- [checkout-debugging.md](../troubleshooting/checkout-debugging.md) を参照

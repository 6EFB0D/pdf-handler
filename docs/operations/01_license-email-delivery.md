# 1. ライセンス発行後のメール自動配信

## 概要

決済完了後、Stripe Webhook が `stripe-webhook` Edge Function を呼び出し、Supabase の `licenses` テーブルにライセンスを登録します。同時に、**Resend** を使って購入者へライセンスキーをメールで自動送信します。

## 実装済みの内容

- `supabase/functions/stripe-webhook/index.ts` に `sendLicenseEmail()` を追加
- `RESEND_API_KEY` が設定されている場合のみメール送信（未設定時はスキップ、エラーにならない）
- メール本文: ライセンスキー、プラン名、入力方法の案内

## セットアップ手順

### 1. Resend アカウント作成

1. [Resend](https://resend.com) にアクセス
2. アカウント作成（無料プランあり）
3. [API Keys](https://resend.com/api-keys) で API キーを発行

### 2. ドメインの検証（任意・推奨）

個人情報を露出しないため、**独自ドメイン**を使うことを推奨します。

- Resend ダッシュボード → Domains → Add Domain
- 例: `noreply@pdf-handler.yourdomain.com` など
- DNS レコード（SPF、DKIM）を設定して検証

**独自ドメインがない場合**: 初期は `onboarding@resend.dev` で送信可能（1日100通まで、テスト用）

### 3. Supabase に環境変数を設定

Supabase ダッシュボード → Settings → Edge Functions → Secrets:

| 変数名 | 値 | 必須 |
|--------|-----|------|
| `RESEND_API_KEY` | Resend の API キー | メール送信する場合 |
| `LICENSE_EMAIL_FROM` | `PDFハンドラ <noreply@yourdomain.com>` | 未設定時は `onboarding@resend.dev` |

### 4. stripe-webhook の再デプロイ

```powershell
cd プロジェクトディレクトリ
npx supabase functions deploy stripe-webhook --project-ref yzmjuotvkxcfnsgleyxl --no-verify-jwt
```

または `scripts/deploy-supabase-functions.ps1` を実行

### 5. 動作確認

1. アプリから購入 → Stripe Checkout で決済完了
2. 購入時に入力したメールアドレスにライセンスキーが届くか確認
3. Supabase Edge Functions → stripe-webhook → Logs で `License email sent to xxx` を確認

## 個人情報の保護

- **送信元メール**: `LICENSE_EMAIL_FROM` で `noreply@yourdomain.com` など**事業者名・ドメイン**のみを表示
- **個人のメールアドレスは使わない**: 例: `support@yourdomain.com` や `noreply@yourdomain.com` を使用
- **署名**: メール本文に個人名を入れない。必要なら「PDFハンドラ サポート」など事業者名のみ

## トラブルシューティング

### メールが届かない

- `RESEND_API_KEY` が正しく設定されているか確認
- Resend ダッシュボードの Logs で送信状況を確認
- 迷惑メールフォルダを確認
- Stripe の `customer_email` が正しく取得されているか確認（Checkout でメール入力必須）

### 送信が失敗する（403 など）

- ドメイン未検証のまま独自ドメインの from を使っている場合
- 一時的に `onboarding@resend.dev` で送信して動作確認

## 関連リンク

- [Resend - Send with Supabase](https://resend.com/docs/send-with-supabase-edge-functions)
- [Resend ドメイン検証](https://resend.com/domains)

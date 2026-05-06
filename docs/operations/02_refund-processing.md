# 2. 返金を求められた際の処理方法

## 概要

顧客から返金を求められた場合の、Stripe での返金手順と、ライセンス無効化の対応です。

## 前提

- 返金ポリシーは「購入後14日以内であれば無条件で全額返金」など、事前に決めておく
- サポートサイトや購入ガイドで明示しておく

## 処理フロー

```
顧客から返金依頼
    ↓
[1] 購入確認（Stripe / Supabase）
    ↓
[2] Stripe で返金実行
    ↓
[3] ライセンスを無効化（Supabase）
    ↓
[4] 顧客へ返金完了の連絡
```

## 手順

### 1. 購入の確認

**Stripe ダッシュボード**で確認:

1. [Payments](https://dashboard.stripe.com/payments) を開く
2. 顧客のメールアドレスまたは Payment ID で検索
3. 該当する決済を確認（金額、日付、支払いステータス）

**Supabase** で確認:

1. Table Editor → `licenses`
2. `user_email` または `stripe_payment_intent_id` で検索
3. 該当する `license_key` をコピー（後で無効化に使用）

### 2. Stripe で返金実行

**方法A: 全額返金**

1. Stripe ダッシュボード → Payments → 該当の決済をクリック
2. 「Refund payment」をクリック
3. 返金理由を選択（例: Requested by customer）
4. 「Refund」をクリック

**方法B: 部分返金**

1. 上記と同様に「Refund payment」を開く
2. 「Partial refund」を選択
3. 返金金額を入力

### 3. ライセンスの無効化

返金後は、ライセンスを無効化する必要があります。

**Supabase SQL Editor** で実行:

```sql
-- license_key を該当のキーに置き換える
UPDATE licenses
SET is_active = false
WHERE license_key = 'PDFH-XXXXXXXXXXXXXXXX';
```

**または Table Editor** で:

1. `licenses` テーブルを開く
2. 該当行を選択
3. `is_active` を `false` に変更

### 4. 顧客への連絡

返金完了とライセンス無効化を伝えるメールを送信。テンプレート例:

```
件名: 【PDFハンドラ】返金完了のご連絡

ご依頼いただいた返金処理が完了いたしました。

・返金金額: ¥X,XXX
・返金先: ご利用のカード（数営業日で反映されます）

ライセンスは無効化いたしました。アプリの再起動後、
試用期間またはライセンス未所持の状態となります。

ご不明な点がございましたら、お問い合わせください。
```

## 個人情報の保護

- **連絡は事業者メールから**: `support@yourdomain.com` など、個人名を出さない
- **返金理由の記録**: Stripe の「Reason」に記録するだけで、顧客の詳細は Stripe に残す
- **問い合わせフォーム**: 返金依頼はフォーム経由で受け、個人メールは使わない

## 自動化の検討（将来）

- Stripe の `charge.refunded` Webhook で返金を検知
- `stripe-webhook` に `charge.refunded` ハンドラを追加
- 該当ライセンスを自動で `is_active = false` に更新

現状は手動対応で問題ありません。

## 関連リンク

- [Stripe 返金ドキュメント](https://docs.stripe.com/refunds)

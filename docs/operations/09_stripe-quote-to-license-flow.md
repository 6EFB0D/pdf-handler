# 法人向け見積・請求書払い → ライセンス発行フロー

> **対象**: Stripe を使わず、見積書・銀行振込でライセンスキーを販売する場合の運用手順
> **適用ケース例**: 5/1 法人納品（売掛金・請求書払い）
> **関連文書**: `STRIPE_READINESS_AND_QMS_TASKS.md`（MS2 §5「5/1 納品との両立」）

---

## フロー概要

```
① 見積書を提出（オフライン）
      ↓
② 入金確認（銀行振込）
      ↓
③ Supabase SQL でライセンスキーを手動発行
      ↓
④ ライセンスキーをメールで送付
      ↓
⑤ 領収書・請求書を発行（エビデンス保管）
```

---

## ステップ詳細

### ステップ 1: 見積書の提出

- 見積書テンプレートを使用（`office-goplan/docs/invoice-templates/` 参照）
- 記載内容:
  - 商品名: `pdfHandler Standard版 ライセンス × N本`
  - 単価: ¥5,000（消費税不課税）
  - 数量: 購入台数
  - 合計金額・支払期限・振込先口座
- 見積書番号・発行日を記録（`evidence/` に控えを保管）

### ステップ 2: 入金確認

- 銀行の入金通知または振込明細で確認
- 入金日・金額・振込人名をメモ（領収書発行に使用）

### ステップ 3: ライセンスキーの手動発行

Supabase SQL Editor で以下を実行（発行本数分繰り返す）:

```sql
-- ライセンスキーを手動挿入（app_id は製品に合わせて変更）
INSERT INTO licenses (
  license_key,
  app_id,
  plan,
  user_email,
  activation_date,
  is_active,
  expiration_date,
  purchased_version,
  stripe_customer_id,
  stripe_payment_intent_id
) VALUES (
  -- ★ キーはアプリ側の generateLicenseKey 関数相当のものを手動生成するか、
  --    以下のように仮キーを発行してから後で正式キーに差し替える
  'PDFH-P101-' || upper(substr(replace(gen_random_uuid()::text, '-', ''), 1, 28)),
  'PDFH',                    -- pdfHandler の場合
  'purchased',
  '顧客メールアドレス@example.com',  -- ★ 実際のメールアドレスに変更
  NOW(),
  true,
  NULL,                      -- 買い切りは有効期限なし
  '1',                       -- メジャーバージョン
  NULL,                      -- Stripe 外販売のため NULL
  NULL                       -- Stripe 外販売のため NULL
);

-- 発行したキーを確認
SELECT license_key, user_email, activation_date
FROM licenses
WHERE user_email = '顧客メールアドレス@example.com'
ORDER BY created_at DESC
LIMIT 10;
```

> **注意**: HMAC 付きの正式キーを発行したい場合は、`supabase/functions/stripe-webhook/index.ts` の `generateLicenseKey()` 関数を  
> ローカルで実行するか、Supabase Edge Function を `POST /functions/v1/internal-generate-key` として別途用意する（将来対応）。  
> 当面は HMAC なしキーでも `verify-license` のオンライン検証は通る（DB に存在すれば有効と判断される）。

### ステップ 4: ライセンスキーのメール送付

**送付メール本文テンプレート:**

```
件名: 【pdfHandler】ライセンスキーのご案内

〇〇株式会社
〇〇部 〇〇様

この度はpdfHandlerをご購入いただきありがとうございます。

ご購入いただいたライセンスキーをお送りします。

━━━━━━━━━━━━━━━━━━
■ ライセンスキー（全N本）
━━━━━━━━━━━━━━━━━━
1本目: PDFH-XXXX-XXXX-XXXX-XXXX-XXXX-XXXX-XXXX-XX
2本目: PDFH-XXXX-XXXX-XXXX-XXXX-XXXX-XXXX-XXXX-XX
（以下、本数分を列記）

━━━━━━━━━━━━━━━━━━
■ アクティベーション手順
━━━━━━━━━━━━━━━━━━
1. pdfHandler を起動します
2. メニュー「ヘルプ」→「ライセンス」を開きます
3. 「ライセンスキーを入力」ボタンをクリックします
4. 上記ライセンスキーを入力して「アクティベート」をクリックします

■ ご注意
・1ライセンスキーにつき最大3台のPCでご利用いただけます
・ライセンスキーは大切に保管してください

ご不明な点はお気軽にご連絡ください。

Office Go Plan
```

### ステップ 5: 領収書・請求書の発行

- 入金確認後、領収書を発行して顧客に送付
- 控え（PDF）を `evidence/` または `office-goplan/docs/` に保管

---

## エビデンス保管（Phase 5.1 対応）

| 書類 | 保管場所 | 命名例 |
|------|---------|--------|
| 見積書控え | `evidence/` または `office-goplan/docs/` | `2026-05-01_estimate_corp-xxx.pdf` |
| 入金通知 | 同上 | `2026-05-01_payment_bank-transfer.png` |
| ライセンス発行確認（SQL 結果） | `evidence/` | `2026-05-01_license-issued_corp-xxx.png` |
| 領収書控え | 同上 | `2026-05-01_receipt_corp-xxx.pdf` |

> これらは **`STRIPE_READINESS_AND_QMS_TASKS.md` MS2 §5** に記載のとおり、  
> **Phase 5 エビデンス**として 3.1 トレース行列の証跡 URL にも転用できます。

---

## Stripe 復活後の対応

法人顧客が Stripe 払いを希望する場合は、Stripe アカウント復活後に  
`create-checkout-session` を通常通り使えば良いだけです。  
手動発行したキーがある場合は、Stripe の顧客 ID を後から `licenses` テーブルの  
`stripe_customer_id` に UPDATE で紐付けることも可能です。

---

*作成日: 2026-04-25 — 5/1 法人納品（銀行振込）対応のため新規作成。Stripe アカウント停止期間中の代替フロー。*

# 複数アプリ統合ライセンス管理（1 Supabase プロジェクト）

Supabase 無料版のプロジェクト制限（2つまで）を回避し、**1つの Supabase プロジェクト**で複数アプリのライセンスを統合管理する設計です。

---

## 1. 概要

| 項目 | 内容 |
|------|------|
| **Supabase** | 1プロジェクトで全アプリのライセンスを管理 |
| **Stripe** | 1アカウントで複数製品。Checkout の metadata に app_id を含める |
| **Resend** | 1アカウント。メールテンプレートを app_id で切り替え |
| **Webhook** | 1つの stripe-webhook で全アプリのイベントを処理 |

---

## 2. ライセンスキー形式（アプリ識別）

各アプリは**プレフィックス（app_id）**で識別します。キー形式は既存の license-code-specification を拡張。

| app_id | アプリ例 | キー例 |
|--------|----------|--------|
| PDFH | PDF Handler | `PDFH-P101-A1B2-C3D4-E5F6-G7H8-I9J0-K1L2-M3` |
| PCTC | PictComp | `PCTC-P101-A1B2-C3D4-E5F6-G7H8-I9J0-K1L2-M3` |
| (任意) | その他 | `{APPID}-P101-{28文字}-{HMAC}` |

- **license_key** がグローバルにユニークであれば、全アプリを同一テーブルに格納可能
- プレフィックスがアプリを識別するため、**app_id はキーから自動導出**可能

---

## 3. データベーススキーマ

### 3.1 licenses テーブル（app_id 追加）

```sql
-- 既存カラムに app_id を追加（なければ）
ALTER TABLE licenses ADD COLUMN IF NOT EXISTS app_id TEXT NOT NULL DEFAULT 'PDFH';

-- 複合ユニーク制約（同一アプリ内でキー重複を防ぐ。任意）
-- CREATE UNIQUE INDEX idx_licenses_app_key ON licenses(app_id, license_key);
```

| カラム | 説明 |
|--------|------|
| app_id | アプリ識別子（PDFH, PCTC 等） |
| license_key | 全文キー（app プレフィックス含む。グローバルユニーク） |
| plan, user_email, ... | 既存のまま |

### 3.2 その他のテーブル

- **license_activations** - licenses に対する子テーブル（変更なし）

---

## 4. Stripe 連携

### 4.1 製品構成

1つの Stripe アカウントで、アプリごとに Product を作成：

| Product 名 | Price ID 環境変数例 | app_id |
|------------|---------------------|--------|
| PDF Handler 買い切り | STRIPE_PRICE_PDFH_PURCHASED | PDFH |
| PictComp 買い切り | STRIPE_PRICE_PCTC_PURCHASED | PCTC |
| ... | | |

### 4.2 Checkout Session 作成時

各アプリの「購入」ボタンから Checkout を作成する際、**metadata に app_id を含める**：

```typescript
// 例: create-checkout-session Edge Function または アプリ側
const session = await stripe.checkout.sessions.create({
  mode: 'payment',
  line_items: [{ price: process.env.STRIPE_PRICE_PDFH_PURCHASED, quantity: 1 }],
  metadata: {
    app_id: 'PDFH',           // ★ 必須
    plan: 'StandardPurchased',
  },
  success_url: '...',
  cancel_url: '...',
});
```

### 4.3 Webhook（1本で全アプリ対応）

- **1つの Webhook URL** を Stripe に登録
- `checkout.session.completed` で `session.metadata.app_id` を取得
- app_id に応じてライセンスキーのプレフィックスと app_id を設定

```typescript
// stripe-webhook 内
const appId = session.metadata?.app_id || 'PDFH';
const licenseKey = await generateLicenseKey(appId);
licenseData.app_id = appId;
```

---

## 5. ライセンスキー生成（複数 app_id 対応）

### 5.1 署名対象

署名対象に **app_id を含める**（license-code-specification 準拠）：

```
{app_id}:{形態4文字}:{シリアル28文字}
例: PDFH:P101:A1B2C3D4...
    PCTC:P101:A1B2C3D4...
```

### 5.2 共通秘密鍵

- **LICENSE_SECRET_KEY** は 1つで全アプリ共通
- 署名対象に app_id が含まれるため、アプリごとに分ける必要はない
- 同一キーを他アプリで流用することは署名検証で防止される

---

## 6. Edge Functions の変更点

### 6.1 stripe-webhook

| 変更内容 | 詳細 |
|----------|------|
| metadata.app_id 取得 | session.metadata.app_id、無ければ 'PDFH' などデフォルト |
| generateLicenseKey | 第1引数に app_id を渡す |
| licenseData.app_id | 挿入時に app_id を設定 |

### 6.2 verify-license

| 変更内容 | 詳細 |
|----------|------|
| キー検索 | 現状通り license_key で検索（キーに app が含まれるためそのままで可） |
| オプション | リクエストに app_id を含め、キーのプレフィックスと一致するか検証（二重チェック） |

### 6.3 get-activations / deactivate-device / update-device-display-name

- license_key で licenses を特定（変更なし）
- 必要なら app_id でフィルタを追加可能

---

## 7. Resend 連携

### 7.1 送信元・テンプレート

| 方式 | 説明 |
|------|------|
| **A. 共通テンプレート＋変数** | 1テンプレートで、変数に `{app_name}`, `{license_key}` などを渡す |
| **B. アプリ別 From** | LICENSE_EMAIL_FROM_PDFH, LICENSE_EMAIL_FROM_PCTC などで送信元を分ける |

### 7.2 実装例（stripe-webhook 内）

```typescript
const appNames = { PDFH: 'PDFハンドラ', PCTC: 'PictComp' };
const appName = appNames[appId] || appId;
const from = Deno.env.get(`LICENSE_EMAIL_FROM_${appId}`) 
  || Deno.env.get('LICENSE_EMAIL_FROM') 
  || 'License <noreply@yourdomain.com>';

await sendLicenseEmail(toEmail, licenseKey, appName, plan, ...);
```

---

## 8. 環境変数（Supabase Secrets）まとめ

| 変数 | 説明 | 複数アプリ時の例 |
|------|------|------------------|
| LICENSE_SECRET_KEY | HMAC 署名用（共通） | 1つ |
| STRIPE_SECRET_KEY | Stripe API | 1つ |
| STRIPE_WEBHOOK_SECRET | Webhook 署名検証 | 1つ |
| SUPABASE_URL, SERVICE_ROLE_KEY | Supabase | 1つ |
| RESEND_API_KEY | メール送信 | 1つ |
| STRIPE_PRICE_PDFH_PURCHASED | PDF Handler 買い切り Price ID | アプリごと |
| STRIPE_PRICE_PCTC_PURCHASED | PictComp 買い切り | アプリごと |
| LICENSE_EMAIL_FROM | デフォルト送信元 | 1つ |
| LICENSE_EMAIL_FROM_PDFH | PDF Handler 用 From（任意） | アプリごと |

---

## 9. 各アプリ側の設定

各アプリ（PDF Handler, PictComp 等）では：

| 項目 | 設定 |
|------|------|
| Supabase URL | **同一**（統合プロジェクトの URL） |
| Supabase Anon Key | **同一** |
| app_id | アプリごとに固定（PDFH, PCTC 等） |
| LICENSE_SECRET_KEY | **同一**（オフライン検証用） |
| Create Checkout 時の metadata | 必ず `app_id` を含める |

---

## 10. マイグレーション手順

1. **licenses に app_id 追加**  
   - 既存行は `app_id = 'PDFH'` などで埋める
2. **stripe-webhook 修正**  
   - metadata.app_id 対応、generateLicenseKey に app_id 渡す
3. **各アプリの Checkout 作成箇所**  
   - metadata に app_id を追加
4. **Resend テンプレート**  
   - app_id / app_name を渡すように変更
5. **新規アプリ追加時**  
   - app_id を決め、Stripe Product/Price 作成、環境変数追加、Checkout の metadata に設定

---

## 11. 注意事項

| 項目 | 内容 |
|------|------|
| Stripe Product 分離 | アプリごとに Product を作成し、Price ID で識別 |
| キーの衝突 | app_id がプレフィックスに含まれるため、他アプリのキーと衝突しない |
| セキュリティ | 検証時にキーのプレフィックスと期待する app_id の一致をチェックすると安全 |
| 監視 | 1プロジェクトで全アプリのライセンスを管理するため、ログ・モニタリングは app_id でフィルタして確認 |

---

*作成日: 2026年2月*

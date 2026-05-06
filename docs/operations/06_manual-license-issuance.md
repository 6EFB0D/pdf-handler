# 手動ライセンス発行手順（法人・銀行振込対応）

> 対象: Stripe を使わず、見積書→納品書→請求書→銀行振込 の流れで発行するライセンス  
> 前提: `15_add_manual_payment_fields.sql` を Supabase で実行済みであること

---

## 全体フロー

```
① 見積書を作成・送付
       ↓
② 発注確認（注文書 or メール承諾）
       ↓
③ ライセンスキーを生成（本手順書 Step 1）
       ↓
④ 納品書＋ライセンスキーを送付（本手順書 Step 2）
       ↓
⑤ 請求書を送付（外部ツールで発行）
       ↓
⑥ 入金確認（銀行通帳・振込通知を確認）
       ↓
⑦ Supabase に登録（本手順書 Step 3）
       ↓
⑧ エビデンスを記録（本手順書 Step 4）
```

> **注意**: ライセンスキーは納品時に送付しますが、`is_active = true` の登録は  
> **入金確認後（Step 3）** に行います。発行前でも送付はできますが、  
> 登録前は認証サーバーに存在しないためアクティベーション不可です。  
> 運用ポリシーに応じて「入金前仮発行」「入金後発行」を選択してください。

---

## Step 1: ライセンスキーの生成

### 1-1. LICENSE_SECRET_KEY を確認する

Supabase ダッシュボード → **Project Settings → Edge Functions → Secrets** から  
`LICENSE_SECRET_KEY` の値をコピーします。

> ⚠️ この値は絶対に外部に漏らさないこと。作業後はターミナル履歴をクリアしてください。

### 1-2. PowerShell スクリプトを実行する

```powershell
# pdf-handler プロジェクトルートで実行
.\scripts\generate-license-key.ps1 -SecretKey "ここにLICENSE_SECRET_KEYを貼る"

# 複数ライセンスを一括生成する場合（例: 3件）
.\scripts\generate-license-key.ps1 -SecretKey "xxx" -Count 3
```

### 1-3. 出力を記録する

```
[1] DB保存用（正規化）:
      PDFH-P101-A1B2C3D4E5F6G7H8I9J0K1L2-3A4B5C6D...（←これを Supabase に INSERT）
    メール送付用（表示）:
      PDFH-P101-A1B2-C3D4-E5F6-G7H8-I9J0-K1L2-3A4B5C6D...（←これをメールで送付）
```

> メール送付用キーは自動的にクリップボードにコピーされます（1件の場合）。

### FormCode の選択

| FormCode | 意味 | 用途 |
|----------|------|------|
| `P101`   | 買い切り（標準） | 個人・法人の通常購入 |
| `P201`   | 買い切り（複数本パック） | 複数ライセンスの一括購入 |

---

## Step 2: 納品書＋ライセンスキーの送付

### メール文面例（サポートメールから送付）

```
件名: 【Office Go Plan】pdfHandler ライセンスキーのご案内

〇〇株式会社
〇〇様

この度はpdfHandlerをご購入いただき、ありがとうございます。
以下のライセンスキーをお送りします。

━━━━━━━━━━━━━━━━━━━━━━━━
ライセンスキー:
PDFH-P101-XXXX-XXXX-XXXX-XXXX-XXXX-XXXX-XXXX-XXXXXXXX...

対応台数: 最大3台
━━━━━━━━━━━━━━━━━━━━━━━━

アクティベーション手順:
1. pdfHandler を起動します
2. メニュー → ライセンス認証 を開きます
3. 上記ライセンスキーを入力して「認証」をクリックします

ご不明な点はこのメールにご返信ください。

---
Office Go Plan サポート
support@office-goplan.com
```

---

## Step 3: Supabase への登録（入金確認後）

### 3-1. Supabase Table Editor を開く

Supabase ダッシュボード → **Table Editor** → `licenses` テーブル → **Insert row**

### 3-2. INSERT する値

| 列名 | 値 | 備考 |
|------|-----|------|
| `id` | （自動生成） | 変更不要 |
| `license_key` | Step 1 の **DB保存用（正規化）** キー | 表示用ではなく正規化形式を入れる |
| `plan` | `purchased` | 固定 |
| `app_id` | `PDFH` | pdfHandler の場合 |
| `user_email` | 担当者のメールアドレス | サポートメールでのやりとり相手 |
| `is_active` | `true` | 入金確認後に true にする |
| `activation_count` | `0` | 変更不要 |
| `payment_type` | `manual` | **必須** |
| `customer_company` | 会社名（例: 〇〇株式会社） | 法人の場合 |
| `customer_contact` | 担当者名（例: 山田 太郎） | |
| `invoice_number` | 請求書番号（例: INV-2026-001） | 外部ツールの番号 |
| `payment_confirmed_at` | 入金確認日時（例: `2026-04-19T15:00:00+09:00`） | |
| `notes` | 管理メモ（例: A社 購買部対応・3台使用予定） | 任意 |
| `stripe_*` 列 | NULL のまま | 変更不要 |

### 3-3. SQL で INSERT する場合（複数件・効率的）

```sql
INSERT INTO licenses (
  license_key,
  plan,
  app_id,
  user_email,
  is_active,
  activation_count,
  payment_type,
  customer_company,
  customer_contact,
  invoice_number,
  payment_confirmed_at,
  notes
) VALUES (
  'PDFH-P101-xxxxxxxxxxxxxxxxxxxxxxxx-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',
  'purchased',
  'PDFH',
  'yamada@example.co.jp',
  true,
  0,
  'manual',
  '〇〇株式会社',
  '山田 太郎',
  'INV-2026-001',
  NOW(),
  '法人契約・3台使用予定'
);
```

### 3-4. 登録確認

```sql
-- 登録したライセンスキーの先頭部分で検索して確認
SELECT
  id,
  license_key,
  user_email,
  is_active,
  payment_type,
  customer_company,
  invoice_number,
  payment_confirmed_at
FROM licenses
WHERE payment_type = 'manual'
ORDER BY created_at DESC
LIMIT 5;
```

---

## Step 4: エビデンスの記録

`evidence/` フォルダに以下のファイルを作成します。

**ファイル名例**: `evidence/2026-04-19_manual_INV-2026-001.md`

```markdown
# 手動発行記録: INV-2026-001

- 発行日: 2026-04-19
- 顧客: 〇〇株式会社 山田 太郎 様
- ライセンス数: 1
- app_id: PDFH
- invoice_number: INV-2026-001
- 入金確認日: 2026-04-19
- 入金確認方法: 銀行通帳（〇〇銀行）
- Supabase licenses.id: （UUIDをここに貼る）
- 対応者: （自分の名前 or 識別子）
- 備考: 3台使用予定。追加ライセンスの相談あり。
```

---

## トラブルシューティング

### アクティベーションが「ライセンスが見つかりません」になる

| 確認箇所 | チェック内容 |
|---------|------------|
| `license_key` | 正規化形式（ハイフン区切りの正しい形式）で INSERT されているか |
| `is_active` | `true` になっているか（入金前に `false` で登録した場合） |
| `app_id` | `PDFH` になっているか |
| `plan` | `purchased` になっているか |

### `license_key` の形式を確認する

```sql
-- 正規化形式の例:
-- PDFH-P101-A1B2C3D4E5F6G7H8I9J0K1L2-3A4B5C6D7E8F... (計4セクション)
-- × PDFH-P101-A1B2-C3D4-E5F6-... (4桁区切り表示形式はDBに入れない)

SELECT license_key FROM licenses WHERE id = 'ここにUUIDを貼る';
```

---

## 関連ファイル

| ファイル | 内容 |
|---------|------|
| `scripts/generate-license-key.ps1` | ライセンスキー生成スクリプト |
| `docs/supabase-setup/15_add_manual_payment_fields.sql` | 手動発行対応マイグレーション |
| `src/PdfHandler.Infrastructure/Helpers/LicenseKeyHelper.cs` | キー生成・検証ロジック（C#） |
| `evidence/` | 発行記録の保管フォルダ |

---

*作成: 2026-04-19*

-- マイグレーション: 手動発行（法人・銀行振込）対応フィールド追加
-- ファイル: docs/supabase-setup/15_add_manual_payment_fields.sql
-- Supabase SQL Editor で実行してください
--
-- 目的:
--   Stripe 決済以外（見積書・請求書・銀行振込による手動ライセンス発行）に対応するため
--   決済経路・法人情報・請求書番号・入金確認日時・管理メモ の各列を追加します。
--
-- 実行タイミング:
--   法人取引・手動ライセンス発行を開始する前に実行してください。
--   既存の Stripe 発行レコードには影響ありません（payment_type が 'stripe' になります）。
--
-- 続けて実行した場合の影響:
--   ADD COLUMN IF NOT EXISTS / CREATE INDEX IF NOT EXISTS を使用しているため
--   2回実行しても安全です。UPDATE 文は冪等です。


-- ============================================================
-- 1. 決済経路（stripe / manual）
-- ============================================================
ALTER TABLE licenses
  ADD COLUMN IF NOT EXISTS payment_type TEXT DEFAULT 'stripe'
    CHECK (payment_type IN ('stripe', 'manual'));


-- ============================================================
-- 2. 法人名・担当者名
-- ============================================================
ALTER TABLE licenses
  ADD COLUMN IF NOT EXISTS customer_company TEXT,    -- 法人名（個人取引は NULL）
  ADD COLUMN IF NOT EXISTS customer_contact TEXT;    -- 担当者名（個人取引は NULL）


-- ============================================================
-- 3. 請求書番号（外部の請求書ツールで採番した番号を記録）
--    例: INV-2026-001, 見積書-20260419-001
-- ============================================================
ALTER TABLE licenses
  ADD COLUMN IF NOT EXISTS invoice_number TEXT;


-- ============================================================
-- 4. 入金確認日時（銀行振込の確認が取れた日時）
-- ============================================================
ALTER TABLE licenses
  ADD COLUMN IF NOT EXISTS payment_confirmed_at TIMESTAMP WITH TIME ZONE;


-- ============================================================
-- 5. 管理メモ（内部用・顧客には非公開）
--    例: 「3台ライセンス、A社 購買部 〇〇様対応」
-- ============================================================
ALTER TABLE licenses
  ADD COLUMN IF NOT EXISTS notes TEXT;


-- ============================================================
-- 6. インデックス（検索・フィルタ用）
-- ============================================================
CREATE INDEX IF NOT EXISTS idx_licenses_payment_type
  ON licenses(payment_type);

-- 法人名での検索用（NULL行は除外）
CREATE INDEX IF NOT EXISTS idx_licenses_customer_company
  ON licenses(customer_company)
  WHERE customer_company IS NOT NULL;

-- 請求書番号での逆引き用（NULL行は除外）
CREATE INDEX IF NOT EXISTS idx_licenses_invoice_number
  ON licenses(invoice_number)
  WHERE invoice_number IS NOT NULL;


-- ============================================================
-- 7. 既存レコードの payment_type を 'stripe' で補完
--    DEFAULT 'stripe' は今後の INSERT にのみ適用されるため
--    既存行を明示的に更新する
-- ============================================================
UPDATE licenses
SET payment_type = 'stripe'
WHERE payment_type IS NULL;


-- ============================================================
-- 8. 確認クエリ（6 行が返れば成功）
-- ============================================================
SELECT
  column_name,
  data_type,
  column_default,
  is_nullable
FROM information_schema.columns
WHERE table_name = 'licenses'
  AND column_name IN (
    'payment_type',
    'customer_company',
    'customer_contact',
    'invoice_number',
    'payment_confirmed_at',
    'notes'
  )
ORDER BY column_name;

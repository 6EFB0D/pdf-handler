-- 18_add_transaction_id_to_licenses.sql
-- 目的:
--   backoffice の invoice_manager と license_manager を取引IDで紐づける。
--   admin-generate-license は transactionId を licenses.transaction_id に保存し、
--   admin-list-licenses は発行履歴に表示する。

ALTER TABLE public.licenses
  ADD COLUMN IF NOT EXISTS transaction_id TEXT;

CREATE INDEX IF NOT EXISTS idx_licenses_transaction_id
  ON public.licenses(transaction_id)
  WHERE transaction_id IS NOT NULL;

SELECT
  column_name,
  data_type,
  is_nullable
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'licenses'
  AND column_name = 'transaction_id';

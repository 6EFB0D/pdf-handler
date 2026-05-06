-- マイグレーション: stripe_checkout_session_id を追加（C-1 冪等性強化）
-- Supabase SQL Editor で実行してください
--
-- 目的:
--   - 銀行振込等 payment_intent が null になる決済手段でも二重発行を防ぐ
--   - stripe_payment_intent_id / stripe_checkout_session_id の両列に UNIQUE 制約を追加し
--     コード側の冪等性チェックを DB レベルで補強する
--
-- 実行タイミング:
--   stripe-webhook Edge Function のデプロイ前に必ず実行すること。
--   カラムが存在しない状態で新しい Webhook コードをデプロイすると
--   checkout.session.completed イベントの INSERT が全件エラーになります。

-- 1. カラム追加
ALTER TABLE licenses
  ADD COLUMN IF NOT EXISTS stripe_checkout_session_id TEXT;

-- 2. UNIQUE インデックス（NULL を除外: 旧レコードが NULL でも重複エラーにならない）
CREATE UNIQUE INDEX IF NOT EXISTS idx_licenses_stripe_checkout_session_id
  ON licenses(stripe_checkout_session_id)
  WHERE stripe_checkout_session_id IS NOT NULL;

-- 3. stripe_payment_intent_id にも同様の UNIQUE インデックスを追加（未設定の場合）
--    既に存在する場合は IF NOT EXISTS でスキップされます。
CREATE UNIQUE INDEX IF NOT EXISTS idx_licenses_stripe_payment_intent_id_unique
  ON licenses(stripe_payment_intent_id)
  WHERE stripe_payment_intent_id IS NOT NULL;

-- 4. 確認クエリ（期待: 2 行 表示）
SELECT
  indexname,
  indexdef
FROM pg_indexes
WHERE tablename = 'licenses'
  AND indexname IN (
    'idx_licenses_stripe_checkout_session_id',
    'idx_licenses_stripe_payment_intent_id_unique'
  );

-- =============================================================================
-- 99_verify_schema_state.sql
-- =============================================================================
-- 目的:
--   DEV / PROD どちらの Supabase プロジェクトで実行しても良い「スキーマ現況確認」スクリプト。
--   01〜15 のマイグレーションがどこまで適用されているかを 1 度で診断する。
--
-- 使い方:
--   1. Supabase Dashboard → SQL Editor で本ファイルを丸ごと貼り付けて Run
--   2. 各セクション末尾の「期待」コメントと結果を突き合わせる
--   3. 不足が見つかったセクションのファイル番号（例: 09／10／14／15／16）を Run する
--
-- 注意:
--   - 本ファイルは SELECT / 情報取得のみ。テーブル構造を変更しない（読み取り専用）。
--   - PROD で初回実行すると殆どのセクションで空（0 件）が返る。それは正常。
--     その状態を出発点に、01 → 02 → 08 → 09 → 10 → 11 → 12 → 13 → 14 → 15 → 16 を順に流す。
-- =============================================================================


-- ---------------------------------------------------------------------------
-- セクション 1: 主要テーブルが存在するか
-- ---------------------------------------------------------------------------
-- 期待（DEV）: licenses / license_activations の 2 行  ／  subscriptions は NULL
-- 期待（PROD 初回）: 全て NULL  → 01_database-schema.sql を流す合図
SELECT
  to_regclass('public.licenses')             AS licenses_table,
  to_regclass('public.license_activations')  AS license_activations_table,
  to_regclass('public.subscriptions')        AS subscriptions_should_be_null;


-- ---------------------------------------------------------------------------
-- セクション 2: licenses の主要カラム在庫（ファイル別の網羅状況）
-- ---------------------------------------------------------------------------
-- 期待（DEV / 全 16 適用済み）: 17 行（下記すべて）
-- カラム → 由来ファイル の対応:
--   license_key / plan / user_email / stripe_customer_id /
--   stripe_payment_intent_id / activation_count → 01 + 08
--   purchased_version                           → 09
--   app_id                                      → 10
--   stripe_checkout_session_id                  → 14
--   payment_type / customer_company /
--   customer_contact / invoice_number /
--   payment_confirmed_at / notes                → 15
--   revoked_at / revoked_reason                 → 16
SELECT
  c.column_name,
  c.data_type,
  c.column_default,
  c.is_nullable
FROM information_schema.columns AS c
WHERE c.table_schema = 'public'
  AND c.table_name   = 'licenses'
  AND c.column_name IN (
    'license_key',
    'plan',
    'user_email',
    'stripe_customer_id',
    'stripe_payment_intent_id',
    'stripe_checkout_session_id',
    'activation_count',
    'purchased_version',
    'app_id',
    'payment_type',
    'customer_company',
    'customer_contact',
    'invoice_number',
    'payment_confirmed_at',
    'notes',
    'revoked_at',
    'revoked_reason'
  )
ORDER BY c.column_name;


-- ---------------------------------------------------------------------------
-- セクション 3: license_activations の主要カラム
-- ---------------------------------------------------------------------------
-- 期待: device_name / display_name の 2 行
SELECT
  c.column_name,
  c.data_type
FROM information_schema.columns AS c
WHERE c.table_schema = 'public'
  AND c.table_name   = 'license_activations'
  AND c.column_name IN ('device_name', 'display_name')
ORDER BY c.column_name;


-- ---------------------------------------------------------------------------
-- セクション 4: 一意制約・インデックスの存在
-- ---------------------------------------------------------------------------
-- 期待（DEV）:
--   idx_licenses_stripe_checkout_session_id          (14 由来)
--   idx_licenses_stripe_payment_intent_id_unique     (14 由来)
--   idx_licenses_payment_type                        (15 由来)
--   idx_licenses_customer_company                    (15 由来)
--   idx_licenses_invoice_number                      (15 由来)
--   idx_licenses_revoked_at                          (16 由来)
SELECT
  indexname,
  indexdef
FROM pg_indexes
WHERE schemaname = 'public'
  AND tablename  = 'licenses'
  AND indexname IN (
    'idx_licenses_stripe_checkout_session_id',
    'idx_licenses_stripe_payment_intent_id_unique',
    'idx_licenses_payment_type',
    'idx_licenses_customer_company',
    'idx_licenses_invoice_number',
    'idx_licenses_revoked_at'
  )
ORDER BY indexname;


-- ---------------------------------------------------------------------------
-- セクション 5: licenses.plan CHECK 制約（買い切りのみ）
-- ---------------------------------------------------------------------------
-- 期待: definition に plan = 'purchased' が含まれる
SELECT con.conname,
       pg_get_constraintdef(con.oid) AS definition
FROM pg_constraint AS con
JOIN pg_class AS rel       ON rel.oid = con.conrelid
JOIN pg_namespace AS nsp   ON nsp.oid = rel.relnamespace
WHERE nsp.nspname = 'public'
  AND rel.relname = 'licenses'
  AND con.contype = 'c'
  AND con.conname = 'licenses_plan_check';


-- ---------------------------------------------------------------------------
-- セクション 6: payment_type CHECK 制約（stripe / manual）
-- ---------------------------------------------------------------------------
-- 期待（15 適用済み）: definition に payment_type IN ('stripe', 'manual') が含まれる
SELECT con.conname,
       pg_get_constraintdef(con.oid) AS definition
FROM pg_constraint AS con
JOIN pg_class AS rel       ON rel.oid = con.conrelid
JOIN pg_namespace AS nsp   ON nsp.oid = rel.relnamespace
WHERE nsp.nspname = 'public'
  AND rel.relname = 'licenses'
  AND con.contype = 'c'
  AND pg_get_constraintdef(con.oid) ILIKE '%payment_type%';


-- ---------------------------------------------------------------------------
-- セクション 7: RLS（Row Level Security）が有効か
-- ---------------------------------------------------------------------------
-- 期待（DEV / 02 適用済み）: licenses / license_activations の relrowsecurity が true
SELECT
  rel.relname AS table_name,
  rel.relrowsecurity AS rls_enabled
FROM pg_class AS rel
JOIN pg_namespace AS nsp ON nsp.oid = rel.relnamespace
WHERE nsp.nspname = 'public'
  AND rel.relname IN ('licenses', 'license_activations')
ORDER BY rel.relname;


-- ---------------------------------------------------------------------------
-- セクション 8: トリガー（updated_at / check_device_limit）
-- ---------------------------------------------------------------------------
-- 期待（DEV）: licenses_updated_at / license_activations_updated_at /
--              check_device_limit_trigger（または同等名）の各行
SELECT
  trigger_name,
  event_object_table,
  action_timing,
  event_manipulation
FROM information_schema.triggers
WHERE event_object_schema = 'public'
  AND event_object_table  IN ('licenses', 'license_activations')
ORDER BY event_object_table, trigger_name;


-- ---------------------------------------------------------------------------
-- セクション 9: 最終診断（適用済みファイル数の概算）
-- ---------------------------------------------------------------------------
-- 各カラムの存在を bool で集計し、適用済みマイグレーションを推定
WITH cols AS (
  SELECT column_name
  FROM information_schema.columns
  WHERE table_schema = 'public' AND table_name = 'licenses'
)
SELECT
  EXISTS (SELECT 1 FROM cols WHERE column_name = 'license_key')                  AS file_01_database_schema,
  EXISTS (SELECT 1 FROM cols WHERE column_name = 'activation_count')             AS file_08_activation_count,
  EXISTS (SELECT 1 FROM cols WHERE column_name = 'purchased_version')            AS file_09_purchased_version,
  EXISTS (SELECT 1 FROM cols WHERE column_name = 'app_id')                       AS file_10_app_id,
  to_regclass('public.subscriptions') IS NULL                                    AS file_12_subscription_removed,
  EXISTS (SELECT 1 FROM cols WHERE column_name = 'stripe_checkout_session_id')   AS file_14_checkout_session_id,
  EXISTS (SELECT 1 FROM cols WHERE column_name = 'payment_type')                 AS file_15_manual_payment_fields,
  EXISTS (SELECT 1 FROM cols WHERE column_name = 'revoked_at')                   AS file_16_revoked_columns;
-- 期待（DEV / 全適用）: 全列 true
-- 期待（PROD 初回）   : 全列 false

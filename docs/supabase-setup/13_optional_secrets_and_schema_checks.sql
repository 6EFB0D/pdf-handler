-- =============================================================================
-- 13_optional_secrets_and_schema_checks.sql
-- =============================================================================
-- 「足りないもの」のうち、メール送信用の API キー等は Supabase の
-- **Edge Functions の Secrets** で設定します。SQL Editor では作成・保存できません。
--
-- 【Dashboard】
--   Project Settings → Edge Functions → Secrets
--   - RESEND_API_KEY        … Resend の API キー（re_ で始まる）
--   - LICENSE_EMAIL_FROM    … 送信元。例: PDFハンドラ <onboarding@resend.dev>
--                             自ドメインでは Resend でドメイン検証後に変更
--
-- 【CLI の例】（プロジェクトルートで、<...> を置き換え）
--   npx supabase secrets set RESEND_API_KEY=<re_...> --project-ref yzmjuotvkxcfnsgleyxl
--   npx supabase secrets set LICENSE_EMAIL_FROM="PDFハンドラ <onboarding@resend.dev>" --project-ref yzmjuotvkxcfnsgleyxl
--
-- 【任意の Secret】（Webhook が買い切りキーのメジャーバージョンに使う。未設定なら Edge 側は 1）
--   npx supabase secrets set LICENSE_PURCHASED_MAJOR_VERSION=1 --project-ref yzmjuotvkxcfnsgleyxl
--
-- 既に Dashboard にある STRIPE_* / LICENSE_SECRET_KEY / APP_URL 等はそのまま利用でよいです。
-- 不要になった STRIPE_PRICE_ID_SUBSCRIPTION_* や ENABLE_PREMIUM_PLAN は Dashboard から削除推奨。
-- =============================================================================


-- ---------------------------------------------------------------------------
-- A. DB 側: verify-license が device_name を書き込むためのカラム（無ければ追加）
--     01_database-schema.sql のみの古い DB で不足しがちです。
-- ---------------------------------------------------------------------------
ALTER TABLE license_activations
  ADD COLUMN IF NOT EXISTS device_name TEXT;


-- ---------------------------------------------------------------------------
-- B. 確認用: public.licenses に想定カラムがあるか（足りない行があれば 09 / 10 / 08 を実行）
-- ---------------------------------------------------------------------------
SELECT
  c.column_name,
  c.data_type
FROM information_schema.columns AS c
WHERE c.table_schema = 'public'
  AND c.table_name = 'licenses'
  AND c.column_name IN (
    'license_key',
    'plan',
    'user_email',
    'stripe_customer_id',
    'stripe_payment_intent_id',
    'purchased_version',
    'app_id',
    'activation_count',
    'is_active'
  )
ORDER BY c.column_name;

-- 期待: 上記 9 つがすべて 1 行ずつ表示されること。
-- 欠ける場合:
--   purchased_version → 09_add-purchased-version.sql
--   app_id           → 10_add_app_id.sql
--   activation_count → 08_add-activation-count.sql


-- ---------------------------------------------------------------------------
-- C. 確認用: subscriptions テーブルが無いこと（買い切りのみのスキーマ）
-- ---------------------------------------------------------------------------
SELECT to_regclass('public.subscriptions') AS subscriptions_should_be_null;
-- 期待: subscriptions_should_be_null が NULL


-- ---------------------------------------------------------------------------
-- D. 確認用: licenses.plan の CHECK（purchased のみ）
-- ---------------------------------------------------------------------------
SELECT con.conname,
       pg_get_constraintdef(con.oid) AS definition
FROM pg_constraint AS con
JOIN pg_class AS rel ON rel.oid = con.conrelid
JOIN pg_namespace AS nsp ON nsp.oid = rel.relnamespace
WHERE nsp.nspname = 'public'
  AND rel.relname = 'licenses'
  AND con.contype = 'c'
  AND con.conname = 'licenses_plan_check';
-- 期待: definition に plan = 'purchased' が含まれる

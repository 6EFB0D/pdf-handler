-- 買い切りのみへ移行: subscriptions テーブルとサブスク用カラムを削除
-- 既存DBで 01 の旧版を実行済みの場合に Supabase SQL Editor で実行
-- plan が purchased 以外の行があると CHECK 追加が失敗します（該当行を削除または更新）

DROP TABLE IF EXISTS subscriptions CASCADE;

DROP INDEX IF EXISTS idx_licenses_stripe_subscription_id;

ALTER TABLE licenses DROP CONSTRAINT IF EXISTS licenses_plan_check;
ALTER TABLE licenses ADD CONSTRAINT licenses_plan_check CHECK (plan = 'purchased');

ALTER TABLE licenses DROP COLUMN IF EXISTS stripe_subscription_id;
ALTER TABLE licenses DROP COLUMN IF EXISTS subscription_renewal_date;

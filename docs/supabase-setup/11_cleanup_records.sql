-- Phase 5: Supabase レコードクリーンアップ
-- Supabase SQL Editor で実行してください
--
-- ※ 実行前にバックアップを取得してください。
--    - Pro/Team/Enterprise: Dashboard → Database → Backups で手動バックアップ
--    - Free プラン: ダッシュボードのバックアップは利用不可。
--      CLI でエクスポート: supabase db dump -f backup.sql
--
-- ※ 本番で顧客データがある場合は絶対に実行しないでください。
--    テスト・開発環境（Stripe サンドボックスのみ）向けです。

-- =============================================================================
-- オプションA: 全件削除（開発・テスト環境向け）
-- license_activations → licenses（subscriptions テーブルは 12_remove_subscription_model で廃止）
-- =============================================================================

TRUNCATE license_activations CASCADE;
TRUNCATE licenses CASCADE;

-- =============================================================================
-- オプションB: テスト用メールのレコードのみ削除（本番で一部削除する場合）
-- 下記をコメントアウトして実行するか、条件を編集してください
-- =============================================================================
/*
DELETE FROM licenses 
WHERE user_email ILIKE '%test%' 
   OR user_email ILIKE '%example%'
   OR user_email ILIKE '%@example.com'
   OR user_email = 'your-test-email@example.com';
*/

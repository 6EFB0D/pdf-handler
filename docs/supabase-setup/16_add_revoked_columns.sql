-- マイグレーション: ライセンス取消（soft revoke）対応カラム追加
-- ファイル: docs/supabase-setup/16_add_revoked_columns.sql
-- Supabase SQL Editor で実行してください
--
-- 目的:
--   GUI ツール（License generator）の「取消ボタン」によるロールバックに対応するため
--   licenses テーブルに revoked_at / revoked_reason の 2 カラムを追加する。
--
-- 設計思想:
--   - 物理削除ではなく soft revoke を既定運用とし、is_active=false + revoked_at で履歴を保持
--   - 監査・税務上の証跡を確保するため、誤発行であっても DB から消さない
--   - hard delete は license_activations が無い場合のみ admin-delete-license で実行
--
-- 実行タイミング:
--   admin-revoke-license Edge Function のデプロイ前に必ず実行すること。
--   カラムが存在しない状態で revoke エンドポイントを叩くと UPDATE 文がエラーになる。
--
-- 続けて実行した場合の影響:
--   ADD COLUMN IF NOT EXISTS / CREATE INDEX IF NOT EXISTS を使用しているため
--   2 回実行しても安全。


-- ============================================================
-- 1. 取消日時（NULL = 有効 / NOT NULL = 取消済み）
-- ============================================================
ALTER TABLE licenses
  ADD COLUMN IF NOT EXISTS revoked_at TIMESTAMP WITH TIME ZONE;


-- ============================================================
-- 2. 取消理由（自由記述・必須運用）
--    例:
--      "宛先メール誤り: yamada@example.co.jp → yamada@correct.co.jp"
--      "顧客都合キャンセル / 発注書差戻し"
--      "テスト発行のクリーンアップ"
-- ============================================================
ALTER TABLE licenses
  ADD COLUMN IF NOT EXISTS revoked_reason TEXT;


-- ============================================================
-- 3. インデックス（取消済みのみ抽出する用途）
-- ============================================================
CREATE INDEX IF NOT EXISTS idx_licenses_revoked_at
  ON licenses(revoked_at)
  WHERE revoked_at IS NOT NULL;


-- ============================================================
-- 4. 整合性: 取消済みなら is_active=false であるべき
--    既存レコードの整合チェックのみ（自動補正はしない）
-- ============================================================
SELECT
  id,
  license_key,
  is_active,
  revoked_at,
  revoked_reason
FROM licenses
WHERE revoked_at IS NOT NULL
  AND is_active = true;
-- 期待: 0 行
-- もし 1 行以上返る場合は、運用上の不整合。GUI の取消処理を見直すこと。


-- ============================================================
-- 5. 確認クエリ（2 行が返れば成功）
-- ============================================================
SELECT
  column_name,
  data_type,
  column_default,
  is_nullable
FROM information_schema.columns
WHERE table_name = 'licenses'
  AND column_name IN ('revoked_at', 'revoked_reason')
ORDER BY column_name;

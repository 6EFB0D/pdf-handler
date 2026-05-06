-- マイグレーション: app_id を追加（複数アプリ統合ライセンス管理用）
-- Supabase SQL Editor で実行してください
-- 参照: docs/specs/multi_app_license_unified_supabase.md

ALTER TABLE licenses 
ADD COLUMN IF NOT EXISTS app_id TEXT NOT NULL DEFAULT 'PDFH';

-- 既存レコードの app_id を PDFH で統一
UPDATE licenses SET app_id = 'PDFH' WHERE app_id IS NULL OR app_id = '';

-- インデックス（app_id でフィルタするクエリ用、任意）
CREATE INDEX IF NOT EXISTS idx_licenses_app_id ON licenses(app_id);

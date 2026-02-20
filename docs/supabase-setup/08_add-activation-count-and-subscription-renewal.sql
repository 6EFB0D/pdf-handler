-- マイグレーション: activation_count と subscription_renewal_date を追加
-- Supabase SQL Editorで実行してください
-- 既存のテーブルにカラムを追加するため、テーブルが既に存在する場合はこのスクリプトを実行してください

-- 1. licensesテーブルに activation_count カラムを追加
ALTER TABLE licenses 
ADD COLUMN IF NOT EXISTS activation_count INTEGER DEFAULT 0;

-- 2. licensesテーブルに subscription_renewal_date カラムを追加（サブスクリプション更新日用）
ALTER TABLE licenses 
ADD COLUMN IF NOT EXISTS subscription_renewal_date TIMESTAMP WITH TIME ZONE;

-- 3. 既存データの activation_count を license_activations の件数で更新
UPDATE licenses l
SET activation_count = (
  SELECT COUNT(*)::INTEGER 
  FROM license_activations la 
  WHERE la.license_id = l.id AND la.is_active = true
)
WHERE EXISTS (
  SELECT 1 FROM license_activations la WHERE la.license_id = l.id
);

-- 変更がない行も0に更新
UPDATE licenses 
SET activation_count = 0 
WHERE activation_count IS NULL;

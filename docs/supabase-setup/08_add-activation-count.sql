-- マイグレーション: activation_count の追加・同期（レガシーDB向け）
-- 新規は 01_database-schema.sql に含まれます

ALTER TABLE licenses
ADD COLUMN IF NOT EXISTS activation_count INTEGER DEFAULT 0;

UPDATE licenses l
SET activation_count = (
  SELECT COUNT(*)::INTEGER
  FROM license_activations la
  WHERE la.license_id = l.id AND la.is_active = true
)
WHERE EXISTS (
  SELECT 1 FROM license_activations la WHERE la.license_id = l.id
);

UPDATE licenses
SET activation_count = 0
WHERE activation_count IS NULL;

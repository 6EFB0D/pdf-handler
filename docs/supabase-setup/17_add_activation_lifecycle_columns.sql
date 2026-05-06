-- 17_add_activation_lifecycle_columns.sql
-- 目的:
--   端末解除後に同じ端末を再アクティベーションできるよう、
--   license_activations の解除/再有効化時刻を保持する。
--
-- 背景:
--   license_activations は UNIQUE(license_id, hardware_id) を持つため、
--   解除済み行を残したまま同じ端末を再登録すると INSERT が一意制約で失敗する。
--   verify-license は解除済み行を UPDATE で再有効化するため、以下の履歴列を追加する。

ALTER TABLE public.license_activations
  ADD COLUMN IF NOT EXISTS deactivated_at TIMESTAMP WITH TIME ZONE;

ALTER TABLE public.license_activations
  ADD COLUMN IF NOT EXISTS reactivated_at TIMESTAMP WITH TIME ZONE;

CREATE INDEX IF NOT EXISTS idx_license_activations_deactivated_at
  ON public.license_activations(deactivated_at)
  WHERE deactivated_at IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_license_activations_reactivated_at
  ON public.license_activations(reactivated_at)
  WHERE reactivated_at IS NOT NULL;

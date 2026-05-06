-- Row Level Security (RLS)
-- Supabase SQL Editorで実行してください

ALTER TABLE licenses ENABLE ROW LEVEL SECURITY;
ALTER TABLE license_activations ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Service role can access all licenses"
    ON licenses FOR ALL
    USING (auth.role() = 'service_role');

CREATE POLICY "Anonymous can verify license by key"
    ON licenses FOR SELECT
    USING (true);

CREATE POLICY "Service role can access all activations"
    ON license_activations FOR ALL
    USING (auth.role() = 'service_role');

CREATE POLICY "Anonymous can verify activation by hardware_id"
    ON license_activations FOR SELECT
    USING (true);

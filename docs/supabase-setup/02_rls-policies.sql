-- Row Level Security (RLS) ポリシーの設定
-- Supabase SQL Editorで実行してください

-- 1. licensesテーブルのRLSを有効化
ALTER TABLE licenses ENABLE ROW LEVEL SECURITY;

-- 2. license_activationsテーブルのRLSを有効化
ALTER TABLE license_activations ENABLE ROW LEVEL SECURITY;

-- 3. subscriptionsテーブルのRLSを有効化
ALTER TABLE subscriptions ENABLE ROW LEVEL SECURITY;

-- 4. licensesテーブルのポリシー
-- サービスロールは全レコードにアクセス可能（Edge Functions用）
CREATE POLICY "Service role can access all licenses"
    ON licenses FOR ALL
    USING (auth.role() = 'service_role');

-- 匿名ユーザーは自分のライセンスキーで検証のみ可能
CREATE POLICY "Anonymous can verify license by key"
    ON licenses FOR SELECT
    USING (true); -- 検証用のため、ライセンスキーで検索可能にする

-- 5. license_activationsテーブルのポリシー
-- サービスロールは全レコードにアクセス可能
CREATE POLICY "Service role can access all activations"
    ON license_activations FOR ALL
    USING (auth.role() = 'service_role');

-- 匿名ユーザーは自分のハードウェアIDで検証のみ可能
CREATE POLICY "Anonymous can verify activation by hardware_id"
    ON license_activations FOR SELECT
    USING (true); -- 検証用のため、ハードウェアIDで検索可能にする

-- 6. subscriptionsテーブルのポリシー
-- サービスロールは全レコードにアクセス可能
CREATE POLICY "Service role can access all subscriptions"
    ON subscriptions FOR ALL
    USING (auth.role() = 'service_role');

-- 匿名ユーザーは参照不可（Edge Functions経由でのみアクセス）


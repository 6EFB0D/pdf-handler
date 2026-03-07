-- マイグレーション: purchased_version を追加
-- Supabase SQL Editorで実行してください
-- 買い切りライセンスの利用可能メジャーバージョン（初回アクティベーション時に設定）

ALTER TABLE licenses 
ADD COLUMN IF NOT EXISTS purchased_version TEXT;

-- 既存の買い切りライセンスは NULL のまま（初回アクティベーション時に設定）
-- 新形式のライセンスキー（PDFH-P101-xxx）からは形態コードから取得可能

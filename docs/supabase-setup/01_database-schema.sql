-- PDFハンドラ ライセンス管理データベーススキーマ（買い切りのみ）
-- Supabase SQL Editorで実行してください
-- 続けて 09_add-purchased-version.sql → 10_add_app_id.sql を実行してください

-- 1. licensesテーブル
CREATE TABLE IF NOT EXISTS licenses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    license_key TEXT UNIQUE NOT NULL,
    hardware_id TEXT,
    plan TEXT NOT NULL DEFAULT 'purchased' CHECK (plan = 'purchased'),
    user_email TEXT NOT NULL,
    stripe_customer_id TEXT,
    stripe_payment_intent_id TEXT,
    activation_date TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    expiration_date TIMESTAMP WITH TIME ZONE,
    last_verification_date TIMESTAMP WITH TIME ZONE,
    is_active BOOLEAN DEFAULT true,
    activation_count INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_licenses_license_key ON licenses(license_key);
CREATE INDEX IF NOT EXISTS idx_licenses_hardware_id ON licenses(hardware_id);
CREATE INDEX IF NOT EXISTS idx_licenses_stripe_customer_id ON licenses(stripe_customer_id);
CREATE INDEX IF NOT EXISTS idx_licenses_is_active ON licenses(is_active);

-- 2. license_activations（1ライセンスにつき最大3デバイス）
CREATE TABLE IF NOT EXISTS license_activations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    license_id UUID NOT NULL REFERENCES licenses(id) ON DELETE CASCADE,
    hardware_id TEXT NOT NULL,
    activation_date TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    last_verification_date TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    is_active BOOLEAN DEFAULT true,
    UNIQUE(license_id, hardware_id)
);

CREATE INDEX IF NOT EXISTS idx_license_activations_license_id ON license_activations(license_id);
CREATE INDEX IF NOT EXISTS idx_license_activations_hardware_id ON license_activations(hardware_id);
CREATE INDEX IF NOT EXISTS idx_license_activations_is_active ON license_activations(is_active);

-- 3. updated_at トリガー
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER update_licenses_updated_at BEFORE UPDATE ON licenses
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- 4. デバイス数制限（最大1台）
CREATE OR REPLACE FUNCTION check_device_limit()
RETURNS TRIGGER AS $$
DECLARE
    device_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO device_count
    FROM license_activations
    WHERE license_id = NEW.license_id AND is_active = true;

    IF device_count >= 1 THEN
        RAISE EXCEPTION 'ライセンスは最大1デバイスまでアクティベーション可能です';
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER check_device_limit_trigger BEFORE INSERT ON license_activations
    FOR EACH ROW EXECUTE FUNCTION check_device_limit();

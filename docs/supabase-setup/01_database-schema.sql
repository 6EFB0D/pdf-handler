-- PDFハンドラ ライセンス管理データベーススキーマ
-- Supabase SQL Editorで実行してください

-- 1. licensesテーブルの作成
CREATE TABLE IF NOT EXISTS licenses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    license_key TEXT UNIQUE NOT NULL,
    hardware_id TEXT,
    plan TEXT NOT NULL CHECK (plan IN ('purchased', 'subscription_standard', 'subscription_premium')),
    user_email TEXT NOT NULL,
    stripe_customer_id TEXT,
    stripe_subscription_id TEXT,
    stripe_payment_intent_id TEXT,
    activation_date TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    expiration_date TIMESTAMP WITH TIME ZONE,
    subscription_renewal_date TIMESTAMP WITH TIME ZONE,
    last_verification_date TIMESTAMP WITH TIME ZONE,
    is_active BOOLEAN DEFAULT true,
    activation_count INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- インデックスの作成
CREATE INDEX IF NOT EXISTS idx_licenses_license_key ON licenses(license_key);
CREATE INDEX IF NOT EXISTS idx_licenses_hardware_id ON licenses(hardware_id);
CREATE INDEX IF NOT EXISTS idx_licenses_stripe_customer_id ON licenses(stripe_customer_id);
CREATE INDEX IF NOT EXISTS idx_licenses_stripe_subscription_id ON licenses(stripe_subscription_id);
CREATE INDEX IF NOT EXISTS idx_licenses_is_active ON licenses(is_active);

-- 2. license_activationsテーブルの作成（1ライセンスにつき最大3デバイスまで）
CREATE TABLE IF NOT EXISTS license_activations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    license_id UUID NOT NULL REFERENCES licenses(id) ON DELETE CASCADE,
    hardware_id TEXT NOT NULL,
    activation_date TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    last_verification_date TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    is_active BOOLEAN DEFAULT true,
    UNIQUE(license_id, hardware_id)
);

-- インデックスの作成
CREATE INDEX IF NOT EXISTS idx_license_activations_license_id ON license_activations(license_id);
CREATE INDEX IF NOT EXISTS idx_license_activations_hardware_id ON license_activations(hardware_id);
CREATE INDEX IF NOT EXISTS idx_license_activations_is_active ON license_activations(is_active);

-- 3. subscriptionsテーブルの作成
CREATE TABLE IF NOT EXISTS subscriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    license_id UUID NOT NULL REFERENCES licenses(id) ON DELETE CASCADE,
    stripe_subscription_id TEXT UNIQUE NOT NULL,
    status TEXT NOT NULL CHECK (status IN ('active', 'canceled', 'past_due', 'unpaid')),
    current_period_start TIMESTAMP WITH TIME ZONE NOT NULL,
    current_period_end TIMESTAMP WITH TIME ZONE NOT NULL,
    cancel_at_period_end BOOLEAN DEFAULT false,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- インデックスの作成
CREATE INDEX IF NOT EXISTS idx_subscriptions_license_id ON subscriptions(license_id);
CREATE INDEX IF NOT EXISTS idx_subscriptions_stripe_subscription_id ON subscriptions(stripe_subscription_id);
CREATE INDEX IF NOT EXISTS idx_subscriptions_status ON subscriptions(status);

-- 4. updated_atを自動更新するトリガー関数
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- トリガーの作成
CREATE TRIGGER update_licenses_updated_at BEFORE UPDATE ON licenses
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_subscriptions_updated_at BEFORE UPDATE ON subscriptions
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- 5. ライセンスキー生成関数（UUID v4ベース）
CREATE OR REPLACE FUNCTION generate_license_key()
RETURNS TEXT AS $$
BEGIN
    RETURN 'PDFH-' || UPPER(REPLACE(gen_random_uuid()::TEXT, '-', ''));
END;
$$ LANGUAGE plpgsql;

-- 6. デバイス数制限チェック関数（1ライセンスにつき最大3デバイス）
CREATE OR REPLACE FUNCTION check_device_limit()
RETURNS TRIGGER AS $$
DECLARE
    device_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO device_count
    FROM license_activations
    WHERE license_id = NEW.license_id AND is_active = true;
    
    IF device_count >= 3 THEN
        RAISE EXCEPTION 'ライセンスは最大3デバイスまでアクティベーション可能です';
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- トリガーの作成
CREATE TRIGGER check_device_limit_trigger BEFORE INSERT ON license_activations
    FOR EACH ROW EXECUTE FUNCTION check_device_limit();




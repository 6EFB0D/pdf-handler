#!/bin/bash
# Supabase Edge Functions デプロイスクリプト
# 使用方法: ./scripts/deploy-supabase-functions.sh

# プロジェクトリファレンス
PROJECT_REF="yzmjuotvkxcfnsgleyxl"

echo "=========================================="
echo "Supabase Edge Functions デプロイ開始"
echo "=========================================="
echo ""

# プロジェクトルートに移動
cd "$(dirname "$0")/.." || exit

# 1. create-checkout-session のデプロイ（JWT検証無効: 401エラー対策）
echo "1/3: create-checkout-session をデプロイ中..."
supabase functions deploy create-checkout-session --project-ref "$PROJECT_REF" --no-verify-jwt
if [ $? -eq 0 ]; then
    echo "✅ create-checkout-session デプロイ成功"
else
    echo "❌ create-checkout-session デプロイ失敗"
    exit 1
fi
echo ""

# 2. verify-license のデプロイ（JWT検証無効: 401エラー対策）
echo "2/3: verify-license をデプロイ中..."
supabase functions deploy verify-license --project-ref "$PROJECT_REF" --no-verify-jwt
if [ $? -eq 0 ]; then
    echo "✅ verify-license デプロイ成功"
else
    echo "❌ verify-license デプロイ失敗"
    exit 1
fi
echo ""

# 3. stripe-webhook のデプロイ（JWT認証無効: 401エラー対策）
echo "3/3: stripe-webhook をデプロイ中..."
supabase functions deploy stripe-webhook --project-ref "$PROJECT_REF" --no-verify-jwt
if [ $? -eq 0 ]; then
    echo "✅ stripe-webhook デプロイ成功"
else
    echo "❌ stripe-webhook デプロイ失敗"
    exit 1
fi
echo ""

echo "=========================================="
echo "✅ すべての Edge Functions のデプロイが完了しました"
echo "=========================================="
echo ""
echo "次のステップ："
echo "1. Supabaseダッシュボードで Edge Functions が正しくデプロイされているか確認"
echo "2. アプリケーションを起動して動作確認"
echo "3. Stripe Webhookのテストイベントを送信して確認"
echo ""

# Supabase Edge Functions デプロイスクリプト (PowerShell版)
# 使用方法: .\scripts\deploy-supabase-functions.ps1

# プロジェクトリファレンス
$PROJECT_REF = "yzmjuotvkxcfnsgleyxl"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Supabase Edge Functions デプロイ開始" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# プロジェクトルートに移動
Set-Location $PSScriptRoot\..

# 1. create-checkout-session のデプロイ（JWT検証無効: 401エラー対策）
Write-Host "1/3: create-checkout-session をデプロイ中..." -ForegroundColor Yellow
npx supabase functions deploy create-checkout-session --project-ref $PROJECT_REF --no-verify-jwt
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ create-checkout-session デプロイ成功" -ForegroundColor Green
} else {
    Write-Host "❌ create-checkout-session デプロイ失敗" -ForegroundColor Red
    exit 1
}
Write-Host ""

# 2. verify-license のデプロイ（JWT検証無効: 401エラー対策）
Write-Host "2/3: verify-license をデプロイ中..." -ForegroundColor Yellow
npx supabase functions deploy verify-license --project-ref $PROJECT_REF --no-verify-jwt
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ verify-license デプロイ成功" -ForegroundColor Green
} else {
    Write-Host "❌ verify-license デプロイ失敗" -ForegroundColor Red
    exit 1
}
Write-Host ""

# 3. stripe-webhook のデプロイ（JWT認証無効: 401エラー対策）
Write-Host "3/3: stripe-webhook をデプロイ中..." -ForegroundColor Yellow
npx supabase functions deploy stripe-webhook --project-ref $PROJECT_REF --no-verify-jwt
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ stripe-webhook デプロイ成功" -ForegroundColor Green
} else {
    Write-Host "❌ stripe-webhook デプロイ失敗" -ForegroundColor Red
    exit 1
}
Write-Host ""

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "✅ すべての Edge Functions のデプロイが完了しました" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "次のステップ：" -ForegroundColor Yellow
Write-Host "1. Supabaseダッシュボードで Edge Functions が正しくデプロイされているか確認"
Write-Host "2. アプリケーションを起動して動作確認"
Write-Host "3. Stripe Webhookのテストイベントを送信して確認"
Write-Host ""

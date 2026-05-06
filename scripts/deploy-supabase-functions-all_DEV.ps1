# Deploy all 11 Supabase Edge Functions (DEV or PROD).
# Usage:
#   1. Set $PROJECT_REF below (Dashboard -> Project Settings -> General -> Reference ID)
#   2. From repo root:  .\scripts\deploy-supabase-functions-all.ps1
# --no-verify-jwt : required (license key / ADMIN_API_KEY / Stripe verify in-function)
# See: docs/supabase-setup/MIGRATION_DEV_TO_PROD.md section 3-A
#
# If you see parser errors on Japanese paths, save this file as UTF-8 with BOM in your editor.

#region User editable
$PROJECT_REF = "yzmjuotvkxcfnsgleyxl"
#endregion

if (-not $PROJECT_REF -or $PROJECT_REF.Trim().Length -eq 0) {
    Write-Host "ERROR: Set `$PROJECT_REF in scripts/deploy-supabase-functions-all.ps1" -ForegroundColor Red
    exit 1
}

$Functions = @(
    "create-checkout-session"
    "request-checkout"
    "stripe-webhook"
    "verify-license"
    "get-activations"
    "deactivate-device"
    "update-device-display-name"
    "ping"
    "admin-generate-license"
    "admin-list-licenses"
    "admin-deactivate-license"
)

$Root = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $Root

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Deploying $($Functions.Count) Supabase Edge Functions" -ForegroundColor Cyan
Write-Host "project-ref: $PROJECT_REF" -ForegroundColor Cyan
Write-Host "cwd: $Root" -ForegroundColor DarkGray
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

$i = 0
foreach ($name in $Functions) {
    $i++
    Write-Host "[$i/$($Functions.Count)] deploy $name ..." -ForegroundColor Yellow

    npx supabase functions deploy $name --project-ref $PROJECT_REF --no-verify-jwt

    if ($LASTEXITCODE -ne 0) {
        Write-Host "FAILED: $name (exit $LASTEXITCODE)" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Write-Host "OK: $name" -ForegroundColor Green
    Write-Host ""
}

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Done. All $($Functions.Count) functions deployed." -ForegroundColor Green
Write-Host "Next: check Dashboard -> Edge Functions; Stripe webhook URLs for this project." -ForegroundColor DarkGray
Write-Host "==========================================" -ForegroundColor Cyan

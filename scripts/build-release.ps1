# Build a customer-facing Release package.
# The PROD anon key is public in Supabase clients, but keep it out of git.
# Pass -SupabaseAnonKey or set PDFHANDLER_PROD_SUPABASE_ANON_KEY before running.

param(
    [string]$SupabaseAnonKey = $env:PDFHANDLER_PROD_SUPABASE_ANON_KEY,
    [string]$ContactUrl = $env:CONTACT_URL,
    [string]$ProductPageUrl = $env:PRODUCT_PAGE_URL,
    [string]$SurveyFormUrl = $env:SURVEY_FORM_URL,
    [switch]$NoKill
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$uiProject = Join-Path $projectRoot "src\PdfHandler.UI\PdfHandler.UI.csproj"
$prodUrl = "https://kmrzktsykjibslajpecu.supabase.co"

if ([string]::IsNullOrWhiteSpace($SupabaseAnonKey)) {
    throw "PROD Supabase anon key is required. Set PDFHANDLER_PROD_SUPABASE_ANON_KEY or pass -SupabaseAnonKey."
}

if (-not $NoKill) {
    Get-Process -Name "PdfHandler.UI" -ErrorAction SilentlyContinue |
        Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

[xml]$projectXml = Get-Content $uiProject
$version = $projectXml.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($version)) {
    $version = "0.0.0"
}

$publishDir = Join-Path $projectRoot "artifacts\release\PdfHandler-$version-win-x64"
if (Test-Path $publishDir) {
    Remove-Item -Recurse -Force $publishDir
}
New-Item -ItemType Directory -Force $publishDir | Out-Null

Write-Host "Publishing PDF Handler $version for PROD..." -ForegroundColor Cyan
dotnet publish $uiProject `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$runtimeSettings = [ordered]@{
    SupabaseUrl = $prodUrl
    SupabaseAnonKey = $SupabaseAnonKey
}

if (-not [string]::IsNullOrWhiteSpace($ContactUrl)) {
    $runtimeSettings.ContactUrl = $ContactUrl
}
if (-not [string]::IsNullOrWhiteSpace($ProductPageUrl)) {
    $runtimeSettings.ProductPageUrl = $ProductPageUrl
}
if (-not [string]::IsNullOrWhiteSpace($SurveyFormUrl)) {
    $runtimeSettings.SurveyFormUrl = $SurveyFormUrl
}

$runtimeSettings |
    ConvertTo-Json -Depth 3 |
    Set-Content -Path (Join-Path $publishDir "PdfHandler.runtime.json") -Encoding UTF8

@"
PDF Handler Release Package
Version: $version
Supabase: PROD ($prodUrl)

Before delivery:
- Launch PdfHandler.UI.exe from this folder.
- Activate with a PROD license key.
- Verify license status and device management.
- Zip the whole folder, including PdfHandler.runtime.json.
"@ | Set-Content -Path (Join-Path $publishDir "README_RELEASE.txt") -Encoding UTF8

Write-Host "Release package created:" -ForegroundColor Green
Write-Host $publishDir

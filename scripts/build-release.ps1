# Build a customer-facing Release package.
# Output layout:
#   artifacts\release\prod\PdfHandler-<version>-win-x64
#   artifacts\release\dev\PdfHandler-<version>-win-x64
#
# PROD:
#   The PROD anon key is public in Supabase clients, but keep it out of git.
#   Prefer scripts\Secrets.local.ps1 (local only, git ignored).
#
# DEV:
#   Uses the DEV Supabase URL / anon key below unless -SupabaseAnonKey is passed.

param(
    [string]$TargetEnvironment = "",
    [string]$SupabaseAnonKey = "",
    [string]$ContactUrl = $env:CONTACT_URL,
    [string]$ProductPageUrl = $env:PRODUCT_PAGE_URL,
    [string]$SurveyFormUrl = $env:SURVEY_FORM_URL,
    [switch]$NoKill
)

$ErrorActionPreference = "Stop"

$secretsPath = Join-Path $PSScriptRoot "Secrets.local.ps1"
if (Test-Path $secretsPath) {
    . $secretsPath
}

$projectRoot = Split-Path -Parent $PSScriptRoot
$uiProject = Join-Path $projectRoot "src\PdfHandler.UI\PdfHandler.UI.csproj"
$prodUrl = "https://kmrzktsykjibslajpecu.supabase.co"
$devUrl = "https://yzmjuotvkxcfnsgleyxl.supabase.co"
$devAnonKey = "sb_publishable_ELiCbHZwAR-ekkwEvhzCcQ_mWWYB_-2"

$targetInput = ($TargetEnvironment ?? "").Trim()
if ([string]::IsNullOrWhiteSpace($targetInput)) {
    Write-Host "Select target environment for release build:" -ForegroundColor Cyan
    Write-Host "  [1] PROD" -ForegroundColor DarkGray
    Write-Host "  [2] DEV" -ForegroundColor DarkGray
    $selection = (Read-Host "Enter 1 or 2").Trim()
    switch ($selection) {
        "1" { $targetInput = "PROD" }
        "2" { $targetInput = "DEV" }
        default { throw "Invalid selection: '$selection'. Use 1 (PROD) or 2 (DEV)." }
    }
}

$target = $targetInput.ToUpperInvariant()
if ($target -ne "PROD" -and $target -ne "DEV") {
    throw "Invalid -TargetEnvironment '$TargetEnvironment'. Use DEV or PROD."
}

if ($target -eq "PROD") {
    $targetUrl = $prodUrl
    if ([string]::IsNullOrWhiteSpace($SupabaseAnonKey)) {
        $SupabaseAnonKey = $env:PDFHANDLER_PROD_SUPABASE_ANON_KEY
    }
    if ([string]::IsNullOrWhiteSpace($SupabaseAnonKey)) {
        throw "PROD Supabase anon key is required. Set PDFHANDLER_PROD_SUPABASE_ANON_KEY, pass -SupabaseAnonKey, or create scripts\Secrets.local.ps1 from scripts\Secrets.local.ps1.example."
    }
} else {
    $targetUrl = $devUrl
    if ([string]::IsNullOrWhiteSpace($SupabaseAnonKey)) {
        $SupabaseAnonKey = $devAnonKey
    }
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

$targetFolder = $target.ToLowerInvariant()
$publishDir = Join-Path $projectRoot "artifacts\release\$targetFolder\PdfHandler-$version-win-x64"
if (Test-Path $publishDir) {
    Remove-Item -Recurse -Force $publishDir
}
New-Item -ItemType Directory -Force $publishDir | Out-Null

Write-Host "Publishing PDF Handler $version for $target..." -ForegroundColor Cyan
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
    SupabaseUrl = $targetUrl
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
Environment: $target
Supabase: $target ($targetUrl)

Before delivery:
- Launch PdfHandler.UI.exe from this folder.
- Activate with a $target license key.
- Verify license status and device management.
- Zip the whole folder, including PdfHandler.runtime.json.
"@ | Set-Content -Path (Join-Path $publishDir "README_RELEASE.txt") -Encoding UTF8

Write-Host "Release package created:" -ForegroundColor Green
Write-Host $publishDir

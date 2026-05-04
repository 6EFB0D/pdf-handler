# Launch PDF Handler against the PROD Supabase project.
# Pass -SupabaseAnonKey or set PDFHANDLER_PROD_SUPABASE_ANON_KEY in your shell.

param(
    [string]$SupabaseAnonKey = $env:PDFHANDLER_PROD_SUPABASE_ANON_KEY,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$uiProject = Join-Path $projectRoot "src\PdfHandler.UI\PdfHandler.UI.csproj"
$prodUrl = "https://kmrzktsykjibslajpecu.supabase.co"

if ([string]::IsNullOrWhiteSpace($SupabaseAnonKey)) {
    throw "PROD Supabase anon key is required. Set PDFHANDLER_PROD_SUPABASE_ANON_KEY or pass -SupabaseAnonKey."
}

$env:SUPABASE_URL = $prodUrl
$env:SUPABASE_ANON_KEY = $SupabaseAnonKey
$env:PDFHANDLER_ENVIRONMENT = "PROD"

Get-Process -Name "PdfHandler.UI" -ErrorAction SilentlyContinue |
    Stop-Process -Force -ErrorAction SilentlyContinue

Write-Host "Starting PDF Handler (PROD Supabase)..." -ForegroundColor Red
Write-Host "SUPABASE_URL=$env:SUPABASE_URL" -ForegroundColor DarkGray

if ($NoBuild) {
    $exePath = Join-Path $projectRoot "src\PdfHandler.UI\bin\Debug\net8.0-windows\PdfHandler.UI.exe"
    Start-Process -FilePath $exePath -WorkingDirectory (Split-Path -Parent $exePath)
} else {
    dotnet run --project $uiProject
}

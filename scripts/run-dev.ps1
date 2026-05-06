# Launch PDF Handler against the DEV Supabase project.

param(
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$uiProject = Join-Path $projectRoot "src\PdfHandler.UI\PdfHandler.UI.csproj"

$env:SUPABASE_URL = "https://yzmjuotvkxcfnsgleyxl.supabase.co"
$env:SUPABASE_ANON_KEY = "sb_publishable_ELiCbHZwAR-ekkwEvhzCcQ_mWWYB_-2"
$env:PDFHANDLER_ENVIRONMENT = "DEV"

Get-Process -Name "PdfHandler.UI" -ErrorAction SilentlyContinue |
    Stop-Process -Force -ErrorAction SilentlyContinue

Write-Host "Starting PDF Handler (DEV Supabase)..." -ForegroundColor Cyan
Write-Host "SUPABASE_URL=$env:SUPABASE_URL" -ForegroundColor DarkGray

if ($NoBuild) {
    $exePath = Join-Path $projectRoot "src\PdfHandler.UI\bin\Debug\net8.0-windows\PdfHandler.UI.exe"
    Start-Process -FilePath $exePath -WorkingDirectory (Split-Path -Parent $exePath)
} else {
    dotnet run --project $uiProject
}

# PDF Handler Build Script
# Stops running PdfHandler processes before build to prevent exe lock

param(
    [switch]$Release,
    [switch]$NoKill
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$uiProject = Join-Path $projectRoot "src\PdfHandler.UI\PdfHandler.UI.csproj"
$config = if ($Release) { "Release" } else { "Debug" }
$exePath = Join-Path $projectRoot "src\PdfHandler.UI\bin\$config\net8.0-windows\PdfHandler.UI.exe"

# Stop running PdfHandler processes
if (-not $NoKill) {
    $stopScript = Join-Path $PSScriptRoot "Stop-PdfHandlerProcesses.ps1"
    $null = & $stopScript -WaitSeconds 5 -Configuration $config
    $still = Get-Process -Name "PdfHandler*" -ErrorAction SilentlyContinue
    if ($still) {
        Write-Host "Warning: Process still running. Stop Cursor/VS debugger and try again." -ForegroundColor Red
    }
}

# Build (retry once on lock error)
for ($attempt = 1; $attempt -le 2; $attempt++) {
    Write-Host "Building (config: $config, attempt $attempt/2)..." -ForegroundColor Cyan
    & dotnet build $uiProject -c $config
    if ($LASTEXITCODE -eq 0) { exit 0 }
    if ($attempt -eq 1) {
        Write-Host "`nPossible lock error. Force stopping processes and retrying..." -ForegroundColor Yellow
        Get-Process -Name "PdfHandler*" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
        $null = & taskkill /F /IM "PdfHandler.UI.exe" 2>&1
        Start-Sleep -Seconds 5
    }
}
exit 1

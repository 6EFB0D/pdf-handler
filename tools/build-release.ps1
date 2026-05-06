# PDF Handler インストーラビルド + SHA-256 チェックサム生成スクリプト
# 使い方:
#   .\tools\build-release.ps1 -TargetEnvironment PROD
#   .\tools\build-release.ps1 -TargetEnvironment DEV

param(
    [ValidateSet("DEV", "PROD")]
    [string]$TargetEnvironment = "PROD"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$UiProject = Join-Path $ProjectRoot "src\PdfHandler.UI\PdfHandler.UI.csproj"
$IssFile = Join-Path $ProjectRoot "installer\installer.iss"
$OutputDir = Join-Path $ProjectRoot "installer_output"

if (-not (Test-Path $IssFile)) {
    Write-Error "Inno Setup script not found: $IssFile"
}

[xml]$ProjectXml = Get-Content $UiProject
$Version = $ProjectXml.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = "0.0.0"
}

$TargetTag = $TargetEnvironment.ToLowerInvariant()
$SourceDir = Join-Path $ProjectRoot "artifacts\release\$TargetTag\PdfHandler-$Version-win-x64"
if (-not (Test-Path $SourceDir)) {
    Write-Error "Release folder not found: $SourceDir`nRun .\scripts\build-release.ps1 -TargetEnvironment $TargetEnvironment first."
}

New-Item -ItemType Directory -Force $OutputDir | Out-Null

# ---------- 1. ISCC.exe を探す ----------
$IsccCandidates = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)
$Iscc = $IsccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $Iscc) {
    Write-Error "Inno Setup が見つかりません。インストール先を確認してください。"
}

# ---------- 2. コンパイル ----------
Write-Host "`n[1/3] Inno Setup でビルド中..." -ForegroundColor Cyan
& $Iscc `
    "/DMyAppVersion=$Version" `
    "/DSourceDir=$SourceDir" `
    "/DOutputDir=$OutputDir" `
    "/DTargetEnvironment=$TargetEnvironment" `
    "/DTargetTag=$TargetTag" `
    $IssFile
if ($LASTEXITCODE -ne 0) {
    Write-Error "ビルドに失敗しました（終了コード: $LASTEXITCODE）"
}
Write-Host "      ビルド完了。" -ForegroundColor Green

# ---------- 3. 最新の .exe を取得 ----------
Write-Host "`n[2/3] インストーラを確認中..." -ForegroundColor Cyan
$Installer = Get-ChildItem $OutputDir -Filter "*-setup.exe" |
             Sort-Object LastWriteTime -Descending |
             Select-Object -First 1

if (-not $Installer) {
    Write-Error "インストーラが $OutputDir に見つかりません。"
}
Write-Host "      対象: $($Installer.Name)" -ForegroundColor Green

# ---------- 4. SHA-256 生成 ----------
Write-Host "`n[3/3] SHA-256 を生成中..." -ForegroundColor Cyan
$Hash = (Get-FileHash $Installer.FullName -Algorithm SHA256).Hash
$Date = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

$ChecksumText = @"
# pdfHandler インストーラ チェックサム
# 生成日時: $Date
# ファイル: $($Installer.Name)

SHA256: $Hash
"@

$ChecksumFile = Join-Path $OutputDir "$($Installer.BaseName)-checksum.txt"
[System.IO.File]::WriteAllText($ChecksumFile, $ChecksumText, [System.Text.UTF8Encoding]::new($false))

# ---------- 5. 結果表示 ----------
Write-Host ""
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "  ビルド完了！" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "  ファイル : $($Installer.Name)"
Write-Host "  SHA-256  : $Hash"
Write-Host "  保存先   : $ChecksumFile"
Write-Host ""
Write-Host "  GitHub Releases の説明欄にこのハッシュを貼り付けてください:" -ForegroundColor Cyan
Write-Host "  SHA-256: $Hash" -ForegroundColor White
Write-Host ""

# クリップボードにコピー
$Hash | Set-Clipboard
Write-Host "  (SHA-256 をクリップボードにコピーしました)" -ForegroundColor DarkGray

# PDF Handler インストーラビルド + GitHub Release 用アセット生成
# 使い方:
#   .\tools\build-release.ps1 -TargetEnvironment PROD
#
# GitHub Releases に載せるもの（v1.1.2 以降のルール）:
#   - PdfHandler-<version>-<prod|dev>-setup.exe
#   - PdfHandler-<version>-<prod|dev>-setup-checksum.txt
#   - PdfHandler-<version>-<prod|dev>-setup.zip  （setup.exe を ZIP にしたもの。EXE 直ダウンロードがブロックされる場合用）
#
# ※ win-x64.zip（展開して PdfHandler.UI.exe を起動する形式）は GitHub 公開しない。

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

$IsccCandidates = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)
$Iscc = $IsccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $Iscc) {
    Write-Error "Inno Setup が見つかりません。インストール先を確認してください。"
}

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

Write-Host "`n[2/3] インストーラ ZIP を作成中..." -ForegroundColor Cyan
$Installer = Get-ChildItem $OutputDir -Filter "*-setup.exe" |
             Sort-Object LastWriteTime -Descending |
             Select-Object -First 1
if (-not $Installer) {
    Write-Error "インストーラが $OutputDir に見つかりません。"
}

$InstallerZipPath = Join-Path $OutputDir ($Installer.BaseName + ".zip")
if (Test-Path -LiteralPath $InstallerZipPath) {
    Remove-Item -LiteralPath $InstallerZipPath -Force
}
Compress-Archive -LiteralPath $Installer.FullName -DestinationPath $InstallerZipPath -CompressionLevel Optimal
$InstallerZip = Get-Item -LiteralPath $InstallerZipPath
Write-Host "      作成: $($InstallerZip.Name)" -ForegroundColor Green

Write-Host "`n[3/3] SHA-256 を生成中..." -ForegroundColor Cyan
$InstallerHash = (Get-FileHash $Installer.FullName -Algorithm SHA256).Hash
$Date = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

$InstallerChecksumText = @"
# PDFハンドラ インストーラ チェックサム
# 生成日時: $Date
# ファイル: $($Installer.Name)

SHA256: $InstallerHash
"@

$InstallerChecksumFile = Join-Path $OutputDir "$($Installer.BaseName)-checksum.txt"
[System.IO.File]::WriteAllText($InstallerChecksumFile, $InstallerChecksumText, [System.Text.UTF8Encoding]::new($false))

Write-Host ""
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "  ビルド完了！" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "  インストーラ     : $($Installer.Name)"
Write-Host "  インストーラ ZIP : $($InstallerZip.Name)"
Write-Host "  SHA-256 (setup)  : $InstallerHash"
Write-Host ""
Write-Host "  GitHub Releases の Assets:" -ForegroundColor Cyan
Write-Host "    - $($Installer.Name)" -ForegroundColor White
Write-Host "    - $($Installer.BaseName)-checksum.txt" -ForegroundColor White
Write-Host "    - $($InstallerZip.Name)" -ForegroundColor White
Write-Host ""
Write-Host "  ※ Source code (zip) は GitHub が自動生成します。" -ForegroundColor DarkGray
Write-Host ""

$InstallerHash | Set-Clipboard
Write-Host "  (setup.exe の SHA-256 をクリップボードにコピーしました)" -ForegroundColor DarkGray

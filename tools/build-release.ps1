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

# ---------- 4. ポータブル ZIP（scripts で生成済みならコピー、なければ作成） ----------
Write-Host "`n[3/4] ポータブル ZIP を確認中..." -ForegroundColor Cyan
$ReleaseRoot = Split-Path $SourceDir -Parent
$PortableZipName = "PdfHandler-$Version-win-x64.zip"
$PortableZipInArtifacts = Join-Path $ReleaseRoot $PortableZipName
$PortableZipInOutput = Join-Path $OutputDir $PortableZipName

if (Test-Path -LiteralPath $PortableZipInArtifacts) {
    Copy-Item -LiteralPath $PortableZipInArtifacts -Destination $PortableZipInOutput -Force
    $PortableZip = Get-Item -LiteralPath $PortableZipInOutput
    Write-Host "      コピー: $($PortableZip.Name)" -ForegroundColor Green
} else {
    if (Test-Path -LiteralPath $PortableZipInOutput) {
        Remove-Item -LiteralPath $PortableZipInOutput -Force
    }
    $items = Get-ChildItem -LiteralPath $SourceDir -Force
    Compress-Archive -Path ($items | ForEach-Object { $_.FullName }) -DestinationPath $PortableZipInOutput -CompressionLevel Optimal
    $PortableZip = Get-Item -LiteralPath $PortableZipInOutput
    Write-Host "      作成: $($PortableZip.Name)" -ForegroundColor Green
}

# ---------- 5. SHA-256 生成 ----------
Write-Host "`n[4/4] SHA-256 を生成中..." -ForegroundColor Cyan
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

$PortableHash = (Get-FileHash $PortableZip.FullName -Algorithm SHA256).Hash
$PortableChecksumText = @"
# PDFハンドラ ポータブル ZIP チェックサム
# 生成日時: $Date
# ファイル: $($PortableZip.Name)

SHA256: $PortableHash
"@

$PortableChecksumFile = Join-Path $OutputDir ($PortableZip.BaseName + "-checksum.txt")
[System.IO.File]::WriteAllText($PortableChecksumFile, $PortableChecksumText, [System.Text.UTF8Encoding]::new($false))

# ---------- 6. 結果表示 ----------
Write-Host ""
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "  ビルド完了！" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "  インストーラ : $($Installer.Name)"
Write-Host "  インストーラ SHA-256 : $InstallerHash"
Write-Host "  ZIP          : $($PortableZip.Name)"
Write-Host "  ZIP SHA-256  : $PortableHash"
Write-Host ""
Write-Host "  GitHub Releases の Assets に以下を添付:" -ForegroundColor Cyan
Write-Host "    - $($Installer.Name)" -ForegroundColor White
Write-Host "    - $($Installer.BaseName)-checksum.txt" -ForegroundColor White
Write-Host "    - $($PortableZip.Name)" -ForegroundColor White
Write-Host "    - $($PortableZip.BaseName)-checksum.txt" -ForegroundColor White
Write-Host ""
Write-Host "  EXE が SmartScreen / Defender で止まる場合は ZIP を案内してください。" -ForegroundColor DarkGray
Write-Host ""

$InstallerHash | Set-Clipboard
Write-Host "  (インストーラ SHA-256 をクリップボードにコピーしました)" -ForegroundColor DarkGray

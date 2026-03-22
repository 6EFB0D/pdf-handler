# v1.0.0 バックアップスクリプト
# PDF Handler の v1.0.0 をビルドし、backups/v1.0.0/ に保存します。
# 顧客サポート・バグ再現試験用に使用してください。
#
# 前提: git タグ v1.0.0 が存在すること（存在しない場合は先に git tag -a v1.0.0 を実行）

param(
    [switch]$SkipCheckout  # 現在のブランチのままビルドする（タグにチェックアウトしない）
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$backupDir = Join-Path $projectRoot "backups\v1.0.0"

# バックアップディレクトリ作成
New-Item -ItemType Directory -Force -Path $backupDir | Out-Null

# タグの存在確認
$tagExists = git tag -l "v1.0.0" 2>$null
if (-not $tagExists -and -not $SkipCheckout) {
    Write-Host "Error: タグ v1.0.0 が存在しません。先に以下を実行してください:" -ForegroundColor Red
    Write-Host "  git tag -a v1.0.0 -m `"PDF Handler v1.0.0`"" -ForegroundColor Yellow
    exit 1
}

$originalBranch = git rev-parse --abbrev-ref HEAD 2>$null

try {
    if (-not $SkipCheckout) {
        Write-Host "v1.0.0 にチェックアウトしています..." -ForegroundColor Cyan
        git checkout v1.0.0
    }

    Write-Host "ビルドしています..." -ForegroundColor Cyan
    & "$PSScriptRoot\build.ps1" -Release -NoKill
    if ($LASTEXITCODE -ne 0) {
        throw "ビルドに失敗しました"
    }

    $releaseDir = Join-Path $projectRoot "src\PdfHandler.UI\bin\Release\net8.0-windows"
    if (-not (Test-Path $releaseDir)) {
        throw "ビルド出力が見つかりません: $releaseDir"
    }

    Write-Host "バックアップを $backupDir にコピーしています..." -ForegroundColor Cyan
    Copy-Item (Join-Path $releaseDir "*") -Destination $backupDir -Recurse -Force

    # バージョン情報を記録
    $dllPath = Join-Path $backupDir "PdfHandler.UI.dll"
    if (Test-Path $dllPath) {
        $ver = (Get-Item $dllPath).VersionInfo
        @"
PDF Handler v1.0.0 バックアップ
作成日時: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
FileVersion: $($ver.FileVersion)
ProductVersion: $($ver.ProductVersion)
"@ | Out-File (Join-Path $backupDir "version-info.txt") -Encoding UTF8
    }

    Write-Host "完了しました。バックアップ先: $backupDir" -ForegroundColor Green
}
finally {
    if (-not $SkipCheckout -and $originalBranch) {
        Write-Host "元のブランチ ($originalBranch) に戻しています..." -ForegroundColor Cyan
        git checkout $originalBranch 2>$null
    }
}

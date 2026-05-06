# PDFハンドラ - ライセンス残日数設定スクリプト
# 使用方法: .\SetLicenseRemainingDays.ps1 -RemainingDays 3

param(
    [Parameter(Mandatory=$true)]
    [int]$RemainingDays
)

$licensePath = "$env:APPDATA\PDFHandler\license.json"

if (-not (Test-Path $licensePath)) {
    Write-Host "エラー: ライセンスファイルが見つかりません: $licensePath" -ForegroundColor Red
    Write-Host "アプリを一度起動してライセンスファイルを作成してください。" -ForegroundColor Yellow
    exit 1
}

try {
    # JSONを読み込み
    $jsonContent = Get-Content $licensePath -Raw -Encoding UTF8
    $json = $jsonContent | ConvertFrom-Json
    
    # FirstLaunchDateを（14 - 残日数）日前に設定
    $daysAgo = 14 - $RemainingDays
    $targetDate = (Get-Date).AddDays(-$daysAgo)
    $json.FirstLaunchDate = $targetDate.ToString("yyyy-MM-ddTHH:mm:ss")
    $json.Plan = 0  # Trial
    $json.LicenseKey = $null
    
    # JSONを保存
    $json | ConvertTo-Json -Depth 10 | Set-Content $licensePath -Encoding UTF8
    
    Write-Host "✓ 残日数を $RemainingDays 日に設定しました" -ForegroundColor Green
    Write-Host "  初回起動日: $($json.FirstLaunchDate)" -ForegroundColor Gray
    Write-Host "  アプリを再起動して確認してください。" -ForegroundColor Yellow
} catch {
    Write-Host "エラー: ライセンスファイルの編集に失敗しました" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}



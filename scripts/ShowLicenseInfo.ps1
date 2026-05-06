# PDFハンドラ - ライセンス情報表示スクリプト

$licensePath = "$env:APPDATA\PDFHandler\license.json"

if (-not (Test-Path $licensePath)) {
    Write-Host "ライセンスファイルが見つかりません: $licensePath" -ForegroundColor Yellow
    Write-Host "アプリを一度起動してライセンスファイルを作成してください。" -ForegroundColor Yellow
    exit 0
}

try {
    $jsonContent = Get-Content $licensePath -Raw -Encoding UTF8
    $json = $jsonContent | ConvertFrom-Json
    
    Write-Host "=== PDFハンドラ ライセンス情報 ===" -ForegroundColor Cyan
    Write-Host ""
    
    # プラン名を取得
    $planNames = @{
        0 = "試用期間中"
        1 = "Standard版（買い切り）"
        2 = "Standard版（サブスクリプション）"
        3 = "Premium版"
        4 = "Premium版（BYOK）"
    }
    
    $planName = $planNames[$json.Plan]
    if (-not $planName) {
        $planName = "不明 ($($json.Plan))"
    }
    
    Write-Host "プラン: " -NoNewline
    Write-Host $planName -ForegroundColor White
    
    if ($json.Plan -eq 0) {
        $firstLaunch = [DateTime]::Parse($json.FirstLaunchDate)
        $elapsed = (Get-Date) - $firstLaunch
        $remaining = 14 - $elapsed.TotalDays
        $remainingDays = [Math]::Max(0, [Math]::Ceiling($remaining))
        
        Write-Host "初回起動日: $($json.FirstLaunchDate)" -ForegroundColor Gray
        Write-Host "残り日数: " -NoNewline
        if ($remainingDays -gt 0) {
            Write-Host "$remainingDays 日" -ForegroundColor Green
        } else {
            Write-Host "0 日（試用期間終了）" -ForegroundColor Red
        }
    } else {
        Write-Host "ライセンスキー: $($json.LicenseKey)" -ForegroundColor Gray
        if ($json.ActivationDate) {
            Write-Host "アクティベーション日: $($json.ActivationDate)" -ForegroundColor Gray
        }
        if ($json.SubscriptionRenewalDate) {
            Write-Host "サブスクリプション更新日: $($json.SubscriptionRenewalDate)" -ForegroundColor Gray
        }
    }
    
    Write-Host "ハードウェアID: $($json.HardwareId)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "ファイルパス: $licensePath" -ForegroundColor DarkGray
} catch {
    Write-Host "エラー: ライセンスファイルの読み込みに失敗しました" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}



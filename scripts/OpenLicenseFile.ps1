# PDFハンドラ - ライセンスファイルを開くスクリプト

$licensePath = "$env:APPDATA\PDFHandler\license.json"

if (-not (Test-Path $licensePath)) {
    Write-Host "ライセンスファイルが見つかりません: $licensePath" -ForegroundColor Yellow
    Write-Host "アプリを一度起動してライセンスファイルを作成してください。" -ForegroundColor Yellow
    
    # フォルダを開く
    $folderPath = Split-Path $licensePath
    if (Test-Path $folderPath) {
        explorer.exe $folderPath
    }
    exit 0
}

# メモ帳で開く
notepad $licensePath
Write-Host "ライセンスファイルを開きました: $licensePath" -ForegroundColor Green



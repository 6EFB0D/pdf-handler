# PDFハンドラ - ライセンスリセットスクリプト
# ライセンスファイルを削除して、次回起動時にトライアルを開始します

$licensePath = "$env:APPDATA\PDFHandler\license.json"

if (-not (Test-Path $licensePath)) {
    Write-Host "ライセンスファイルが見つかりません: $licensePath" -ForegroundColor Yellow
    Write-Host "既にトライアル状態です。" -ForegroundColor Yellow
    exit 0
}

Write-Host "ライセンスファイルを削除しますか？ (Y/N): " -NoNewline -ForegroundColor Yellow
$response = Read-Host

if ($response -eq "Y" -or $response -eq "y") {
    try {
        # バックアップを作成
        $backupPath = "$licensePath.backup.$(Get-Date -Format 'yyyyMMddHHmmss')"
        Copy-Item $licensePath $backupPath
        Write-Host "バックアップを作成しました: $backupPath" -ForegroundColor Gray
        
        # ライセンスファイルを削除
        Remove-Item $licensePath
        Write-Host "✓ ライセンスファイルを削除しました" -ForegroundColor Green
        Write-Host "次回アプリ起動時に14日間のトライアルが開始されます。" -ForegroundColor Yellow
    } catch {
        Write-Host "エラー: ライセンスファイルの削除に失敗しました" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "キャンセルしました。" -ForegroundColor Gray
}



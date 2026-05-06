# ライセンスファイル操作ヘルパー

## ライセンスファイルの場所

- **Windows**: `%APPDATA%\PDFHandler\license.json`
- **完全パス例**: `C:\Users\<ユーザー名>\AppData\Roaming\PDFHandler\license.json`

## PowerShellスクリプト（Windows用）

### ライセンスファイルを開く
```powershell
# ライセンスファイルのパスを取得
$licensePath = "$env:APPDATA\PDFHandler\license.json"

# ファイルが存在するか確認
if (Test-Path $licensePath) {
    # メモ帳で開く
    notepad $licensePath
} else {
    Write-Host "ライセンスファイルが見つかりません: $licensePath"
}
```

### ライセンスファイルをバックアップ
```powershell
$licensePath = "$env:APPDATA\PDFHandler\license.json"
$backupPath = "$env:APPDATA\PDFHandler\license.backup.json"

if (Test-Path $licensePath) {
    Copy-Item $licensePath $backupPath
    Write-Host "バックアップを作成しました: $backupPath"
} else {
    Write-Host "ライセンスファイルが見つかりません"
}
```

### ライセンスファイルを削除（トライアルにリセット）
```powershell
$licensePath = "$env:APPDATA\PDFHandler\license.json"

if (Test-Path $licensePath) {
    Remove-Item $licensePath
    Write-Host "ライセンスファイルを削除しました。次回起動時にトライアルが開始されます。"
} else {
    Write-Host "ライセンスファイルが見つかりません"
}
```

### 残日数を0に設定（14日前に設定）
```powershell
$licensePath = "$env:APPDATA\PDFHandler\license.json"

if (Test-Path $licensePath) {
    # JSONを読み込み
    $json = Get-Content $licensePath -Raw | ConvertFrom-Json
    
    # FirstLaunchDateを14日前に設定
    $json.FirstLaunchDate = (Get-Date).AddDays(-14).ToString("yyyy-MM-ddTHH:mm:ss")
    $json.Plan = 0  # Trial
    $json.LicenseKey = $null
    
    # JSONを保存
    $json | ConvertTo-Json -Depth 10 | Set-Content $licensePath
    Write-Host "残日数を0に設定しました（14日前に設定）"
} else {
    Write-Host "ライセンスファイルが見つかりません"
}
```

### 残日数を14日に設定（今日の日付に設定）
```powershell
$licensePath = "$env:APPDATA\PDFHandler\license.json"

if (Test-Path $licensePath) {
    # JSONを読み込み
    $json = Get-Content $licensePath -Raw | ConvertFrom-Json
    
    # FirstLaunchDateを今日に設定
    $json.FirstLaunchDate = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss")
    $json.Plan = 0  # Trial
    $json.LicenseKey = $null
    
    # JSONを保存
    $json | ConvertTo-Json -Depth 10 | Set-Content $licensePath
    Write-Host "残日数を14日に設定しました（今日の日付に設定）"
} else {
    Write-Host "ライセンスファイルが見つかりません"
}
```

### 残日数を指定した日数に設定
```powershell
param(
    [int]$RemainingDays = 14
)

$licensePath = "$env:APPDATA\PDFHandler\license.json"

if (Test-Path $licensePath) {
    # JSONを読み込み
    $json = Get-Content $licensePath -Raw | ConvertFrom-Json
    
    # FirstLaunchDateを（14 - 残日数）日前に設定
    $daysAgo = 14 - $RemainingDays
    $json.FirstLaunchDate = (Get-Date).AddDays(-$daysAgo).ToString("yyyy-MM-ddTHH:mm:ss")
    $json.Plan = 0  # Trial
    $json.LicenseKey = $null
    
    # JSONを保存
    $json | ConvertTo-Json -Depth 10 | Set-Content $licensePath
    Write-Host "残日数を $RemainingDays 日に設定しました"
} else {
    Write-Host "ライセンスファイルが見つかりません"
}

# 使用例: .\SetRemainingDays.ps1 -RemainingDays 3
```

### 現在のライセンス情報を表示
```powershell
$licensePath = "$env:APPDATA\PDFHandler\license.json"

if (Test-Path $licensePath) {
    $json = Get-Content $licensePath -Raw | ConvertFrom-Json
    
    Write-Host "=== ライセンス情報 ==="
    Write-Host "プラン: $($json.Plan)"
    Write-Host "初回起動日: $($json.FirstLaunchDate)"
    
    if ($json.Plan -eq 0) {
        $firstLaunch = [DateTime]::Parse($json.FirstLaunchDate)
        $elapsed = (Get-Date) - $firstLaunch
        $remaining = 14 - $elapsed.TotalDays
        Write-Host "残り日数: $([Math]::Max(0, [Math]::Ceiling($remaining)))日"
    }
    
    Write-Host "ハードウェアID: $($json.HardwareId)"
} else {
    Write-Host "ライセンスファイルが見つかりません"
}
```

## 使用例

### 1. ライセンスファイルを開く
```powershell
notepad "$env:APPDATA\PDFHandler\license.json"
```

### 2. 残日数を3日に設定
```powershell
.\SetRemainingDays.ps1 -RemainingDays 3
```

### 3. トライアルにリセット（残日数14日）
```powershell
.\SetRemainingDays.ps1 -RemainingDays 14
```

### 4. 試用期間を終了させる（残日数0）
```powershell
.\SetRemainingDays.ps1 -RemainingDays 0
```

## 手動編集時の注意事項

1. **JSON形式を正しく保つ**: カンマや引用符を正しく配置してください
2. **日付形式**: `FirstLaunchDate`は`yyyy-MM-ddTHH:mm:ss`形式で指定してください
3. **Plan値**（現在の製品は試用と買い切りのみ）:
   - `0` = Trial（試用期間）
   - `1` = StandardPurchased（買い切り）
4. **バックアップ**: 編集前に必ずバックアップを取ってください

## ライセンスファイルの例

### 試用期間中（残り10日）
```json
{
  "Plan": 0,
  "LicenseKey": null,
  "FirstLaunchDate": "2025-01-05T00:00:00",
  "HardwareId": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "ExpirationDate": null,
  "ActivationDate": null,
  "LastVerificationDate": null,
  "NextVerificationDate": null
}
```

### Standard版（買い切り）
```json
{
  "Plan": 1,
  "LicenseKey": "PDFH-P101-A1B2C3D4E5F6G7H8I9J0K1L2M3-1A2B3C4D",
  "FirstLaunchDate": "2025-01-01T00:00:00",
  "HardwareId": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "ExpirationDate": null,
  "PurchasedVersion": "1",
  "ActivationDate": "2025-01-15T10:30:00",
  "LastVerificationDate": null,
  "NextVerificationDate": null
}
```



# LICENSE_SECRET_KEY 用の32桁ランダム英数字を生成
$chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"
$result = -join ((1..32) | ForEach-Object { $chars[(Get-Random -Maximum $chars.Length)] })
Write-Host $result

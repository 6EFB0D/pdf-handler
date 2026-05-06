# generate-license-key.ps1
# 手動ライセンス発行用 ライセンスキー生成スクリプト
#
# 使い方:
#   .\scripts\generate-license-key.ps1 -SecretKey "YOUR_LICENSE_SECRET_KEY"
#   .\scripts\generate-license-key.ps1 -SecretKey "xxx" -FormCode P101 -Count 3
#
# パラメータ:
#   -SecretKey  : LICENSE_SECRET_KEY (Supabase Secrets に設定した値)
#   -FormCode   : キー種別 (既定: P101 = pdfHandler 買い切り)
#                 P101 = purchased (標準), P201 = purchased (5本パック)
#   -Count      : 生成するキー数 (既定: 1)
#
# 注意:
#   - SecretKey は画面に表示されます。作業後はターミナル履歴をクリアしてください。
#   - 生成したキーは手順書に従って Supabase に INSERT し、メール送付してください。
#   - 本スクリプトをバージョン管理に含める場合、SecretKey を直接書き込まないこと。

param(
    [Parameter(Mandatory = $true)]
    [string]$SecretKey,

    [string]$FormCode = "P101",

    [int]$Count = 1
)

# FormCode の検証
if ($FormCode -notmatch '^(P[12]|S[12])\d{2}$') {
    Write-Error "FormCode の形式が不正です。例: P101, P201, S101"
    exit 1
}

if ($Count -lt 1 -or $Count -gt 50) {
    Write-Error "Count は 1〜50 の範囲で指定してください。"
    exit 1
}

Write-Host ""
Write-Host "=== ライセンスキー生成 ===" -ForegroundColor Cyan
Write-Host "FormCode : $FormCode"
Write-Host "生成数   : $Count"
Write-Host ""

$results = @()

for ($i = 1; $i -le $Count; $i++) {

    # 1. シリアル生成（14 バイト = 28 文字の大文字 HEX）
    $serialBytes = [byte[]]::new(14)
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($serialBytes)
    $serial = [BitConverter]::ToString($serialBytes).Replace("-", "").ToUpperInvariant()

    # 2. HMAC-SHA256 計算
    #    署名対象: "PDFH:{FormCode}:{serial}"  ← LicenseKeyHelper.cs と同じ
    $message     = "PDFH:$($FormCode):$($serial)"
    $keyBytes    = [System.Text.Encoding]::UTF8.GetBytes($SecretKey)
    $msgBytes    = [System.Text.Encoding]::UTF8.GetBytes($message)
    $hmacObj     = [System.Security.Cryptography.HMACSHA256]::new($keyBytes)
    $hash        = $hmacObj.ComputeHash($msgBytes)
    $hmacStr     = [BitConverter]::ToString($hash).Replace("-", "").ToUpperInvariant()
    $hmacObj.Dispose()

    # 3. 正規化形式（DB 保存用）
    $normalizedKey = "PDFH-$($FormCode)-$($serial)-$($hmacStr)"

    # 4. 表示用形式（メール送付用・4桁区切り）
    $serialParts    = ($serial -split '(?<=\G.{4})(?=.)') -join '-'
    $displayKey     = "PDFH-$($FormCode)-$($serialParts)-$($hmacStr)"

    $results += [PSCustomObject]@{
        No           = $i
        NormalizedKey = $normalizedKey
        DisplayKey   = $displayKey
    }

    Write-Host "[$i] DB保存用（正規化）:"
    Write-Host "      $normalizedKey" -ForegroundColor Green
    Write-Host "    メール送付用（表示）:"
    Write-Host "      $displayKey" -ForegroundColor Yellow
    Write-Host ""
}

# クリップボードに最初のキー（1件の場合）をコピー
if ($Count -eq 1) {
    $results[0].DisplayKey | Set-Clipboard
    Write-Host "（メール送付用キーをクリップボードにコピーしました）" -ForegroundColor Gray
}

Write-Host "=== 完了 ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "[次のステップ]"
Write-Host "  1. 上記の DB保存用キーを Supabase に INSERT する"
Write-Host "     手順書: docs/operations/06_manual-license-issuance.md"
Write-Host "  2. メール送付用キーを顧客にメールで送付する"
Write-Host "  3. Supabase で is_active=true / payment_confirmed_at を設定する"
Write-Host ""

return $results

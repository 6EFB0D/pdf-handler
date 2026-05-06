# PROD 謗･邯壹〒縺ｮ蜍穂ｽ懃｢ｺ隱咲畑縲ゅン繝ｫ繝峨〒縺ｯ縺ｪ縺上い繝励Μ襍ｷ蜍輔・陬懷勧縺ｧ縺吶・#
# 菴ｿ縺・婿:
#   cd <pdf-handler 繝ｪ繝昴ず繝医Μ繝ｫ繝ｼ繝・
#   . .\scripts\Secrets.local.ps1
#   .\scripts\run-prod.ps1

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$SecretsScript = Join-Path $ProjectRoot "scripts\Secrets.local.ps1"
if (Test-Path -LiteralPath $SecretsScript) {
    Write-Host "[info] scripts\Secrets.local.ps1 繧定ｪｭ縺ｿ霎ｼ繧薙〒縺・∪縺・ -ForegroundColor DarkGray
    . $SecretsScript
}
else {
    Write-Warning "Secrets.local.ps1 縺後≠繧翫∪縺帙ｓ縲・ .\scripts\Secrets.local.ps1 縺ｧ謇句虚隱ｭ縺ｿ霎ｼ縺ｿ縺励◆縺狗｢ｺ隱阪＠縺ｦ縺上□縺輔＞縲・
}

$UiProject = Join-Path $ProjectRoot "src\PdfHandler.UI\PdfHandler.UI.csproj"

& dotnet run --project $UiProject --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet run failed with exit code $LASTEXITCODE"
}

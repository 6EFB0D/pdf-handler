# PDF繝上Φ繝峨Λ: 繝ｪ繝ｪ繝ｼ繧ｹ逕ｨ EXE 繧・dotnet publish 縺ｧ逕滓・・・IP / Inno 縺ｮ蜑榊ｷ･遞具ｼ・#
# 菴ｿ縺・婿:
#   .\scripts\build-release.ps1 -TargetEnvironment PROD
#   .\scripts\build-release.ps1 -TargetEnvironment DEV
#
# 谺｡縺ｮ繧ｹ繝・ャ繝暦ｼ医う繝ｳ繧ｹ繝医・繝ｩ・・
#   .\tools\build-release.ps1 -TargetEnvironment PROD

param(
    [ValidateSet("DEV", "PROD")]
    [string]$TargetEnvironment = "PROD",
    [int]$BuildNumber = -1
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$UiProject = Join-Path $ProjectRoot "src\PdfHandler.UI\PdfHandler.UI.csproj"

$SecretsScript = Join-Path $ProjectRoot "scripts\Secrets.local.ps1"
if (Test-Path -LiteralPath $SecretsScript) {
    Write-Host '[info] scripts\Secrets.local.ps1 loaded' -ForegroundColor DarkGray
    . $SecretsScript
}

if (-not (Test-Path -LiteralPath $UiProject)) {
    Write-Error ("csproj not found: " + $UiProject)
}

[xml]$ProjectXml = Get-Content $UiProject
$Version = $ProjectXml.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = "0.0.0"
}

function Resolve-BuildNumber {
    param([int]$ExplicitBuildNumber)

    if ($ExplicitBuildNumber -ge 0) {
        return $ExplicitBuildNumber
    }

    if (-not [string]::IsNullOrWhiteSpace($env:PDFHANDLER_BUILD_NUMBER)) {
        [int]$parsed = 0
        if ([int]::TryParse($env:PDFHANDLER_BUILD_NUMBER, [ref]$parsed)) {
            return $parsed
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_RUN_NUMBER)) {
        [int]$parsed = 0
        if ([int]::TryParse($env:GITHUB_RUN_NUMBER, [ref]$parsed)) {
            return $parsed
        }
    }

    return 0
}

$ResolvedBuildNumber = Resolve-BuildNumber -ExplicitBuildNumber $BuildNumber
$InformationalVersion = "$Version+build.$ResolvedBuildNumber"
$FileVersion = "$Version.$ResolvedBuildNumber"

function Get-FileSha256OrNull {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        return $null
    }
    return (Get-FileHash -Algorithm SHA256 -Path $Path).Hash
}

function Get-GitCommitOrNull {
    param([string]$RepoRoot)
    try {
        $commit = (git -C $RepoRoot rev-parse HEAD 2>$null)
        if ([string]::IsNullOrWhiteSpace($commit)) {
            return $null
        }
        return $commit.Trim()
    } catch {
        return $null
    }
}

function Write-BuildManifest {
    param(
        [string]$RepoRoot,
        [string]$TargetEnvironment,
        [string]$Version,
        [int]$BuildNumber,
        [string]$InformationalVersion,
        [string]$OutputDir
    )

    $manifestDir = Join-Path $RepoRoot "artifacts\build-manifest"
    New-Item -ItemType Directory -Force -Path $manifestDir | Out-Null
    $manifestPath = Join-Path $manifestDir "build-manifest.jsonl"

    $exePath = Join-Path $OutputDir "PdfHandler.UI.exe"
    $runtimePath = Join-Path $OutputDir "PdfHandler.runtime.json"
    $readmePath = Join-Path $OutputDir "README_RELEASE.txt"

    $entry = [ordered]@{
        timestamp_utc = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        app = "PdfHandler"
        target_environment = $TargetEnvironment
        version = $Version
        build_number = $BuildNumber
        informational_version = $InformationalVersion
        git_commit = (Get-GitCommitOrNull -RepoRoot $RepoRoot)
        output_dir = $OutputDir
        exe_sha256 = (Get-FileSha256OrNull -Path $exePath)
        runtime_json_sha256 = (Get-FileSha256OrNull -Path $runtimePath)
        readme_release_sha256 = (Get-FileSha256OrNull -Path $readmePath)
    }

    $entry | ConvertTo-Json -Compress | Add-Content -Path $manifestPath -Encoding UTF8

    return $manifestPath
}

$TargetTag = $TargetEnvironment.ToLowerInvariant()
$OutDirName = "PdfHandler-$Version-win-x64"
$OutDir = Join-Path $ProjectRoot "artifacts\release\$TargetTag\$OutDirName"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  dotnet publish  ($TargetEnvironment)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Version : $Version" -ForegroundColor White
Write-Host "  Build   : $ResolvedBuildNumber" -ForegroundColor White
Write-Host "  InformationalVersion : $InformationalVersion" -ForegroundColor White
Write-Host "  Output  : $OutDir" -ForegroundColor White
Write-Host ""

if (Test-Path -LiteralPath $OutDir) {
    Remove-Item -LiteralPath $OutDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

# 繧ｨ繝ｳ繝峨Θ繝ｼ繧ｶ繝ｼ蜷代￠繝昴・繧ｿ繝悶Ν驟榊ｸ・・ self-contained 謗ｨ螂ｨ・・NET 繝ｩ繝ｳ繧ｿ繧､繝譛ｪ蟆主・PC縺ｧ繧ょｮ溯｡悟庄・・& dotnet publish $UiProject `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    /p:DebugType=None `
    /p:DebugSymbols=false `
    /p:FileVersion=$FileVersion `
    /p:InformationalVersion=$InformationalVersion `
    -o $OutDir

if ($LASTEXITCODE -ne 0) {
    Write-Error ("dotnet publish failed with exit code " + $LASTEXITCODE)
}

$manifestPath = Write-BuildManifest `
    -RepoRoot $ProjectRoot `
    -TargetEnvironment $TargetEnvironment `
    -Version $Version `
    -BuildNumber $ResolvedBuildNumber `
    -InformationalVersion $InformationalVersion `
    -OutputDir $OutDir

Write-Host ""
Write-Host ("Done: " + $OutDir) -ForegroundColor Green
Write-Host ("Manifest: " + $manifestPath) -ForegroundColor DarkGray
Write-Host ("Next: .\\tools\\build-release.ps1 -TargetEnvironment " + $TargetEnvironment) -ForegroundColor DarkGray
Write-Host ""

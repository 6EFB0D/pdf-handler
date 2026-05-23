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

function Write-Sha256ChecksumFile {
    param(
        [string]$FilePath,
        [string]$Label
    )

    if (-not (Test-Path -LiteralPath $FilePath)) {
        Write-Error ("File not found for checksum: " + $FilePath)
    }

    $hash = (Get-FileHash -LiteralPath $FilePath -Algorithm SHA256).Hash
    $fileName = [IO.Path]::GetFileName($FilePath)
    $date = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $text = @"
# PDFハンドラ $Label チェックサム
# 生成日時: $date
# ファイル: $fileName

SHA256: $hash
"@
    if ($FilePath -match '\.zip$') {
        $checksumPath = $FilePath -replace '\.zip$', '-checksum.txt'
    } elseif ($FilePath -match '\.exe$') {
        $checksumPath = $FilePath -replace '\.exe$', '-checksum.txt'
    } else {
        $checksumPath = "$FilePath-checksum.txt"
    }

    [System.IO.File]::WriteAllText($checksumPath, $text, [System.Text.UTF8Encoding]::new($false))
    return @{
        Path = $checksumPath
        Hash = $hash
    }
}

function Write-PortableReleaseZip {
    param(
        [string]$SourceDir,
        [string]$Version
    )

    $releaseRoot = Join-Path (Split-Path $SourceDir -Parent) ""
    $zipName = "PdfHandler-$Version-win-x64.zip"
    $zipPath = Join-Path $releaseRoot $zipName

    if (Test-Path -LiteralPath $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }

    $items = Get-ChildItem -LiteralPath $SourceDir -Force
    if ($items.Count -eq 0) {
        Write-Error ("Nothing to zip in: " + $SourceDir)
    }

    Compress-Archive -Path ($items | ForEach-Object { $_.FullName }) -DestinationPath $zipPath -CompressionLevel Optimal

    $checksum = Write-Sha256ChecksumFile -FilePath $zipPath -Label "ポータブル ZIP"

    Write-Host ("  ZIP: " + $zipPath) -ForegroundColor Green
    Write-Host ("  ZIP SHA-256: " + $checksum.Hash) -ForegroundColor DarkGray
    Write-Host ("  Checksum: " + $checksum.Path) -ForegroundColor DarkGray

    return @{
        ZipPath = $zipPath
        ChecksumPath = $checksum.Path
        Sha256 = $checksum.Hash
    }
}

function Write-ReadmeReleaseTxt {
    param(
        [string]$OutputDir,
        [string]$Version,
        [string]$TargetEnvironment,
        [string]$InformationalVersion
    )

    $readmePath = Join-Path $OutputDir "README_RELEASE.txt"
    $content = @"
PDFハンドラ $Version ($TargetEnvironment)
InformationalVersion: $InformationalVersion

起動: PdfHandler.UI.exe をダブルクリック（このフォルダ内）

配布:
- 推奨: インストーラ (tools\build-release.ps1 で生成する *-setup.exe)
- 代替: 同じバージョンの PdfHandler-$Version-win-x64.zip（SmartScreen 等で EXE が止まる場合）

同梱: PdfHandler.runtime.json（接続先 $TargetEnvironment）
"@
    [System.IO.File]::WriteAllText($readmePath, $content.TrimEnd() + "`n", [System.Text.UTF8Encoding]::new($false))
    Write-Host ("  Wrote " + $readmePath) -ForegroundColor DarkGray
}

function Write-PdfHandlerRuntimeJson {
    param(
        [string]$OutputDir,
        [ValidateSet("DEV", "PROD")]
        [string]$TargetEnvironment
    )

    $devUrl = "https://yzmjuotvkxcfnsgleyxl.supabase.co"
    $prodUrl = "https://kmrzktsykjibslajpecu.supabase.co"
    $devAnonFallback = "sb_publishable_ELiCbHZwAR-ekkwEvhzCcQ_mWWYB_-2"

    if ($TargetEnvironment -eq "PROD") {
        $supabaseUrl = $prodUrl
        $anonKey = $env:PDFHANDLER_PROD_SUPABASE_ANON_KEY
        if ([string]::IsNullOrWhiteSpace($anonKey)) {
            $anonKey = $env:SUPABASE_ANON_KEY
        }
    } else {
        $supabaseUrl = $devUrl
        $anonKey = $env:SUPABASE_ANON_KEY
        if ([string]::IsNullOrWhiteSpace($anonKey)) {
            $anonKey = $devAnonFallback
        }
    }

    if ([string]::IsNullOrWhiteSpace($anonKey)) {
        Write-Warning "Supabase Anon Key is empty for $TargetEnvironment. Set PDFHANDLER_PROD_SUPABASE_ANON_KEY or SUPABASE_ANON_KEY in scripts\Secrets.local.ps1"
    }

    $runtime = [ordered]@{
        targetEnvironment = $TargetEnvironment
        supabaseUrl       = $supabaseUrl
        supabaseAnonKey   = $anonKey
    }

    $runtimePath = Join-Path $OutputDir "PdfHandler.runtime.json"
    $runtime | ConvertTo-Json | Set-Content -Path $runtimePath -Encoding UTF8
    Write-Host ("  Wrote " + $runtimePath) -ForegroundColor DarkGray
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
    $releaseRoot = Split-Path $OutputDir -Parent
    $zipPath = Join-Path $releaseRoot ("PdfHandler-" + $Version + "-win-x64.zip")

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
        portable_zip_path = $zipPath
        portable_zip_sha256 = (Get-FileSha256OrNull -Path $zipPath)
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

# エンドユーザー向けポータブル配布: self-contained 推奨（.NET ランタイム未導入PCでも実行可能）
& dotnet publish $UiProject `
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

Write-PdfHandlerRuntimeJson -OutputDir $OutDir -TargetEnvironment $TargetEnvironment
Write-ReadmeReleaseTxt -OutputDir $OutDir -Version $Version -TargetEnvironment $TargetEnvironment -InformationalVersion $InformationalVersion
$zipArtifact = Write-PortableReleaseZip -SourceDir $OutDir -Version $Version

$manifestPath = Write-BuildManifest `
    -RepoRoot $ProjectRoot `
    -TargetEnvironment $TargetEnvironment `
    -Version $Version `
    -BuildNumber $ResolvedBuildNumber `
    -InformationalVersion $InformationalVersion `
    -OutputDir $OutDir

Write-Host ""
Write-Host ("Done: " + $OutDir) -ForegroundColor Green
Write-Host ("ZIP:  " + $zipArtifact.ZipPath) -ForegroundColor Green
Write-Host ("Manifest: " + $manifestPath) -ForegroundColor DarkGray
Write-Host ("Next: .\\tools\\build-release.ps1 -TargetEnvironment " + $TargetEnvironment) -ForegroundColor DarkGray
Write-Host ""

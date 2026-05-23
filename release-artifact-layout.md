# Release Artifact Layout

PDF Handler の配布物・検証出力は、DEV / PROD とバージョンが混ざらないように以下のルールで扱う。

## GitHub Releases に載せるもの（正本）

**v1.1.2 以降のルール**（手動アップロード分）:

| ファイル | 説明 |
|----------|------|
| `PdfHandler-<version>-prod-setup.exe` | Inno Setup インストーラ（**主経路**） |
| `PdfHandler-<version>-prod-setup-checksum.txt` | setup.exe の SHA-256 |
| `PdfHandler-<version>-prod-setup.zip` | **setup.exe を ZIP にしたもの**（EXE 直ダウンロードがブロックされる場合） |

GitHub が自動で付ける **Source code (zip / tar.gz)** はソースアーカイブであり、インストーラではありません。

### GitHub に載せないもの

| ファイル | 理由 |
|----------|------|
| `PdfHandler-<version>-win-x64.zip` | 展開して `PdfHandler.UI.exe` を直接起動する形式。**公開しない**（社内検証用のみ） |

## ビルド成果物（ローカル）

```text
artifacts/
  release/
    prod/
      PdfHandler-<version>-win-x64/          # publish 出力（インストーラの入力・社内検証用）
      PdfHandler-<version>-win-x64.zip       # 社内検証用（GitHub 非公開）

installer_output/                            # tools/build-release.ps1 の出力
  PdfHandler-<version>-prod-setup.exe        # → GitHub Release
  PdfHandler-<version>-prod-setup-checksum.txt
  PdfHandler-<version>-prod-setup.zip        # → GitHub Release
```

## ビルドコマンド

```powershell
.\scripts\build-release.ps1 -TargetEnvironment PROD
.\tools\build-release.ps1 -TargetEnvironment PROD
```

`gh release upload` 例:

```powershell
gh release upload v1.1.3 --repo 6EFB0D/pdf-handler --clobber `
  installer_output/PdfHandler-1.1.3-prod-setup.exe `
  installer_output/PdfHandler-1.1.3-prod-setup-checksum.txt `
  installer_output/PdfHandler-1.1.3-prod-setup.zip
```

## 起動ルール（社内検証）

- PROD: `artifacts\release\prod\PdfHandler-<version>-win-x64\PdfHandler.UI.exe`
- DEV: `artifacts\release\dev\PdfHandler-<version>-win-x64\PdfHandler.UI.exe`

# Release Artifact Layout

PDF Handler の配布物・検証出力は、DEV / PROD とバージョンが混ざらないように以下のルールで扱う。

## 正式な配布候補

`scripts/build-release.ps1` で作成する。

```text
artifacts/
  release/
    prod/
      PdfHandler-<version>-win-x64/          # 配布用展開フォルダ（ZIP 作成元）
        PdfHandler.UI.exe
        PdfHandler.runtime.json              # PROD Supabase
        README_RELEASE.txt
      PdfHandler-<version>-win-x64.zip       # ZIP 版（setup がブロックされる場合の代替）
      PdfHandler-<version>-win-x64-checksum.txt
    dev/
      PdfHandler-<version>-win-x64/
        PdfHandler.UI.exe
        PdfHandler.runtime.json              # DEV Supabase
        README_RELEASE.txt
      PdfHandler-<version>-win-x64.zip
      PdfHandler-<version>-win-x64-checksum.txt

installer_output/                            # Inno Setup 出力（tools/build-release.ps1）
  PdfHandler-<version>-<prod|dev>-setup.exe
  PdfHandler-<version>-<prod|dev>-setup-checksum.txt
  PdfHandler-<version>-win-x64.zip           # リリース作業用にコピー（setup と同梱アップロード用）
  PdfHandler-<version>-win-x64-checksum.txt
```

### 命名規則

| 成果物 | 例（v1.1.3 PROD） |
|--------|-------------------|
| 配布フォルダ | `artifacts/release/prod/PdfHandler-1.1.3-win-x64/` |
| ZIP 版 | `artifacts/release/prod/PdfHandler-1.1.3-win-x64.zip` |
| インストーラ | `installer_output/PdfHandler-1.1.3-prod-setup.exe` |

ZIP 版はインストーラの**代替**（SmartScreen / Defender で setup.exe が止まる場合）。**通常は setup.exe を配布・案内する。**

## ビルドコマンド

推奨（ローカル固定シークレット）:

```powershell
Copy-Item .\scripts\Secrets.local.ps1.example .\scripts\Secrets.local.ps1
# scripts\Secrets.local.ps1 の PDFHANDLER_PROD_SUPABASE_ANON_KEY を実値に置換
```

対話選択:

```powershell
.\scripts\build-release.ps1
```

PROD:

```powershell
.\scripts\build-release.ps1 -TargetEnvironment PROD
```

DEV:

```powershell
.\scripts\build-release.ps1 -TargetEnvironment DEV
```

インストーラ（任意・GitHub 公開時は PROD で実施）:

```powershell
.\tools\build-release.ps1 -TargetEnvironment PROD
```

`scripts/build-release.ps1` で配布フォルダと ZIP／チェックサムを生成し、`tools/build-release.ps1` で setup.exe と ZIP を `installer_output/` に揃えます。

## 起動ルール

- PROD テストは必ず `artifacts\release\prod\PdfHandler-<version>-win-x64\PdfHandler.UI.exe` から起動する。
- DEV テストは必ず `artifacts\release\dev\PdfHandler-<version>-win-x64\PdfHandler.UI.exe` から起動する。
- `src\...\bin\Debug` や `artifacts\run-build` などは開発・検証用であり、リリース判定には使わない。

## 既存の一時出力

以下は検証・一時実行の出力として扱い、正式配布物とは区別する。

```text
artifacts/run-build/
artifacts/verify-*/
artifacts/license-entry-run/
artifacts/prod-functions-inspect/
```

必要がなくなったら削除してよいが、削除前に未回収のログやスクリーンショットがないか確認する。

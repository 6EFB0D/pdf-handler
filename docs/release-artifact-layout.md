# Release Artifact Layout

PDF Handler の配布物・検証出力は、DEV / PROD とバージョンが混ざらないように以下のルールで扱う。

## 正式な配布候補

`scripts/build-release.ps1` で作成する。

```text
artifacts/
  release/
    prod/
      PdfHandler-<version>-win-x64/
        PdfHandler.UI.exe
        PdfHandler.runtime.json  # PROD Supabase
        README_RELEASE.txt
    dev/
      PdfHandler-<version>-win-x64/
        PdfHandler.UI.exe
        PdfHandler.runtime.json  # DEV Supabase
        README_RELEASE.txt
```

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

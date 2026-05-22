# scripts（公開リポジトリ）

| ファイル | 説明 |
|----------|------|
| `build-release.ps1` | `dotnet publish` で `artifacts/release/{dev|prod}/PdfHandler-<Version>-win-x64/` を生成。ビルド番号は `PDFHANDLER_BUILD_NUMBER` / `GITHUB_RUN_NUMBER` で上書き可能。詳細はリポジトリ直下の開発ドキュメント（開発用クローン側）を参照。 |
| `build.ps1` | 開発用ソリューションビルド |
| `Secrets.local.ps1.example` | ローカルの環境変数テンプレート。**このファイルから `Secrets.local.ps1` をコピーして使う**。`Secrets.local.ps1` は `.gitignore` 済み |

Inno Setup によるインストーラ生成は **`tools/build-release.ps1`** で行います。

**削除されたスクリプト（運用・デプロイ・ライセンス内部操作など）は、開発用リポジトリ（`*DEV*`）側で管理してください。**

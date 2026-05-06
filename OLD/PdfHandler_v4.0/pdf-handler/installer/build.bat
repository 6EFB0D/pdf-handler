@echo off
chcp 65001 > nul
echo ========================================
echo PDFハンドラ インストーラービルド
echo ========================================
echo.

REM WiX Toolsetのパス
set WIX=C:\Program Files (x86)\WiX Toolset v3.11\bin

REM ソースディレクトリ（リリースビルドの出力先）
set SOURCE=..\src\PdfHandler.UI\bin\Release\net6.0-windows\win-x64\publish

REM 出力ディレクトリ
set OUTPUT=..\output

echo [1/4] ビルド環境チェック...
echo.

REM WiX Toolsetの確認
if not exist "%WIX%\candle.exe" (
    echo ❌ エラー: WiX Toolsetが見つかりません
    echo.
    echo インストール先を確認してください:
    echo %WIX%
    echo.
    echo WiX Toolsetをインストールするには:
    echo https://wixtoolset.org/
    echo.
    pause
    exit /b 1
)
echo ✅ WiX Toolset: OK

REM リリースビルドの確認
if not exist "%SOURCE%\PdfHandler.UI.exe" (
    echo ❌ エラー: リリースビルドが見つかりません
    echo.
    echo 先に以下を実行してください:
    echo cd src\PdfHandler.UI
    echo dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=false
    echo.
    pause
    exit /b 1
)
echo ✅ リリースビルド: OK

REM アイコンファイルの確認
if not exist "PdfHandler.ico" (
    echo ⚠️  警告: PdfHandler.icoが見つかりません
    echo デフォルトアイコンが使用されます
    echo.
) else (
    echo ✅ アイコンファイル: OK
)

echo.
echo [2/4] 作業ディレクトリ準備...
if not exist "obj" mkdir obj
if not exist "%OUTPUT%" mkdir "%OUTPUT%"
echo ✅ 完了

echo.
echo [3/4] WiXコンパイル中...
echo.
"%WIX%\candle.exe" Product.wxs -dSourceDir="%SOURCE%" -out obj\Product.wixobj
if errorlevel 1 (
    echo.
    echo ❌ エラー: コンパイルに失敗しました
    echo.
    pause
    exit /b 1
)
echo ✅ コンパイル成功

echo.
echo [4/4] インストーラー作成中...
echo.
"%WIX%\light.exe" obj\Product.wixobj -out "%OUTPUT%\PdfHandler_v4.0.0.msi" -ext WixUIExtension -sval
if errorlevel 1 (
    echo.
    echo ❌ エラー: リンクに失敗しました
    echo.
    pause
    exit /b 1
)

echo.
echo ========================================
echo ✅ ビルド成功！
echo ========================================
echo.
echo インストーラー: %OUTPUT%\PdfHandler_v4.0.0.msi
echo.
echo 次の手順:
echo 1. インストーラーをテストしてください
echo 2. 問題がなければGitHub Releasesにアップロード
echo.
pause

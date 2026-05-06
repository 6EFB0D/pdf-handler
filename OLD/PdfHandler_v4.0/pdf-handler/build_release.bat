@echo off
REM ========================================
REM PDF Handler - Release Build Script
REM Version: 3.4.2
REM Date: 2025-12-27
REM ========================================

set VERSION=3.4.2
set OUTPUT_DIR=PdfHandler_v%VERSION%

echo.
echo ========================================
echo  PDF Handler v%VERSION% Build
echo ========================================
echo.

echo [Step 1/5] Clean...
dotnet clean PdfHandler.sln -c Release
if %errorlevel% neq 0 goto error

echo.
echo [Step 2/5] Build Release...
dotnet publish src\PdfHandler.UI\PdfHandler.UI.csproj -c Release -r win-x64 --self-contained true
if %errorlevel% neq 0 goto error

echo.
echo [Step 3/5] Create distribution folder...
if exist %OUTPUT_DIR% rmdir /s /q %OUTPUT_DIR%
mkdir %OUTPUT_DIR%

echo Copying application files...
xcopy /s /y src\PdfHandler.UI\bin\Release\net8.0-windows\win-x64\publish\* %OUTPUT_DIR%\

echo.
echo [Step 4/5] Copy documentation files...
if exist README.txt (
    copy README.txt %OUTPUT_DIR%\
    echo   - README.txt copied
) else (
    echo   WARNING: README.txt not found
)

if exist LICENSE.txt (
    copy LICENSE.txt %OUTPUT_DIR%\
    echo   - LICENSE.txt copied
) else (
    echo   WARNING: LICENSE.txt not found
)

echo.
echo [Step 5/5] Create ZIP file...
if exist PdfHandler_v%VERSION%.zip del PdfHandler_v%VERSION%.zip
powershell Compress-Archive -Path %OUTPUT_DIR% -DestinationPath PdfHandler_v%VERSION%.zip

echo.
echo ========================================
echo  Build completed successfully!
echo ========================================
echo.
echo Output files:
echo   - Folder: %OUTPUT_DIR%\
echo   - ZIP:    PdfHandler_v%VERSION%.zip
echo.
echo Contents:
echo   - Application files
if exist %OUTPUT_DIR%\README.txt echo   - README.txt
if exist %OUTPUT_DIR%\LICENSE.txt echo   - LICENSE.txt
echo.
goto end

:error
echo.
echo ========================================
echo  ERROR!
echo ========================================
pause
exit /b 1

:end
echo Press any key to exit...
pause >nul

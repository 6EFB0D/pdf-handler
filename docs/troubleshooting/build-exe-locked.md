# ビルド時の exe ロックエラー（MSB3021）

## 現象

ビルド時に次のエラーが発生する：

```
error MSB3021: ファイル "..." を "bin\Debug\net8.0-windows\PdfHandler.UI.exe" にコピーできません。
Access to the path '...\PdfHandler.UI.exe' is denied.
```

## ロックされるファイル

- **Debug ビルド**: `src\PdfHandler.UI\bin\Debug\net8.0-windows\PdfHandler.UI.exe`
- **Release ビルド**: `src\PdfHandler.UI\bin\Release\net8.0-windows\PdfHandler.UI.exe`

開発時は通常 Debug フォルダの exe を実行するため、ビルドも同じ exe を上書きしようとしてロックされます。

## 原因

PDFハンドラ（PdfHandler.UI.exe）が起動中のまま、同じ exe を上書きしようとしているため。Windows は実行中の exe をロックする。IDE のデバッガーから実行した場合も同様。

## 解決方法

### 1. Cursor / Visual Studio のデバッグを停止
デバッガーから実行している場合、**「実行の停止」ボタンでプロセスを止めてから**ビルドしてください。ウィンドウを閉じただけではプロセスが残ることがあります。

### 2. 推奨: ビルドスクリプトを使用

`scripts/build.ps1` を実行すると、実行中のプロセスを自動終了してからビルドする：

```powershell
cd D:\Users\admin_mak\project\pdf-handler\scripts
.\build.ps1
```

### 手動でプロセスを終了する場合

```powershell
Get-Process -Name "PdfHandler.UI" -ErrorAction SilentlyContinue | Stop-Process -Force
```

その後、`dotnet build` を実行。

## build.ps1 でも解決しない場合

1. **IDE を閉じてからビルド** - Cursor / Visual Studio のデバッガーがプロセスを保持している場合
2. **タスクマネージャーで確認** - 「PdfHandler.UI」や「dotnet」が残っていないか確認し、終了
3. **別ターミナルでプロセス終了**:
   ```powershell
   Get-Process -Name "PdfHandler*" -ErrorAction SilentlyContinue | Stop-Process -Force
   Start-Sleep -Seconds 3
   dotnet build src\PdfHandler.UI\PdfHandler.UI.csproj
   ```
4. **ロック元の特定** - 下記「Sysinternals Handle での調査」を参照

## Sysinternals Handle での調査

どのプロセスが exe をロックしているか特定する手順です。

### 手順

1. **Handle のダウンロード**
   - https://learn.microsoft.com/sysinternals/downloads/handle にアクセス
   - 「Download」から `handle.zip` をダウンロード・解凍

2. **管理者として実行**
   - コマンドプロンプトまたは PowerShell を**管理者として**起動
   - Handle を解凍したフォルダに移動、またはフルパスで実行

3. **ロック元の検索**
   ```cmd
   handle.exe PdfHandler.UI.exe
   ```
   または exe のフルパスで:
   ```cmd
   handle.exe "D:\Users\admin_mak\project\pdf-handler\src\PdfHandler.UI\bin\Debug\net8.0-windows\PdfHandler.UI.exe"
   ```

4. **結果の見方**
   - 出力例: `cursor.exe  pid: 12345  type: File  ...\PdfHandler.UI.exe`
   - `pid` がロックしているプロセスの ID
   - タスクマネージャーで「PID」列を表示し、該当プロセスを特定して終了

## 販売後の影響について

- **エンドユーザーには直接影響しません**。ユーザーはインストーラーでインストールし、ビルドは行いません。
- アップデート時は、インストーラーが実行中のアプリを終了してから上書きするのが一般的です。
- 本アプリでは **単一インスタンス制限** を実装しています。二重起動を防ぐことで、意図せず複数プロセスが残るケースを減らします。

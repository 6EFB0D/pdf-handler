; Inno Setup script for PDF Handler installer
; Build example:
;   .\tools\build-release.ps1 -TargetEnvironment PROD

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\artifacts\release\prod\PdfHandler-0.0.0-win-x64"
#endif
#ifndef OutputDir
  #define OutputDir "..\installer_output"
#endif
#ifndef TargetEnvironment
  #define TargetEnvironment "PROD"
#endif
#ifndef TargetTag
  #define TargetTag "prod"
#endif

#define AppPayloadDir "app"

[Setup]
AppId={{2BB6690A-F032-42D5-84A2-839A0BE35B12}
AppName=PDFハンドラ
AppVersion={#MyAppVersion}
AppPublisher=Office Go Plan
DefaultDirName={localappdata}\Office Go Plan\PDFハンドラ
DefaultGroupName=PDFハンドラ
OutputDir={#OutputDir}
OutputBaseFilename=PdfHandler-{#MyAppVersion}-{#TargetTag}-setup
SetupIconFile=PdfHandler.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
; アプリ側 Mutex（SingleInstanceCoordinator.MutexName）と一致
AppMutex=Goplan.PDFHandler.SingleInstance
; 終了処理は [Code] で実施（CloseApplications の MoveFile エラーダイアログを避ける）
RestartApplications=no
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\{#AppPayloadDir}\PdfHandler.UI.exe

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Tasks]
Name: "desktopicon"; Description: "デスクトップにショートカットを作成する"; GroupDescription: "追加アイコン:"

[Files]
; v1.1.4+: ペイロードは {app}\app\ 配下（旧レイアウトのロック済み root exe と衝突しない）
Source: "{#SourceDir}\*"; DestDir: "{app}\{#AppPayloadDir}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\PDFハンドラ"; Filename: "{app}\{#AppPayloadDir}\PdfHandler.UI.exe"; WorkingDir: "{app}\{#AppPayloadDir}"
Name: "{group}\インストールフォルダを開く"; Filename: "{sys}\explorer.exe"; Parameters: """{app}\{#AppPayloadDir}"""
Name: "{group}\アンインストール"; Filename: "{uninstallexe}"
Name: "{autodesktop}\PDFハンドラ"; Filename: "{app}\{#AppPayloadDir}\PdfHandler.UI.exe"; WorkingDir: "{app}\{#AppPayloadDir}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppPayloadDir}\PdfHandler.UI.exe"; Description: "PDFハンドラを起動する"; Flags: nowait postinstall skipifsilent

[Code]
const
  PdfHandlerExeName = 'PdfHandler.UI.exe';
  PdfHandlerMutexName = 'Goplan.PDFHandler.SingleInstance';
  AppPayloadDirName = '{#AppPayloadDir}';

function AppPayloadPath(): String;
begin
  Result := ExpandConstant('{app}\' + AppPayloadDirName);
end;

function AppExePath(): String;
begin
  Result := AppPayloadPath() + '\' + PdfHandlerExeName;
end;

function LegacyRootExePath(): String;
begin
  Result := ExpandConstant('{app}\' + PdfHandlerExeName);
end;

function IsPdfHandlerRunning(): Boolean;
var
  ResultCode: Integer;
begin
  if Exec(ExpandConstant('{sys}\cmd.exe'),
    '/C tasklist /FI "IMAGENAME eq ' + PdfHandlerExeName + '" /NH | find /I "' + PdfHandlerExeName + '"',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    Result := (ResultCode = 0)
  else
    Result := False;

  if not Result then
    Result := CheckForMutexes(PdfHandlerMutexName);
end;

function TryTerminatePdfHandler(const MaxAttempts: Integer): Boolean;
var
  I, ResultCode: Integer;
begin
  for I := 1 to MaxAttempts do
  begin
    Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM ' + PdfHandlerExeName + ' /T',
         '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(1200);
    if not IsPdfHandlerRunning() then
    begin
      Result := True;
      Exit;
    end;
  end;
  Result := not IsPdfHandlerRunning();
end;

procedure TrySilentUninstall();
var
  Uninstaller: String;
  ResultCode: Integer;
begin
  Uninstaller := ExpandConstant('{uninstallexe}');
  if (Uninstaller <> '') and FileExists(Uninstaller) then
  begin
    Exec(Uninstaller, '/VERYSILENT /NORESTART /SUPPRESSMSGBOXES', '',
         SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(2000);
  end;

  Uninstaller := ExpandConstant('{app}\unins000.exe');
  if FileExists(Uninstaller) then
  begin
    Exec(Uninstaller, '/VERYSILENT /NORESTART /SUPPRESSMSGBOXES', '',
         SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(2000);
  end;
end;

procedure TryDeleteLegacyRootExe();
var
  I: Integer;
  LegacyExe, BackupExe: String;
begin
  LegacyExe := LegacyRootExePath();
  if not FileExists(LegacyExe) then
    Exit;

  BackupExe := LegacyExe + '.old';
  for I := 1 to 8 do
  begin
    if DeleteFile(LegacyExe) then
      Exit;
    if RenameFile(LegacyExe, BackupExe) then
      Exit;
    Sleep(800);
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  HadRunningApp: Boolean;
begin
  Result := '';
  NeedsRestart := False;
  HadRunningApp := IsPdfHandlerRunning();

  if HadRunningApp then
  begin
    if MsgBox(
      'PDFハンドラが起動中です。' + #13#10 + #13#10 +
      'インストールを続行するため、自動的に終了します。' + #13#10 +
      '未保存の作業がある場合は「キャンセル」を押し、先にアプリを終了してください。',
      mbInformation, MB_OKCANCEL) = IDCANCEL then
    begin
      Result := 'PDFハンドラが起動中のため、インストールを中断しました。';
      Exit;
    end;
  end;

  TryTerminatePdfHandler(6);
  TrySilentUninstall();
  TryTerminatePdfHandler(3);
  TryDeleteLegacyRootExe();

  if IsPdfHandlerRunning() then
  begin
    Result :=
      'PDFハンドラを終了できませんでした。' + #13#10 + #13#10 +
      'タスクマネージャーで「PdfHandler.UI」を終了してから、再度インストールしてください。';
    Exit;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
  begin
    TryTerminatePdfHandler(2);
    TryDeleteLegacyRootExe();
  end;
end;

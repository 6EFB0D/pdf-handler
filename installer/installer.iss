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
CloseApplications=force
CloseApplicationsFilter=PdfHandler.UI.exe
RestartApplications=no
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\PdfHandler.UI.exe

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Tasks]
Name: "desktopicon"; Description: "デスクトップにショートカットを作成する"; GroupDescription: "追加アイコン:"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\PDFハンドラ"; Filename: "{app}\PdfHandler.UI.exe"; WorkingDir: "{app}"
Name: "{group}\インストールフォルダを開く"; Filename: "{sys}\explorer.exe"; Parameters: """{app}"""
Name: "{group}\アンインストール"; Filename: "{uninstallexe}"
Name: "{autodesktop}\PDFハンドラ"; Filename: "{app}\PdfHandler.UI.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\PdfHandler.UI.exe"; Description: "PDFハンドラを起動する"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
  OldExe: String;
begin
  if CurStep = ssInstall then
  begin
    // 1. プロセスを強制終了
    Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM PdfHandler.UI.exe', '',
         SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(2000);

    // 2. 旧 EXE を直接削除して MoveFile エラーを回避
    OldExe := ExpandConstant('{app}\PdfHandler.UI.exe');
    if FileExists(OldExe) then
      DeleteFile(OldExe);

    Sleep(500);
  end;
end;

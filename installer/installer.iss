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
DefaultGroupName=Office Go Plan\PDFハンドラ
OutputDir={#OutputDir}
OutputBaseFilename=PdfHandler-{#MyAppVersion}-{#TargetTag}-setup
SetupIconFile=PdfHandler.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
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

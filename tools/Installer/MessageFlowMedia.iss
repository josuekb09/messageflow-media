; Prefer the repository-root messageflow.iss for new church installers.
; Compile with: powershell -File tools\Installer\build-installer.ps1
;
; This script remains for historical release folders that pass ReleaseDir
; and InstallerOutputDir to ISCC.

#define MyAppName "MessageFlow Media"
#define MyAppVersion "1.0.3"
#define MyAppPublisher "MessageFlow Media"
#define MyAppExeName "MessageFlow.App.exe"

#ifndef ReleaseDir
  #error ReleaseDir must be supplied to ISCC, for example /DReleaseDir="D:\MessageFlow Release\RC-YYYYMMDD-HHMMSS\MessageFlow"
#endif
#ifndef InstallerOutputDir
  #error InstallerOutputDir must be supplied to ISCC.
#endif

[Setup]
AppId={{9B80F0B7-51E2-4B42-9E0F-4E0D5C4F6B91}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppCopyright=Copyright (c) 2026 MessageFlow Media project author. Distributed free of charge. Not for sale.
DefaultDirName=D:\MessageFlowMedia
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir={#InstallerOutputDir}
OutputBaseFilename=MessageFlowMediaSetup
InfoBeforeFile={#ReleaseDir}\README_CHURCH_INSTALL.txt
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupIconFile={#SourcePath}\..\..\src\MessageFlow.App\Assets\Brand\MessageFlow.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: checkedonce

[Files]
Source: "{#ReleaseDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\MessageFlow Media"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{userdesktop}\MessageFlow Media"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch MessageFlow Media"; Flags: nowait postinstall skipifsilent

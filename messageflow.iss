; MessageFlow Media installer
; Copyright (c) 2026 MessageFlow Media project author.
; Distributed free of charge for church use. Not for sale.
;
; Compile from the repository root after publishing the app:
;   powershell -File tools\Installer\build-installer.ps1
; or:
;   ISCC.exe messageflow.iss

#define MyAppName "MessageFlow Media"
#define MyAppVersion "1.0.3"
#define MyAppPublisher "MessageFlow Media"
#define MyAppExeName "MessageFlow.App.exe"

#ifndef RepoRoot
  #define RepoRoot AddBackslash(SourcePath)
#endif
#ifndef PublishDir
  #define PublishDir RepoRoot + "dist\publish"
#endif
#ifndef DatabaseFile
  #define DatabaseFile RepoRoot + "database\messageflow.db"
#endif
#ifndef OutputDir
  #define OutputDir RepoRoot + "dist"
#endif
#ifndef NoticeFile
  #define NoticeFile RepoRoot + "tools\Installer\CHURCH_NOTICE.txt"
#endif
#ifndef SetupIconFile
  #define SetupIconFile RepoRoot + "src\MessageFlow.App\Assets\Brand\MessageFlow.ico"
#endif

[Setup]
AppId={{9B80F0B7-51E2-4B42-9E0F-4E0D5C4F6B91}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppCopyright=Copyright (c) 2026 MessageFlow Media project author. Distributed free of charge. Not for sale.
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=MessageFlow Media installer
VersionInfoCopyright=Copyright (c) 2026 MessageFlow Media project author. Distributed free of charge. Not for sale.
DefaultDirName=D:\MessageFlowMedia
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
DisableDirPage=no
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir={#OutputDir}
OutputBaseFilename=MessageFlowMediaSetup
SetupIconFile={#SetupIconFile}
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
InfoBeforeFile={#NoticeFile}
Compression=lzma2/fast
SolidCompression=no
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
ChangesAssociations=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: checkedonce

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb,\database\*"
Source: "{#DatabaseFile}"; DestDir: "{app}\database"; DestName: "messageflow.db"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch MessageFlow Media"; Flags: nowait postinstall skipifsilent

#define MyAppName "MessageFlow Media"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "MessageFlow Media"
#define MyAppExeName "MessageFlow.App.exe"

[Setup]
AppId={{9B80F0B7-51E2-4B42-9E0F-4E0D5C4F6B91}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppCopyright=Copyright (c) 2026 MessageFlow Media project author. Distributed free of charge. Not for sale.
DefaultDirName={localappdata}\MessageFlow Media
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=D:\MessageFlow Release\Installer
OutputBaseFilename=MessageFlowMediaSetup
InfoBeforeFile=D:\MessageFlow Release\MessageFlow\README_CHURCH_INSTALL.txt
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: checkedonce

[Files]
Source: "D:\MessageFlow Release\MessageFlow\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\MessageFlow Media"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{userdesktop}\MessageFlow Media"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch MessageFlow Media"; Flags: nowait postinstall skipifsilent

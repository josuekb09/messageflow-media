; MessageFlow Media installer
; Copyright (c) 2026 MessageFlow Media project author.
; Distributed free of charge for church use. Not for sale.
;
; Compile from the repository root after publishing the app:
;   powershell -File tools\Installer\build-installer.ps1
; or:
;   ISCC.exe messageflow.iss

#define MyAppName "MessageFlow Media"
#define MyAppVersion "1.0.8"
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
AppPublisherURL=https://www.messageflow.tech
AppSupportURL=https://www.messageflow.tech/feedback
AppUpdatesURL=https://www.messageflow.tech/download
AppCopyright=Copyright (c) 2026 MessageFlow Media project author. Distributed free of charge. Not for sale.
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoProductName={#MyAppName}
VersionInfoDescription=MessageFlow Media installer
VersionInfoCopyright=Copyright (c) 2026 MessageFlow Media project author. Distributed free of charge. Not for sale.
; Per-user and never synced by OneDrive. Documents is often redirected into OneDrive, and
; OneDrive can turn the 2+ GB database into a cloud-only placeholder or lock it while syncing,
; which froze MessageFlow at launch.
DefaultDirName={localappdata}\Programs\MessageFlowMedia
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
DisableDirPage=no
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
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

[Code]
function IsOneDrivePath(const Path: String): Boolean;
var
  Lower: String;
begin
  Lower := Lowercase(AddBackslash(Path));
  Result := (Pos('\onedrive\', Lower) > 0) or (Pos('\onedrive - ', Lower) > 0);
  if not Result and (GetEnv('OneDrive') <> '') then
    Result := Pos(Lowercase(AddBackslash(GetEnv('OneDrive'))), Lower) = 1;
  if not Result and (GetEnv('OneDriveCommercial') <> '') then
    Result := Pos(Lowercase(AddBackslash(GetEnv('OneDriveCommercial'))), Lower) = 1;
  if not Result and (GetEnv('OneDriveConsumer') <> '') then
    Result := Pos(Lowercase(AddBackslash(GetEnv('OneDriveConsumer'))), Lower) = 1;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  SafeDir: String;
begin
  Result := True;
  if (CurPageID = wpSelectDir) and IsOneDrivePath(WizardDirValue) then
  begin
    SafeDir := ExpandConstant('{localappdata}\Programs\MessageFlowMedia');
    if MsgBox('The selected folder is synchronized by OneDrive:' + #13#10#13#10 + WizardDirValue + #13#10#13#10 +
              'OneDrive can make the large MessageFlow library slow to open or unavailable during a service.' + #13#10#13#10 +
              'Install to this safe local folder instead?' + #13#10#13#10 + SafeDir,
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      WizardForm.DirEdit.Text := SafeDir;
      Result := False;
    end;
  end;
end;

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
; The library is over 2 GB, so the suggested folder is on the local data drive with the most
; free space (often E: on church PCs whose C: is nearly full), never in Documents: Documents is
; often redirected into OneDrive, which can keep the database cloud-only or lock it while syncing
; and froze MessageFlow at launch. See DefaultInstallDir below.
DefaultDirName={code:DefaultInstallDir}
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
const
  DRIVE_REMOVABLE = 2;
  DRIVE_FIXED = 3;
  MinimumFreeMB = 5120;

function GetDriveType(lpRootPathName: String): UINT;
  external 'GetDriveTypeW@kernel32.dll stdcall';

{ Non-system local drive (internal or external SSD) with the most free space, if it has room for
  the library; otherwise the per-user local folder on the system drive. }
function DefaultInstallDir(Param: String): String;
var
  Letter: Char;
  Root, SystemDrive: String;
  FreeMB, TotalMB, BestFreeMB: Cardinal;
begin
  Result := ExpandConstant('{localappdata}\Programs\MessageFlowMedia');
  SystemDrive := Uppercase(ExtractFileDrive(ExpandConstant('{win}')));
  BestFreeMB := 0;
  for Letter := 'C' to 'Z' do
  begin
    Root := Letter + ':\';
    if (Uppercase(Letter + ':') <> SystemDrive) and DirExists(Root) and
       ((GetDriveType(Root) = DRIVE_FIXED) or (GetDriveType(Root) = DRIVE_REMOVABLE)) and
       GetSpaceOnDisk(Root, True, FreeMB, TotalMB) and
       (FreeMB >= MinimumFreeMB) and (FreeMB > BestFreeMB) then
    begin
      BestFreeMB := FreeMB;
      Result := Root + 'MessageFlowMedia';
    end;
  end;
end;

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
    SafeDir := DefaultInstallDir('');
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

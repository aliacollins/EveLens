; EVEMon Installer Script for Inno Setup 6.x
; Download Inno Setup from: https://jrsoftware.org/isinfo.php
;
; Build with: iscc /DMyAppVersion=5.2.0 EVEMon.iss
; Or use: build-installer.ps1

#ifndef MyAppVersion
  #define MyAppVersion "5.2.0"
#endif

#define MyAppName "EVEMon"
#define MyAppPublisher "Alia Collins"
#define MyAppURL "https://github.com/aliacollins/evemon"
#define MyAppExeName "EVEMon NexT.exe"

; .NET 8.0 Runtime download URL (x64) - standard runtime, not Windows Desktop
#define DotNet8DownloadUrl "https://download.visualstudio.microsoft.com/download/pr/76e5dbb2-6ae3-4629-9a84-527f8571571b/09002599b32d5d01dc3aa5ef68b5e2ae/dotnet-runtime-8.0.11-win-x64.exe"
#define DotNet8InstallerFile "dotnet-runtime-8.0.11-win-x64.exe"

[Setup]
AppId={{8B3D3C6F-5A7E-4B9A-9D5C-3F2A1B4C5D6E}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=..\src\EVEMon.Common\Resources\License\gpl.txt
; Output to publish folder with name matching UpdateManager pattern
OutputDir=..\publish
OutputBaseFilename=EVEMon-install-{#MyAppVersion}
SetupIconFile=..\src\EVEMon.Avalonia\Properties\EVEMon.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
; Allow non-admin install to user's local app data
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
; Auto-update support: close running EVEMon before installing
CloseApplications=force
CloseApplicationsFilter=EVEMon NexT.exe
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
; For interactive installs - show checkbox to launch
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
; For silent installs (auto-update) - always launch after install
Filename: "{app}\{#MyAppExeName}"; Flags: nowait postinstall skipifnotsilent runasoriginaluser

[Code]
var
  DownloadPage: TDownloadWizardPage;
  ForkNoticePage: TOutputMsgMemoWizardPage;
  DotNetRequired: Boolean;
  OldSettingsBackedUp: Boolean;
  SettingsBackupPath: String;

function IsAlphaVersion: Boolean;
begin
  Result := Pos('alpha', LowerCase('{#MyAppVersion}')) > 0;
end;

function IsBetaVersion: Boolean;
begin
  Result := Pos('beta', LowerCase('{#MyAppVersion}')) > 0;
end;

function GetForkNoticeText: String;
var
  S: String;
begin
  S := 'You are installing EVEMon v{#MyAppVersion}' + #13#10;
  S := S + '(Alia Collins Fork - Avalonia Cross-Platform)' + #13#10#13#10;

  // Add warning for alpha/beta
  if IsAlphaVersion then
  begin
    S := S + '>>> ALPHA BUILD <<<' + #13#10;
    S := S + 'This version is unstable and may contain bugs.' + #13#10;
    S := S + 'Use the stable release for everyday use.' + #13#10#13#10;
  end
  else if IsBetaVersion then
  begin
    S := S + '>>> BETA BUILD <<<' + #13#10;
    S := S + 'This version is for testing new features.' + #13#10;
    S := S + 'It may contain bugs. Report issues on GitHub.' + #13#10#13#10;
  end;

  S := S + #$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500 + #13#10#13#10;

  S := S + 'This version is built on peterhaneve''s EVEMon and is now' + #13#10;
  S := S + 'maintained by Alia Collins.' + #13#10#13#10;

  S := S + 'GitHub: https://github.com/aliacollins/evemon' + #13#10#13#10;

  S := S + #$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500 + #13#10#13#10;

  S := S + 'YOUR DATA' + #13#10#13#10;

  S := S + '  Skill plans will be preserved' + #13#10;
  S := S + '  Settings will be preserved' + #13#10;
  S := S + '  Characters will need to be re-added (ESI login required)' + #13#10#13#10;

  S := S + 'If you have an older EVEMon installation, your settings' + #13#10;
  S := S + 'folder will be backed up automatically just in case.' + #13#10#13#10;

  S := S + #$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500#$2500 + #13#10#13#10;

  S := S + 'CROSS-PLATFORM' + #13#10#13#10;

  S := S + 'EVEMon now runs on Windows, Linux, and macOS!' + #13#10;
  S := S + 'Linux/macOS users: download the portable ZIP from GitHub.' + #13#10#13#10;

  S := S + 'SUPPORT THE PROJECT' + #13#10#13#10;

  S := S + 'I don''t accept donations. If you want to support EVEMon,' + #13#10;
  S := S + 'please donate to Peter Han or the original EVEMonDevTeam' + #13#10;
  S := S + 'who built this tool over many years.';

  Result := S;
end;

function BackupOldSettings: Boolean;
var
  EVEMonAppData: String;
  BackupZip: String;
  Timestamp: String;
  ResultCode: Integer;
begin
  Result := False;
  EVEMonAppData := ExpandConstant('{userappdata}\EVEMon');

  if DirExists(EVEMonAppData) then
  begin
    // Create backup filename with timestamp
    Timestamp := GetDateTimeString('yyyy-mm-dd_hhnnss', '-', ':');
    BackupZip := ExpandConstant('{userappdata}\EVEMon-backup-') + Timestamp + '.zip';

    Log('Backing up EVEMon settings from: ' + EVEMonAppData);
    Log('Backup destination: ' + BackupZip);

    // Use PowerShell to create zip
    if Exec('powershell.exe',
            '-NoProfile -ExecutionPolicy Bypass -Command "Compress-Archive -Path ''' +
            EVEMonAppData + '\*'' -DestinationPath ''' + BackupZip + ''' -Force"',
            '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    begin
      if (ResultCode = 0) and FileExists(BackupZip) then
      begin
        Result := True;
        SettingsBackupPath := BackupZip;
        OldSettingsBackedUp := True;
        Log('Settings backup successful: ' + BackupZip);
      end
      else
        Log('Settings backup failed with code: ' + IntToStr(ResultCode));
    end
    else
      Log('Failed to execute PowerShell for backup');
  end
  else
    Log('No existing EVEMon settings folder found');
end;

function IsDotNet8Installed: Boolean;
var
  ResultCode: Integer;
  TempFile: String;
  Output: AnsiString;
begin
  Result := False;
  TempFile := ExpandConstant('{tmp}\dotnet_check.txt');

  // Check for .NET 8 runtime (not Windows Desktop - we're Avalonia now)
  if Exec('cmd.exe', '/c dotnet --list-runtimes 2>nul | findstr /C:"Microsoft.NETCore.App 8." > "' + TempFile + '"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    if LoadStringFromFile(TempFile, Output) then
    begin
      Result := Length(Output) > 0;
    end;
    DeleteFile(TempFile);
  end;
end;

function OnDownloadProgress(const Url, FileName: String; const Progress, ProgressMax: Int64): Boolean;
begin
  if Progress = ProgressMax then
    Log(Format('Successfully downloaded file to {tmp}: %s', [FileName]));
  Result := True;
end;

procedure InitializeWizard;
begin
  // Create the fork notice page (shown after Welcome, before License)
  ForkNoticePage := CreateOutputMsgMemoPage(wpWelcome,
    'About This Version',
    'Please read the following important information before continuing.',
    'When you are ready to continue with Setup, click Next.',
    GetForkNoticeText);

  // Create the download page for .NET runtime
  DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), SetupMessage(msgPreparingDesc), @OnDownloadProgress);
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  ResultCode: Integer;
  DotNetInstaller: String;
begin
  Result := True;

  // Backup settings when leaving the directory selection page
  if CurPageID = wpSelectDir then
  begin
    if not OldSettingsBackedUp then
      BackupOldSettings;
  end;

  if CurPageID = wpReady then
  begin
    // Check if .NET 8 Runtime is installed
    if not IsDotNet8Installed then
    begin
      DotNetRequired := True;

      if MsgBox('EVEMon requires .NET 8.0 Runtime which is not installed.'#13#10#13#10 +
                'Would you like to download and install it now?'#13#10#13#10 +
                '(Size: approximately 30 MB)', mbConfirmation, MB_YESNO) = IDYES then
      begin
        DownloadPage.Clear;
        DownloadPage.Add('{#DotNet8DownloadUrl}', '{#DotNet8InstallerFile}', '');
        DownloadPage.Show;
        try
          try
            DownloadPage.Download;

            // Run the .NET installer
            DotNetInstaller := ExpandConstant('{tmp}\{#DotNet8InstallerFile}');
            if FileExists(DotNetInstaller) then
            begin
              Log('Running .NET 8 Runtime installer...');
              // /quiet for silent install, /norestart to prevent restart
              if not Exec(DotNetInstaller, '/install /quiet /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
              begin
                MsgBox('Failed to run .NET 8 installer. Error code: ' + IntToStr(ResultCode), mbError, MB_OK);
                Result := False;
              end
              else if ResultCode <> 0 then
              begin
                // ResultCode 3010 means success but restart required
                if ResultCode = 3010 then
                begin
                  MsgBox('.NET 8 Runtime installed successfully.'#13#10 +
                         'A system restart may be required after EVEMon installation.', mbInformation, MB_OK);
                end
                else
                begin
                  MsgBox('.NET 8 installer returned code: ' + IntToStr(ResultCode) + #13#10 +
                         'EVEMon may not run correctly.', mbError, MB_OK);
                end;
              end;
            end;
          except
            if DownloadPage.AbortedByUser then
              Log('Download aborted by user.')
            else
              MsgBox(AddPeriod(GetExceptionMessage), mbCriticalError, MB_OK);
            Result := False;
          end;
        finally
          DownloadPage.Hide;
        end;
      end
      else
      begin
        // User declined to download .NET
        Result := MsgBox('EVEMon will not run without .NET 8.0 Runtime.'#13#10#13#10 +
                         'You can download it later from:'#13#10 +
                         'https://dotnet.microsoft.com/download/dotnet/8.0'#13#10#13#10 +
                         'Continue installation anyway?', mbConfirmation, MB_YESNO) = IDYES;
      end;
    end;
  end;
end;

function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo, MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
var
  S: String;
begin
  S := '';

  // Show version being installed
  S := S + 'Version to install:' + NewLine;
  S := S + Space + '{#MyAppName} {#MyAppVersion}' + NewLine;
  if IsAlphaVersion then
    S := S + Space + '(Alpha - Unstable)' + NewLine
  else if IsBetaVersion then
    S := S + Space + '(Beta - Testing)' + NewLine;
  S := S + NewLine;

  // Show backup info if settings were backed up
  if OldSettingsBackedUp then
  begin
    S := S + 'Settings backup created:' + NewLine;
    S := S + Space + SettingsBackupPath + NewLine + NewLine;
  end;

  if DotNetRequired then
  begin
    S := S + 'Prerequisites to install:' + NewLine;
    S := S + Space + '.NET 8.0 Runtime' + NewLine + NewLine;
  end;

  if MemoDirInfo <> '' then
    S := S + MemoDirInfo + NewLine + NewLine;

  if MemoGroupInfo <> '' then
    S := S + MemoGroupInfo + NewLine + NewLine;

  if MemoTasksInfo <> '' then
    S := S + MemoTasksInfo + NewLine + NewLine;

  Result := S;
end;

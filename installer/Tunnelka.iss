#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\publish"
#endif

[Setup]
AppId={{6F1C2B7E-3D4A-4E8B-9C21-7A5D0E3F9B14}
AppName=Tunnelka
AppVersion={#AppVersion}
AppPublisher=Tunnelka
VersionInfoVersion={#AppVersion}
DefaultDirName={autopf}\Tunnelka
DisableProgramGroupPage=yes
DisableDirPage=yes
PrivilegesRequired=admin
OutputDir=..\dist
OutputBaseFilename=Tunnelka-Setup-{#AppVersion}
SetupIconFile=..\Assets\tunnelka.ico
UninstallDisplayIcon={app}\Tunnelka.exe
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
CloseApplications=force
RestartApplications=no

[Languages]
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "en"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
ru.Options=Дополнительно:
en.Options=Options:
ru.AutoStart=Запускать вместе с Windows
en.AutoStart=Start with Windows
ru.CleanCache=Очистить кэш (ключи и настройки сохранятся)
en.CleanCache=Clear cache (keys and settings are kept)

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "autostart"; Description: "{cm:AutoStart}"; GroupDescription: "{cm:Options}"; Flags: unchecked
Name: "cleancache"; Description: "{cm:CleanCache}"; GroupDescription: "{cm:Options}"; Flags: unchecked

[InstallDelete]
Type: files; Name: "{localappdata}\Tunnelka\config.json"; Tasks: cleancache
Type: files; Name: "{localappdata}\Tunnelka\tun.json"; Tasks: cleancache
Type: files; Name: "{localappdata}\Tunnelka\ping-*.json"; Tasks: cleancache
Type: files; Name: "{localappdata}\Tunnelka\crash.log"; Tasks: cleancache
Type: files; Name: "{localappdata}\Tunnelka\tunnelka.log*"; Tasks: cleancache
Type: files; Name: "{localappdata}\Tunnelka\relay*.json"; Tasks: cleancache
Type: files; Name: "{localappdata}\Tunnelka\*.broken"; Tasks: cleancache
Type: files; Name: "{localappdata}\Tunnelka\*.moved"; Tasks: cleancache
Type: files; Name: "{localappdata}\Tunnelka\*.tmp"; Tasks: cleancache
Type: files; Name: "{app}\core\*.json"; Tasks: cleancache

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Tunnelka"; ValueData: """{app}\Tunnelka.exe"" --minimized"; Flags: uninsdeletevalue; Tasks: autostart
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; ValueName: "Tunnelka"; Flags: deletevalue; Tasks: not autostart; Check: not WizardSilent

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Tunnelka"; Filename: "{app}\Tunnelka.exe"
Name: "{autodesktop}\Tunnelka"; Filename: "{app}\Tunnelka.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Tunnelka.exe"; Description: "{cm:LaunchProgram,Tunnelka}"; Flags: nowait postinstall runasoriginaluser

[UninstallRun]
Filename: "{sys}\taskkill.exe"; Parameters: "/F /T /IM Tunnelka.exe"; Flags: runhidden; RunOnceId: "StopApp"
Filename: "{app}\Tunnelka.exe"; Parameters: "--cleanup"; Flags: runhidden waituntilterminated; RunOnceId: "Cleanup"

[UninstallDelete]
Type: filesandordirs; Name: "{app}\core"
Type: filesandordirs; Name: "{commonappdata}\Tunnelka"

[Code]
const
  ServiceName = 'TunnelkaService';
  OldUninstallKey = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{6F1C2B7E-3D4A-4E8B-9C21-7A5D0E3F9B14}_is1';

function RunHidden(const FileName, Params: String): Integer;
var
  Code: Integer;
begin
  if not Exec(FileName, Params, '', SW_HIDE, ewWaitUntilTerminated, Code) then
    Code := -1;
  Result := Code;
end;

function Sc(const Params: String): Integer;
begin
  Result := RunHidden(ExpandConstant('{sys}\sc.exe'), Params);
end;

function ServiceExists: Boolean;
begin
  Result := Sc('query ' + ServiceName) = 0;
end;

procedure StopService;
begin
  if ServiceExists then
  begin
    RunHidden(ExpandConstant('{sys}\net.exe'), 'stop ' + ServiceName);
    RunHidden(ExpandConstant('{sys}\taskkill.exe'), '/F /T /IM Tunnelka.exe');
  end;
end;

procedure RemoveOldPerUserInstall;
var
  Command: String;
begin
  if RegQueryStringValue(HKCU, OldUninstallKey, 'UninstallString', Command) then
  begin
    Command := RemoveQuotes(Command);
    if FileExists(Command) then
      RunHidden(Command, '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART');
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  StopService;
  RemoveOldPerUserInstall;
  Result := '';
end;

procedure InstallService;
var
  BinPath, Data: String;
begin
  BinPath := '"' + ExpandConstant('{app}\Tunnelka.exe') + '" --service';
  if ServiceExists then
    Sc('config ' + ServiceName + ' binPath= "' + BinPath + '" start= auto obj= LocalSystem')
  else
    Sc('create ' + ServiceName + ' binPath= "' + BinPath + '" start= auto obj= LocalSystem DisplayName= "Tunnelka Service"');

  Sc('description ' + ServiceName + ' "Tunnelka: TUN mode and kill switch without running the app as administrator"');
  Sc('failure ' + ServiceName + ' reset= 86400 actions= restart/5000/restart/5000/none/0');

  Data := ExpandConstant('{commonappdata}\Tunnelka');
  ForceDirectories(Data);
  RegWriteMultiStringValue(HKLM, 'SYSTEM\CurrentControlSet\Services\' + ServiceName, 'Environment',
    'DOTNET_BUNDLE_EXTRACT_BASE_DIR=' + Data + '\runtime');
  RunHidden(ExpandConstant('{sys}\icacls.exe'), '"' + Data + '" /inheritance:r /grant:r *S-1-5-18:(OI)(CI)F *S-1-5-32-544:(OI)(CI)F');
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    InstallService;
    RunHidden(ExpandConstant('{sys}\net.exe'), 'start ' + ServiceName);
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
  begin
    RunHidden(ExpandConstant('{sys}\net.exe'), 'stop ' + ServiceName);
    Sc('delete ' + ServiceName);
  end;
end;

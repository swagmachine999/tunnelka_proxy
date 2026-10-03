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
AppPublisher=swagmachine999
VersionInfoVersion={#AppVersion}
DefaultDirName={localappdata}\Programs\Tunnelka
DisableProgramGroupPage=yes
DisableDirPage=yes
PrivilegesRequired=lowest
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
Filename: "{app}\Tunnelka.exe"; Description: "{cm:LaunchProgram,Tunnelka}"; Flags: nowait postinstall

[UninstallRun]
Filename: "{sys}\taskkill.exe"; Parameters: "/F /T /IM Tunnelka.exe"; Flags: runhidden; RunOnceId: "StopApp"
Filename: "{app}\Tunnelka.exe"; Parameters: "--cleanup"; Flags: runhidden waituntilterminated; RunOnceId: "Cleanup"

[UninstallDelete]
Type: filesandordirs; Name: "{app}\core"

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

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

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

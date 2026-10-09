; L0N Game Launcher – Inno Setup 6
; Build on a Windows PC after dotnet publish has generated ../publish
; This is a genuine native installer, but it is UNSIGNED until a signing certificate is supplied.
#define AppName "L0N Game Launcher"
#define AppVersion "0.1.0"
#define AppExe "L0N.GameLauncher.exe"

[Setup]
AppId={{D706A215-7DE0-4ED0-9E4F-93F9AB4CA0AA}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=L0N
DefaultDirName={autopf}\L0N Game Launcher
DefaultGroupName=L0N Game Launcher
UninstallDisplayIcon={app}\{#AppExe}
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\dist
OutputBaseFilename=L0N_Game_Launcher_Setup
DisableProgramGroupPage=yes
AllowNoIcons=yes
CloseApplications=yes
RestartApplications=yes
VersionInfoVersion=0.1.0.0
VersionInfoDescription=L0N Game Launcher Setup
WizardResizable=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\L0N Game Launcher"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\L0N Game Launcher"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "Launch L0N Game Launcher"; Flags: nowait postinstall skipifsilent

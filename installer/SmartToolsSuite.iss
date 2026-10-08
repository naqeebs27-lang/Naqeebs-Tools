#define MyAppName "Naqeebs Smart Tools"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Naqeebs Multi Services"
#define MyAppExeName "SmartToolsSuite.exe"

[Setup]
AppId={{B1C3D3FD-BAF5-4C0D-9D0D-7BCA7BC4F0D7}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Naqeebs Smart Tools
DefaultGroupName={#MyAppName}
OutputDir=..\artifacts
OutputBaseFilename=Naqeebs-Smart-Tools-win-x64-Setup
Compression=lzma
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}

[Files]
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: postinstall nowait skipifsilent

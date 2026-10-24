; =====================================================================
; LabScape - Enterprise Physical Floor Plan Layout Designer
; Inno Setup 6 Installation Script
; 100% Offline Single-File Administrator Setup
; =====================================================================

#define MyAppName "LabScape"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "LabScape Solutions"
#define MyAppURL "https://github.com/labscape/labscape"
#define MyAppExeName "LabScape.App.exe"
#define MyAppAssocName MyAppName + " Layout Document"
#define MyAppAssocExt ".layout"
#define MyAppAssocKey StringChange(MyAppAssocName, " ", "") + MyAppAssocExt

[Setup]
; Basic Application Info
AppId={{E7A23B94-8B51-4E65-9F7C-39D9B99E0F2B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Destination Directory (%ProgramFiles%\LabScape)
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes

; Output Configuration
OutputDir=..\dist\setup
OutputBaseFilename=LabScape-Setup-v{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes

; Enterprise / Administrator Privileges
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=commandline
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; UI and Visuals
WizardStyle=modern
DisableProgramGroupPage=auto

; Changes Environment & Associations
ChangesAssociations=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "fileassoc"; Description: "Associate .layout files with {#MyAppName}"; GroupDescription: "File Associations:"

[Files]
; All files from self-contained publish directory
Source: "..\dist\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; HTML5 2D Canvas Web Viewer
Source: "..\web-viewer\*"; DestDir: "{app}\web-viewer"; Flags: ignoreversion recursesubdirs createallsubdirs

[Registry]
; Register .layout file association
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocExt}\OpenWithProgids"; ValueType: string; ValueName: "{#MyAppAssocKey}"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}"; ValueType: string; ValueName: ""; ValueData: "{#MyAppAssocName}"; Flags: uninsdeletekey; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Tasks: fileassoc

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\LabScape Web Viewer"; Filename: "{app}\web-viewer\index.html"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

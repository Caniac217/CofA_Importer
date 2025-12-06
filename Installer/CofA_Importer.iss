; CofA Importer Installer Script
[Setup]
AppName=ICC CofA Importer
AppVersion=1.0
DefaultDirName={commonpf64}\INDOFINE\ICC CofA Importer
DefaultGroupName=ICC CofA Importer
OutputDir=Output
OutputBaseFilename=ICC_CofA_Importer_Setup
UninstallDisplayIcon={app}\CofA.ico
SetupIconFile=G:\Software Development\Visual Studio\COFA Solution\CofA_Importer\Installer\CofA.ico
Compression=lzma
SolidCompression=yes
DisableDirPage=yes
DisableProgramGroupPage=yes

[Files]
Source: "G:\Software Development\Visual Studio\COFA Solution\CofA_Importer\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "windowsdesktop-runtime-8.0.18-win-x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall
Source: "CofA.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\INDOFINE\ICC CofA Importer"; Filename: "{app}\CofA_Importer.exe"
Name: "{group}\INDOFINE\Uninstall CofA Importer"; Filename: "{uninstallexe}"
Name: "{commondesktop}\ICC CofA Importer"; Filename: "{app}\CofA_Importer.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"

[Run]
Filename: "{tmp}\windowsdesktop-runtime-8.0.18-win-x64.exe"; Parameters: "/install /quiet /norestart"; StatusMsg: "Installing .NET 8 Desktop Runtime..."; Check: NeedsDotNet8

[Code]
function NeedsDotNet8(): Boolean;
var
  netRegKey: string;
begin
  netRegKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\Microsoft .NET Runtime - 8.0.18 (x64)';
  Result := not RegKeyExists(HKEY_LOCAL_MACHINE, netRegKey);
end;
; Nythera Inno Setup Script
; This script creates a professional Windows Installer (Setup.exe) for Nythera

#define AppExe SourcePath + "Nythera\bin\Release\net10.0-windows10.0.26100.0\win-x64\publish\Nythera.exe"
#dim Version[4]
#expr GetVersionComponents(AppExe, Version[0], Version[1], Version[2], Version[3])
#define AppVer Str(Version[0]) + "." + Str(Version[1]) + "." + Str(Version[2])

[Setup]
; App Information
AppName=Nythera
AppVersion={#AppVer}
AppPublisher=Nythera Studios
AppPublisherURL=https://github.com/rzayevsahil/Nythera_Wallpaper
AppSupportURL=https://github.com/rzayevsahil/Nythera_Wallpaper
AppUpdatesURL=https://github.com/rzayevsahil/Nythera_Wallpaper

; Default Installation Folder (e.g. C:\Program Files\Nythera)
DefaultDirName={autopf}\Nythera

; Start Menu Folder
DefaultGroupName=Nythera

; Output Settings
OutputDir=.\Installer
OutputBaseFilename=Nythera_Setup_v{#AppVer}
SetupIconFile=.\Nythera\Assets\AppIcon.ico
UninstallDisplayIcon={app}\Nythera.exe

; Compression Settings (Makes the setup file smaller)
Compression=lzma2/ultra64
SolidCompression=yes

; Architecture Settings (64-bit app)
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Copy all published files from the publish folder to the installation directory
Source: ".\Nythera\bin\Release\net10.0-windows10.0.26100.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Create Start Menu Shortcut
Name: "{group}\Nythera"; Filename: "{app}\Nythera.exe"; IconFilename: "{app}\Nythera.exe"
; Create Desktop Shortcut (if user checked the box)
Name: "{autodesktop}\Nythera"; Filename: "{app}\Nythera.exe"; IconFilename: "{app}\Nythera.exe"; Tasks: desktopicon

[Run]
; Run the app automatically after installation finishes (manual install checkbox)
Filename: "{app}\Nythera.exe"; Description: "{cm:LaunchProgram,Nythera}"; Flags: nowait postinstall skipifsilent
; Run the app automatically after silent installation finishes
Filename: "{app}\Nythera.exe"; Flags: nowait; Check: IsAutoRestart

[UninstallRun]
; (Optional) Kill process before uninstalling if it is running
Filename: "{cmd}"; Parameters: "/C taskkill /F /IM Nythera.exe"; Flags: runhidden waituntilterminated

[Code]
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  // Kill the application silently before extracting files to prevent locking issues
  Exec(ExpandConstant('{cmd}'), '/c taskkill /F /IM Nythera.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := True;
end;

function IsAutoRestart: Boolean;
var
  I: Integer;
begin
  Result := False;
  for I := 1 to ParamCount do
  begin
    if CompareText(ParamStr(I), '/AUTORESTART') = 0 then
    begin
      Result := True;
      Exit;
    end;
  end;
end;

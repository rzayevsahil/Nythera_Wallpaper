; NoraWallpaper Inno Setup Script
; This script creates a professional Windows Installer (Setup.exe) for NoraWallpaper

[Setup]
; App Information
AppName=NoraWallpaper
AppVersion=1.0.0
AppPublisher=Nora Studios
AppPublisherURL=https://github.com/rzayevsahil/walpaper-for-pc
AppSupportURL=https://github.com/rzayevsahil/walpaper-for-pc
AppUpdatesURL=https://github.com/rzayevsahil/walpaper-for-pc

; Default Installation Folder (e.g. C:\Program Files\NoraWallpaper)
DefaultDirName={autopf}\NoraWallpaper

; Start Menu Folder
DefaultGroupName=NoraWallpaper

; Output Settings
OutputDir=.\Installer
OutputBaseFilename=NoraWallpaper_Setup_v1.0.0
SetupIconFile=.\NoraWallpaper\Assets\AppIcon.ico
UninstallDisplayIcon={app}\NoraWallpaper.exe

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
Source: ".\NoraWallpaper\bin\Release\net10.0-windows10.0.26100.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Create Start Menu Shortcut
Name: "{group}\NoraWallpaper"; Filename: "{app}\NoraWallpaper.exe"; IconFilename: "{app}\NoraWallpaper.exe"
; Create Desktop Shortcut (if user checked the box)
Name: "{autodesktop}\NoraWallpaper"; Filename: "{app}\NoraWallpaper.exe"; IconFilename: "{app}\NoraWallpaper.exe"; Tasks: desktopicon

[Run]
; Run the app automatically after installation finishes
Filename: "{app}\NoraWallpaper.exe"; Description: "{cm:LaunchProgram,NoraWallpaper}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; (Optional) Kill process before uninstalling if it is running
Filename: "{cmd}"; Parameters: "/C taskkill /F /IM NoraWallpaper.exe"; Flags: runhidden waituntilterminated

[Code]
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  // Kill the application silently before extracting files to prevent locking issues
  Exec(ExpandConstant('{cmd}'), '/c taskkill /F /IM NoraWallpaper.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := True;
end;

; LiteRestaurant Windows 11 all-in-one installer
; Build staging first: powershell -ExecutionPolicy Bypass -File build-installer.ps1

#define MyAppName "LiteRestaurant"
#define MyAppVersion "1.0.1"
#define MyAppPublisher "LiteRestaurant"
#define MyAppExeName "LiteRestaurant.exe"

[Setup]
AppId={{B7E2D4A8-1C5F-4A9E-8D3B-6F0A2C9E5B47}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=output
OutputBaseFilename=LiteRestaurant-Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupIconFile=Assets\app.ico
; Close/replace locked files when updating over a running install
CloseApplications=force
RestartApplications=no

[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Dirs]
Name: "{commonappdata}\LiteRestaurant"; Permissions: users-modify
Name: "{commonappdata}\LiteRestaurant\Logs"; Permissions: users-modify

[Files]
Source: "staging\LiteRestaurant.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "Stop-LiteRestaurant.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "staging\RestaurantPOS\*"; DestDir: "{app}\RestaurantPOS"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "staging\PrintServer\*"; DestDir: "{app}\PrintServer"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "deps\MicrosoftEdgeWebview2Setup.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall
Source: "deps\vc_redist.x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\إيقاف LiteRestaurant"; Filename: "{app}\Stop-LiteRestaurant.bat"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{autodesktop}\إيقاف LiteRestaurant"; Filename: "{app}\Stop-LiteRestaurant.bat"; Tasks: desktopicon

[Run]
Filename: "{tmp}\vc_redist.x64.exe"; Parameters: "/install /quiet /norestart"; StatusMsg: "Installing Microsoft Visual C++ Runtime..."; Check: NeedsVCRedist; Flags: waituntilterminated
Filename: "{tmp}\MicrosoftEdgeWebview2Setup.exe"; Parameters: "/silent /install"; StatusMsg: "Installing WebView2 Runtime..."; Check: NeedsWebView2; Flags: waituntilterminated
Filename: "{cmd}"; Parameters: "/c netsh advfirewall firewall delete rule name=""LiteRestaurant API"" & netsh advfirewall firewall add rule name=""LiteRestaurant API"" dir=in action=allow protocol=TCP localport=5189"; StatusMsg: "Opening firewall for LiteRestaurant..."; Flags: runhidden
Filename: "{cmd}"; Parameters: "/c netsh advfirewall firewall delete rule name=""LiteRestaurant PrintServer"" & netsh advfirewall firewall add rule name=""LiteRestaurant PrintServer"" dir=in action=allow protocol=TCP localport=5000"; Flags: runhidden

[UninstallRun]
Filename: "{cmd}"; Parameters: "/c netsh advfirewall firewall delete rule name=""LiteRestaurant API"" & netsh advfirewall firewall delete rule name=""LiteRestaurant PrintServer"""; Flags: runhidden

[Code]
procedure KillLiteRestaurantProcesses;
var
  ResultCode: Integer;
begin
  { Stop running system before overwriting files — no manual Stop bat needed }
  Exec('taskkill.exe', '/F /IM LiteRestaurant.exe /T', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec('taskkill.exe', '/F /IM RestaurantPOS.exe /T', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec('taskkill.exe', '/F /IM PrintServer.exe /T', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Sleep(800);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  KillLiteRestaurantProcesses;
  NeedsRestart := False;
  Result := '';
end;

function InitializeUninstall(): Boolean;
begin
  KillLiteRestaurantProcesses;
  Result := True;
end;

function NeedsVCRedist: Boolean;
var
  Version: String;
begin
  if RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Installed', Version) then
    Result := (Version <> '1')
  else if RegQueryStringValue(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Installed', Version) then
    Result := (Version <> '1')
  else
    Result := True;
end;

function NeedsWebView2: Boolean;
var
  Version: String;
begin
  if RegQueryStringValue(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Version) then
    Result := False
  else if RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Version) then
    Result := False
  else
    Result := True;
end;

[UninstallDelete]
Type: filesandordirs; Name: "{commonappdata}\LiteRestaurant"

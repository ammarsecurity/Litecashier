; Litecashier Windows 11 all-in-one installer
; Build staging first: powershell -ExecutionPolicy Bypass -File build-installer.ps1

#define MyAppName "Litecashier"
#define MyAppVersion "1.0.15"
#define MyAppPublisher "Litecashier"
#define MyAppExeName "Litecashier.exe"

[Setup]
AppId={{A3F9C2E1-8B4D-4F6A-9C1E-2D7E5A8B4F31}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=output
OutputBaseFilename=Litecashier-Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
; Close/replace locked files when updating over a running install
CloseApplications=force
RestartApplications=no

[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Dirs]
Name: "{commonappdata}\Litecashier"; Permissions: users-modify
Name: "{commonappdata}\Litecashier\Logs"; Permissions: users-modify

[Files]
Source: "staging\Litecashier.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "Stop-Litecashier.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "staging\POS\*"; DestDir: "{app}\POS"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "staging\PrintServer\*"; DestDir: "{app}\PrintServer"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "deps\MicrosoftEdgeWebview2Setup.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall
Source: "deps\vc_redist.x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\إيقاف Litecashier"; Filename: "{app}\Stop-Litecashier.bat"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{autodesktop}\إيقاف Litecashier"; Filename: "{app}\Stop-Litecashier.bat"; Tasks: desktopicon

[Run]
Filename: "{tmp}\vc_redist.x64.exe"; Parameters: "/install /quiet /norestart"; StatusMsg: "Installing Microsoft Visual C++ Runtime..."; Check: NeedsVCRedist; Flags: waituntilterminated
Filename: "{tmp}\MicrosoftEdgeWebview2Setup.exe"; Parameters: "/silent /install"; StatusMsg: "Installing WebView2 Runtime..."; Check: NeedsWebView2; Flags: waituntilterminated
Filename: "{cmd}"; Parameters: "/c netsh advfirewall firewall delete rule name=""Litecashier POS"" & netsh advfirewall firewall add rule name=""Litecashier POS"" dir=in action=allow protocol=TCP localport=5189"; StatusMsg: "Opening firewall for Litecashier..."; Flags: runhidden
Filename: "{cmd}"; Parameters: "/c netsh advfirewall firewall delete rule name=""Litecashier PrintServer"" & netsh advfirewall firewall add rule name=""Litecashier PrintServer"" dir=in action=allow protocol=TCP localport=5000"; Flags: runhidden

[UninstallRun]
Filename: "{cmd}"; Parameters: "/c netsh advfirewall firewall delete rule name=""Litecashier POS"" & netsh advfirewall firewall delete rule name=""Litecashier PrintServer"""; Flags: runhidden

[Code]
procedure KillLitecashierProcesses;
var
  ResultCode: Integer;
begin
  { Stop running system before overwriting files — no manual Stop-Litecashier.bat needed }
  Exec('taskkill.exe', '/F /IM Litecashier.exe /T', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec('taskkill.exe', '/F /IM POS.exe /T', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec('taskkill.exe', '/F /IM PrintServer.exe /T', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Sleep(800);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  KillLitecashierProcesses;
  NeedsRestart := False;
  Result := '';
end;

function InitializeUninstall(): Boolean;
begin
  KillLitecashierProcesses;
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
Type: filesandordirs; Name: "{commonappdata}\Litecashier"

; Litecashier Windows 11 all-in-one installer
; Build staging first: powershell -ExecutionPolicy Bypass -File build-installer.ps1

#define MyAppName "Litecashier"
#define MyAppVersion "1.0.26"
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
Name: "{commonappdata}\Litecashier"; Permissions: users-modify
Name: "{commonappdata}\Litecashier\Logs"; Permissions: users-modify

[Files]
Source: "staging\Litecashier.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "Stop-Litecashier.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "staging\POS\*"; DestDir: "{app}\POS"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "appsettings.Production.json"
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
var
  DbPage: TInputQueryWizardPage;

function EscapeJson(const S: string): string;
begin
  Result := S;
  StringChangeEx(Result, '\', '\\', True);
  StringChangeEx(Result, '"', '\"', True);
  StringChangeEx(Result, #13, '\r', True);
  StringChangeEx(Result, #10, '\n', True);
  StringChangeEx(Result, #9, '\t', True);
end;

function BuildDbConnectionString: string;
begin
  Result :=
    'Server=' + Trim(DbPage.Values[0]) +
    ';Port=' + Trim(DbPage.Values[1]) +
    ';Database=' + Trim(DbPage.Values[4]) +
    ';User=' + Trim(DbPage.Values[2]) +
    ';Password=' + DbPage.Values[3] +
    ';CharSet=utf8mb4;Connection Timeout=30;';
end;

procedure WriteProductionAppsettings;
var
  Path, Content, Conn: string;
begin
  Path := ExpandConstant('{app}\POS\appsettings.Production.json');
  Conn := EscapeJson(BuildDbConnectionString);
  Content :=
    '{' + #13#10 +
    '  "Urls": "http://0.0.0.0:5189",' + #13#10 +
    '  "ConnectionStrings": {' + #13#10 +
    '    "WebApiDatabase": "' + Conn + '"' + #13#10 +
    '  },' + #13#10 +
    '  "ApiSettings": {' + #13#10 +
    '    "ImageBaseUrl": "/Images/"' + #13#10 +
    '  },' + #13#10 +
    '  "DatabaseSettings": {' + #13#10 +
    '    "ApplyMigrationsOnStartup": true,' + #13#10 +
    '    "SeedOnStartup": false,' + #13#10 +
    '    "SeedDemoAccounts": false,' + #13#10 +
    '    "CommercialUserId": 0,' + #13#10 +
    '    "MysqldumpPath": ""' + #13#10 +
    '  },' + #13#10 +
    '  "License": {' + #13#10 +
    '    "Enabled": true,' + #13#10 +
    '    "BaseUrl": "https://litecashier-keys.smartstick-iq.com",' + #13#10 +
    '    "Product": "Cashier",' + #13#10 +
    '    "RevalidateHours": 24' + #13#10 +
    '  }' + #13#10 +
    '}' + #13#10;
  if not SaveStringToFile(Path, Content, False) then
    MsgBox('تعذر كتابة إعدادات قاعدة البيانات:' + #13#10 + Path, mbError, MB_OK);
end;

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

procedure InitializeWizard;
begin
  DbPage := CreateInputQueryPage(wpSelectDir,
    'إعدادات قاعدة البيانات MySQL',
    'أدخل بيانات الاتصال — النظام ينشئ القاعدة إن لم تكن موجودة أو يحدّثها إن وُجدت',
    'تأكد أن خدمة MySQL (مثلاً من XAMPP) تعمل قبل تشغيل Litecashier.');
  DbPage.Add('Host / Server:', False);
  DbPage.Add('Port:', False);
  DbPage.Add('User:', False);
  DbPage.Add('Password:', True);
  DbPage.Add('Database name:', False);
  DbPage.Values[0] := 'localhost';
  DbPage.Values[1] := '3306';
  DbPage.Values[2] := 'root';
  DbPage.Values[3] := '';
  DbPage.Values[4] := 'pos';
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = DbPage.ID then
  begin
    if Trim(DbPage.Values[0]) = '' then
    begin
      MsgBox('أدخل عنوان السيرفر (Host).', mbError, MB_OK);
      Result := False;
    end
    else if Trim(DbPage.Values[1]) = '' then
    begin
      MsgBox('أدخل المنفذ (Port).', mbError, MB_OK);
      Result := False;
    end
    else if Trim(DbPage.Values[2]) = '' then
    begin
      MsgBox('أدخل اسم المستخدم (User).', mbError, MB_OK);
      Result := False;
    end
    else if Trim(DbPage.Values[4]) = '' then
    begin
      MsgBox('أدخل اسم قاعدة البيانات.', mbError, MB_OK);
      Result := False;
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ExistingPath: string;
begin
  if CurStep = ssPostInstall then
  begin
    ExistingPath := ExpandConstant('{app}\POS\appsettings.Production.json');
    { Only write a new appsettings when no previous config exists (fresh install).
      On upgrade, the existing config is preserved so the database connection is not lost. }
    if not FileExists(ExistingPath) then
      WriteProductionAppsettings;
  end;
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

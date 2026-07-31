#Requires -Version 5.1
<#
.SYNOPSIS
    Builds Litecashier Windows installer (POS + PrintServer + Launcher).

.DESCRIPTION
    Run from repository root or installer folder on a development machine with:
    - .NET 8 SDK
    - Node.js 18+
    - Inno Setup 6 (ISCC.exe)

    MySQL/MariaDB is NOT bundled — configure XAMPP (or any MySQL) manually:
    Server=localhost, Database=pos, User=root, Password=(empty)
#>
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SkipFrontendBuild,
    [switch]$SkipInstallerCompile
)

$ErrorActionPreference = "Stop"

$InstallerDir = $PSScriptRoot
$RepoRoot = Resolve-Path (Join-Path $InstallerDir "..")
$StagingDir = Join-Path $InstallerDir "staging"
$OutputDir = Join-Path $InstallerDir "output"
$DepsDir = Join-Path $InstallerDir "deps"
$PosProject = Join-Path $RepoRoot "cashier_back\POS\POS.csproj"
$PrintServerProject = Join-Path $RepoRoot "cashier_back\PrintServer\PrintServer.csproj"
$LauncherProject = Join-Path $InstallerDir "Litecashier.Launcher\Litecashier.Launcher.csproj"
$FrontendDir = Join-Path $RepoRoot "cashier_front"
$WebView2Bootstrapper = Join-Path $DepsDir "MicrosoftEdgeWebview2Setup.exe"
$VcRedistInstaller = Join-Path $DepsDir "vc_redist.x64.exe"
$WebView2Url = "https://go.microsoft.com/fwlink/p/?LinkId=2124703"
$VcRedistUrl = "https://aka.ms/vs/17/release/vc_redist.x64.exe"

function Assert-Command {
    param([string]$Name, [string]$Hint)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "$Name not found. $Hint"
    }
}

function Ensure-Directory {
    param([string]$Path)
    if (-not (Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

function Download-File {
    param(
        [string]$Url,
        [string]$Destination
    )

    if (Test-Path $Destination) {
        Write-Host "Using cached file: $Destination"
        return
    }

    Ensure-Directory (Split-Path $Destination -Parent)
    Write-Host "Downloading $Url"

    if (Get-Command curl.exe -ErrorAction SilentlyContinue) {
        curl.exe -fL $Url -o $Destination
    }
    else {
        Invoke-WebRequest -Uri $Url -OutFile $Destination -UseBasicParsing
    }
}

Write-Host "=== Litecashier installer build ==="

Assert-Command "dotnet" "Install .NET 8 SDK from https://dotnet.microsoft.com/download"
Assert-Command "node" "Install Node.js 18+ from https://nodejs.org/"
Assert-Command "npm" "Install Node.js which includes npm"

$IsccCandidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
)
$IsccPath = $IsccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $IsccPath -and -not $SkipInstallerCompile) {
    throw "Inno Setup 6 (ISCC.exe) not found. Install from https://jrsoftware.org/isinfo.php or pass -SkipInstallerCompile."
}

Ensure-Directory $StagingDir
Ensure-Directory $OutputDir
Ensure-Directory $DepsDir

Get-ChildItem $StagingDir -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force

Download-File -Url $WebView2Url -Destination $WebView2Bootstrapper
Download-File -Url $VcRedistUrl -Destination $VcRedistInstaller

if (-not $SkipFrontendBuild) {
    # Force same-origin API so the installed SPA talks to local POS (:5189),
    # not whatever VUE_APP_API_URL is in cashier_front/.env (dev LAN IP / remote).
    Write-Host "Building frontend (VUE_APP_API_URL=/ for installer same-origin)..."
    Push-Location $FrontendDir
    $prevApiUrl = $env:VUE_APP_API_URL
    $env:VUE_APP_API_URL = "/"
    try {
        if (-not (Test-Path "node_modules")) {
            npm ci
        }
        npm run build
    }
    finally {
        if ($null -eq $prevApiUrl) {
            Remove-Item Env:\VUE_APP_API_URL -ErrorAction SilentlyContinue
        }
        else {
            $env:VUE_APP_API_URL = $prevApiUrl
        }
        Pop-Location
    }
}

Write-Host "Publishing POS..."
dotnet publish $PosProject `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -o (Join-Path $StagingDir "POS") `
    /p:PublishReadyToRun=true

Write-Host "Publishing PrintServer..."
dotnet publish $PrintServerProject `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -o (Join-Path $StagingDir "PrintServer") `
    /p:PublishReadyToRun=true

Write-Host "Publishing Launcher..."
dotnet publish $LauncherProject `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -o $StagingDir `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:EnableCompressionInSingleFile=true

$productionSettingsSource = Join-Path $InstallerDir "config\appsettings.Production.json"
$productionSettingsTarget = Join-Path $StagingDir "POS\appsettings.Production.json"
Copy-Item $productionSettingsSource $productionSettingsTarget -Force

$spaTarget = Join-Path $StagingDir "POS\wwwroot\app"
Ensure-Directory $spaTarget
Copy-Item (Join-Path $FrontendDir "dist\*") $spaTarget -Recurse -Force

# Remove launcher publish artifacts except Litecashier.exe from staging root clutter
Get-ChildItem $StagingDir -File | Where-Object { $_.Name -ne "Litecashier.exe" } | Remove-Item -Force

if (-not $SkipInstallerCompile) {
    Write-Host "Compiling installer with Inno Setup..."
    & $IsccPath (Join-Path $InstallerDir "Litecashier.iss")
    Write-Host "Installer created in: $OutputDir"
}
else {
    Write-Host "Staging complete: $StagingDir"
}

Write-Host "Done."

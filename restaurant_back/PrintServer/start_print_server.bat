@echo off
echo ========================================
echo Restaurant POS Print Server (C#)
echo ========================================
echo.

cd /d "%~dp0"

echo Checking .NET SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK not found!
    echo Please install .NET 8.0 SDK or later from https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo .NET SDK found!
echo.

echo Restoring packages...
dotnet restore
if errorlevel 1 (
    echo ERROR: Failed to restore packages
    pause
    exit /b 1
)

echo.
echo Building project...
dotnet build
if errorlevel 1 (
    echo ERROR: Build failed
    pause
    exit /b 1
)

echo.
echo ========================================
echo Starting Print Server...
echo Server will run on http://localhost:5000
echo Press Ctrl+C to stop the server
echo ========================================
echo.

dotnet run

pause



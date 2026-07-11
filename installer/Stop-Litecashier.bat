@echo off
chcp 65001 >nul
title إيقاف Litecashier
echo.
echo ========================================
echo   جاري إيقاف نظام Litecashier...
echo ========================================
echo.

REM Launcher (أيقونة سطح المكتب)
taskkill /F /IM "Litecashier.exe" >nul 2>&1
if %ERRORLEVEL%==0 (echo [OK] تم إيقاف Litecashier.exe) else (echo [--] Litecashier.exe غير شغال)

REM POS API
taskkill /F /IM "POS.exe" >nul 2>&1
if %ERRORLEVEL%==0 (echo [OK] تم إيقاف POS.exe) else (echo [--] POS.exe غير شغال)

REM Print Server
taskkill /F /IM "PrintServer.exe" >nul 2>&1
if %ERRORLEVEL%==0 (echo [OK] تم إيقاف PrintServer.exe) else (echo [--] PrintServer.exe غير شغال)

echo.
echo ========================================
echo   تم إيقاف النظام. يمكنك تثبيت التحديث الآن.
echo ========================================
echo.
pause

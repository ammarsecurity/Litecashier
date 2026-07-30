@echo off
chcp 65001 >nul
title إيقاف LiteRestaurant
echo.
echo ========================================
echo   جاري إيقاف نظام LiteRestaurant...
echo ========================================
echo.

REM Launcher (أيقونة سطح المكتب)
taskkill /F /IM "LiteRestaurant.exe" >nul 2>&1
if %ERRORLEVEL%==0 (echo [OK] تم إيقاف LiteRestaurant.exe) else (echo [--] LiteRestaurant.exe غير شغال)

REM Restaurant API
taskkill /F /IM "RestaurantPOS.exe" >nul 2>&1
if %ERRORLEVEL%==0 (echo [OK] تم إيقاف RestaurantPOS.exe) else (echo [--] RestaurantPOS.exe غير شغال)

REM Print Server
taskkill /F /IM "PrintServer.exe" >nul 2>&1
if %ERRORLEVEL%==0 (echo [OK] تم إيقاف PrintServer.exe) else (echo [--] PrintServer.exe غير شغال)

echo.
echo ========================================
echo   تم إيقاف النظام. يمكنك تثبيت التحديث الآن.
echo ========================================
echo.
pause

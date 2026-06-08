@echo off
setlocal

title Tao WebPhotocopy.zip va copy vao Clipboard

set "SCRIPT_DIR=%~dp0"
set "SCRIPT_ROOT=%SCRIPT_DIR:~0,-1%"
set "PROJECT_DIR=%SCRIPT_DIR%Project"
set "ZIP_PATH=%SCRIPT_DIR%WebPhotocopy.zip"
set "PS_SCRIPT=%SCRIPT_DIR%Create-ChatGPT-SourceZip.ps1"

cd /d "%SCRIPT_DIR%"

echo.
echo Dang tao WebPhotocopy.zip tu source WebPhotocopyHub...
echo File cu se bi ghi de. File moi se loai tru secret, runtime data, cache va file build.
echo File moi se duoc copy vao Clipboard.
echo.

powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File "%PS_SCRIPT%" -WebPhotocopyRoot "%SCRIPT_ROOT%" -ProjectRoot "%PROJECT_DIR%" -ZipPath "%ZIP_PATH%"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo HOAN TAT: WebPhotocopy.zip da duoc copy vao Clipboard.
    echo Cua so se tu dong dong sau 2 giay...
    powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Start-Sleep -Seconds 2" >nul
    exit /b 0
) else (
    echo.
    echo LOI: Tao ZIP hoac copy Clipboard that bai.
    echo Cua so se giu lai de ban xem loi.
    pause
    exit /b 1
)

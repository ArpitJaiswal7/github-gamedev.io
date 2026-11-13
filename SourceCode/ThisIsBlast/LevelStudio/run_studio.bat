@echo off
title This is Blast - Level Studio Launcher
cd /d "%~dp0"
echo ========================================================
echo   Launching 'This is Blast' Level Studio & Generator...
echo ========================================================
python server.py
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Python failed to launch server.py. Opening index.html directly...
    start index.html
)
pause

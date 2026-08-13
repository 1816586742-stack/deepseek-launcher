@echo off
REM DSH Launcher - Windows Script
REM Double-click to start dsh and open browser

echo Starting DeepSeek Harness...

REM Check Node.js
where node > NUL 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Node.js not found. Install from: https://nodejs.org
    pause
    exit /b 1
)

REM Start dsh web (background)
start "" /b npx -y @deepseek-ai/dsh web

REM Wait for port ready (max 60s)
echo Waiting for dsh to start...
set /a count=0
:wait
timeout /t 1 /nobreak > NUL
set /a count+=1
curl -s http://127.0.0.1:3080 > NUL 2>&1
if %errorlevel% equ 0 goto ready
if %count% geq 60 (
    echo [ERROR] dsh startup timeout
    pause
    exit /b 1
)
goto wait

:ready
echo dsh started, opening browser...
start http://127.0.0.1:3080
echo.
echo Closing this window will NOT stop dsh.
echo To stop: kill the npx process in Task Manager.

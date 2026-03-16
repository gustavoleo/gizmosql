@echo off
setlocal

set "INSTALL_ROOT=%~dp0"
set "POWERSHELL_EXE=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"

if /I "%~1"=="launch-demo-server" (
  "%POWERSHELL_EXE%" -ExecutionPolicy Bypass -NoExit -File "%INSTALL_ROOT%launch-demo-server.ps1"
  exit /b %ERRORLEVEL%
)

if /I "%~1"=="open-quickstart" (
  start "" "%INSTALL_ROOT%quickstart.html"
  exit /b 0
)

if /I "%~1"=="open-ui" (
  call :open_ui
  exit /b %ERRORLEVEL%
)

echo Usage: %~nx0 launch-demo-server ^| open-quickstart ^| open-ui
exit /b 1

:open_ui
call :start_demo_server_if_needed

for %%F in (
  "%ProgramFiles%\GizmoSQL UI\GizmoSQL UI.exe"
  "%ProgramFiles%\GizmoSQL UI\gizmosql-ui.exe"
  "%ProgramFiles%\GizmoSQL\GizmoSQL UI.exe"
  "%ProgramFiles%\GizmoSQL\gizmosql-ui.exe"
) do (
  if exist %%~F (
    start "" %%~F
    exit /b 0
  )
)

echo GizmoSQL UI was not found in the default install paths.
echo Re-run GizmoSQL-Setup-x64.exe with "GizmoSQL UI" selected, or install the standalone UI MSI.
start "" "%INSTALL_ROOT%quickstart.html"
exit /b 1

:start_demo_server_if_needed
set "DEMO_PORT_IN_USE="
for /f "delims=" %%L in ('netstat -ano ^| findstr /R /C:":31337 .*LISTENING"') do set "DEMO_PORT_IN_USE=1"

if defined DEMO_PORT_IN_USE (
  exit /b 0
)

start "GizmoSQL Demo Server" /min "%POWERSHELL_EXE%" -ExecutionPolicy Bypass -NoExit -File "%INSTALL_ROOT%launch-demo-server.ps1"
timeout /t 2 /nobreak >nul
exit /b 0

@echo off
setlocal
if not exist "%~dp0Mod Files\Bundle-Launch.ps1" (
 echo Missing Mod Files\Bundle-Launch.ps1. Extract the entire ZIP before opening this launcher.
 exit /b 2
)
set "SIGNALIS_PS=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
if exist "%SystemRoot%\Sysnative\WindowsPowerShell\v1.0\powershell.exe" set "SIGNALIS_PS=%SystemRoot%\Sysnative\WindowsPowerShell\v1.0\powershell.exe"
set "PSModulePath=%SystemRoot%\System32\WindowsPowerShell\v1.0\Modules"
"%SIGNALIS_PS%" -NoProfile -ExecutionPolicy Bypass -File "%~dp0Mod Files\Bundle-Launch.ps1" -Runtime SteamVR %*
exit /b %errorlevel%

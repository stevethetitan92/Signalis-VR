@echo off
setlocal
set "SIGNALIS_PS=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
if exist "%SystemRoot%\Sysnative\WindowsPowerShell\v1.0\powershell.exe" set "SIGNALIS_PS=%SystemRoot%\Sysnative\WindowsPowerShell\v1.0\powershell.exe"
set "PSModulePath=%SystemRoot%\System32\WindowsPowerShell\v1.0\Modules"
"%SIGNALIS_PS%" -NoProfile -ExecutionPolicy Bypass -File "%~dp0Launcher-Diagnostics.ps1" -Run %*
exit /b %errorlevel%


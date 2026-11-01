@echo off
rem Runs pack-local.ps1 regardless of the PowerShell execution policy.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0pack-local.ps1" %*

@echo off
rem Runs update-kayala.ps1 regardless of the PowerShell execution policy.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0update-kayala.ps1" %*

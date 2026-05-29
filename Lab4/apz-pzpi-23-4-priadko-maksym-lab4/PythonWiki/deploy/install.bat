@echo off
powershell -ExecutionPolicy Bypass -File "%~dp0Install-Lab4.ps1" -Recreate %*

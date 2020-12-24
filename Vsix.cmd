@echo off
powershell -ExecutionPolicy RemoteSigned -File %~dp0\scripts\Vsix.ps1 %*

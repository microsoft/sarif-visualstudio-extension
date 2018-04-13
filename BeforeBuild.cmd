::Build initialization step

@ECHO off
SETLOCAL

SET NuGetConfigFile=%~dp0src\NuGet.Config
SET NuGetPackageDir=src\packages

md bld\bin\nuget

::Restore nuget packages
%~dp0.nuget\NuGet.exe restore src\Sarif.Viewer.VisualStudio\Sarif.Viewer.VisualStudio.csproj -ConfigFile "%NuGetConfigFile%" -OutputDirectory "%NuGetPackageDir%"

if "%ERRORLEVEL%" NEQ "0" (
echo NuGet restore failed.
goto ExitFailed
)

%~dp0.nuget\NuGet.exe restore src\Sarif.Viewer.VisualStudio.UnitTests\Sarif.Viewer.VisualStudio.UnitTests.csproj -ConfigFile "%NuGetConfigFile%" -OutputDirectory "%NuGetPackageDir%"

if "%ERRORLEVEL%" NEQ "0" (
echo NuGet restore failed.
goto ExitFailed
)

goto Exit

:ExitFailed
@echo.
@echo script %~n0 failed
exit /b 1

:Exit

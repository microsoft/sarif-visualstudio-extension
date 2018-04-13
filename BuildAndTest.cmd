@ECHO off
SETLOCAL
@REM Uncomment this line to update nuget.exe
@REM Doing so can break SLN build (which uses nuget.exe to
@REM create a nuget package for the SARIF SDK) so must opt-in
@REM %~dp0.nuget\NuGet.exe update -self

set Platform=AnyCPU
set Configuration=Release

:NextArg
if "%1" == "" goto :EndArgs
if "%1" == "/config" (
    if not "%2" == "Debug" if not "%2" == "Release" echo error: /config must be either Debug or Release && goto :ExitFailed
    set Configuration=%2&& shift && shift && goto :NextArg
)
echo Unrecognized option "%1" && goto :ExitFailed

:EndArgs

@REM Remove existing build data
if exist bld (rd /s /q bld)

call BeforeBuild.cmd

if "%ERRORLEVEL%" NEQ "0" (
goto ExitFailed
)

msbuild /verbosity:minimal /target:rebuild src\Sarif.Viewer.VisualStudio.sln /filelogger /fileloggerparameters:Verbosity=detailed /p:AutoGenerateBindingRedirects=false
if "%ERRORLEVEL%" NEQ "0" (
goto ExitFailed
)

src\packages\xunit.runner.console.2.3.0\tools\net452\xunit.console.x86.exe bld\bin\Sarif.Viewer.VisualStudio.UnitTests\AnyCPU_%Configuration%\Sarif.Viewer.VisualStudio.UnitTests.dll -parallel none
if "%ERRORLEVEL%" NEQ "0" (
goto ExitFailed
)

goto Exit

:ExitFailed
@echo Build and test did not complete successfully.
Exit /B 1

:Exit
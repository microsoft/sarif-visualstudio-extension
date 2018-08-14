::Build NuGet packages step
@ECHO off
SETLOCAL

set BinaryOutputDirectory=%1
set Configuration=%1
set Platform=%2

if "%BinaryOutputDirectory%" EQU "" (
set BinaryOutputDirectory=.\bld\bin\
)

if "%Configuration%" EQU "" (
set Configuration=Release
)

if "%Platform%" EQU "" (
set Platform=AnyCpu
)

set ArchiveDirectory=%BinaryOutputDirectory%\Expanded
set SigningDirectory=%BinaryOutputDirectory%\Signing
set BinaryOutputDirectory=%BinaryOutputDirectory%\%Platform%_%Configuration%


:: Copy viewer dll to net461
if exist %ArchiveDirectory% (rd /s /q %ArchiveDirectory%)
md %ArchiveDirectory%

powershell -File .\scripts\Unzip.ps1 %BinaryOutputDirectory%\Sarif.Viewer.VisualStudio\Microsoft.Sarif.Viewer.vsix %ArchiveDirectory%
xcopy /Y %SigningDirectory%\net461\Microsoft.Sarif.Viewer.dll %ArchiveDirectory%
del /Q %BinaryOutputDirectory%\Sarif.Viewer.VisualStudio\Microsoft.Sarif.Viewer.vsix

:: This command will only output to a file with a .zip extension
powershell Compress-Archive -Path %ArchiveDirectory%\* -CompressionLevel Fastest -DestinationPath %BinaryOutputDirectory%\Sarif.Viewer.VisualStudio\Microsoft.Sarif.Viewer.zip
pushd %BinaryOutputDirectory%\Sarif.Viewer.VisualStudio
rename Microsoft.Sarif.Viewer.zip Microsoft.Sarif.Viewer.vsix
popd

goto :Exit

:ExitFailed
@echo.
@echo Build NuGet packages from layout directory step failed.
exit /b 1

:Exit
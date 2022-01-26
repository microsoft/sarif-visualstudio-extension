<#
.SYNOPSIS
    Build, test, and package the SARIF SDK.
.DESCRIPTION
    Builds the SARIF SDK for multiple target frameworks, runs the tests, and creates
    NuGet packages.
.PARAMETER Configuration
    The build configuration: Release or Debug. Default=Release
.PARAMETER NoClean
    Do not remove the outputs from the previous build.
.PARAMETER NoRestore
    Do not restore NuGet packages.
.PARAMETER NoBuild
    Do not build.
.PARAMETER NoTest
    Do not run tests.
.PARAMETER NoFormat
    Do not format files based on dotnet-format tool
.PARAMETER NoPackage
    Do not create NuGet packages.
.PARAMETER NoSigningDirectory
    Do not create a directory containing the binaries that need to be signed.
.PARAMETER Install
    Install the VSIX.
#>

[CmdletBinding()]
param(
    [string]
    [ValidateSet("Debug", "Release")]
    $Configuration="Release",

    [switch]
    $NoClean,

    [switch]
    $NoRestore,

    [switch]
    $NoBuild,

    [switch]
    $NoTest,

    [switch]
    $NoFormat,

    [switch]
    $NoPackage,

    [switch]
    $NoPublish,

    [switch]
    $NoSigningDirectory,

    [switch]
    $Install
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

$ScriptName = $([io.Path]::GetFileNameWithoutExtension($PSCommandPath))

Import-Module -Force $PSScriptRoot\ScriptUtilities.psm1
Import-Module -Force $PSScriptRoot\Projects.psm1

$SolutionFile = "$SourceRoot\Sarif.Viewer.VisualStudio.sln"
$BuildTarget = "Rebuild"

function Invoke-Build {
    Write-Information "Building $SolutionFile..."
    msbuild /verbosity:minimal /target:$BuildTarget /property:Configuration=$Configuration /fileloggerparameters:Verbosity=detailed $SolutionFile
    if ($LASTEXITCODE -ne 0) {
        Exit-WithFailureMessage $ScriptName "Build failed."
    }
}

# Create a directory populated with the binaries that need to be signed.
function New-SigningDirectory {
    Write-Information "Copying files to signing directory..."
    $SigningDirectory = "$BinRoot\Signing"

    foreach ($framework in $Frameworks) {
        New-DirectorySafely $SigningDirectory\$framework
    }

    foreach ($project in $Projects.Product) {
        $projectBinDirectory = Get-ProjectBinDirectory $project $configuration

        foreach ($framework in $Frameworks) {
            $destinationDirectory = "$SigningDirectory\$framework"
            $fileToCopy = "$projectBinDirectory\$project.dll"

            if (Test-Path $fileToCopy) {
                Copy-Item -Force -Path $fileToCopy -Destination $destinationDirectory
            }
        }
    }

    # Copy the viewer. Its name doesn't fit the pattern binary name == project name,
    # so we copy it by hand.
    foreach ($framework in $Frameworks) {
        Copy-Item -Force -Path $BinRoot\${Platform}_$Configuration\Sarif.Viewer.VisualStudio\Microsoft.Sarif.Viewer.dll -Destination $SigningDirectory\$framework
    }
}

function  Install-SarifExtension {
    $vsixInstallerPaths = Get-ChildItem $BinRoot "*.vsix" -Recurse
    if (-not $vsixInstallerPaths) {
        Exit-WithFailureMessage $ScriptName "Cannot install VSIX: .vsix file was not found."
    }

    Write-Information "Launching VSIX installer..."
    & $vsixInstallerPaths[0].FullName
}

# Create registry settings to open SARIF files in Visual Studio by default.
function Set-SarifFileAssociationRegistrySettings {
    # You need to be Admin to modify the registry, so create the settings by
    # running a separate script, elevated ("-Verb RunAs").
    $path = "$PSScriptRoot\RegistrySettings.ps1"
    Write-Information "Creating registry settings to associate SARIF files with Visual Studio..."
    $proc = Start-Process powershell.exe -ArgumentList "-File $path" -Verb RunAs -PassThru
    $proc.WaitForExit()
    $exitCode = $proc.ExitCode
    $proc.Dispose()
    if ($exitCode -ne 0) {
        Exit-WithFailureMessage $ScriptName "Failed to create registry settings ($exitCode)."
    }
}

if (-not (Test-Path "$RepoRoot\Src\sarif-pattern-matcher") -or (Get-ChildItem "$RepoRoot\Src\sarif-pattern-matcher" | Measure-Object).Count -eq 0) {
    Write-Information "Retrieving sarif-pattern-matcher submodule..."
    git submodule update --init --recursive
}

if (-not $NoClean) {
    Remove-DirectorySafely $BuildRoot
}

if (-not $NoRestore) {
    foreach ($project in $Projects.All) {
        Write-Information "Restoring NuGet packages for $project..."
        & $RepoRoot\.nuget\NuGet.exe restore $SourceRoot\$project\$project.csproj -OutputDirectory "$NuGetPackageRoot" -Verbosity quiet
        if ($LASTEXITCODE -ne 0) {
            Exit-WithFailureMessage $ScriptName "NuGet restore failed for $project."
        }
    }
}

if (-not $NoBuild) {
    & $RepoRoot\src\sarif-pattern-matcher\BuildAndTest.cmd -NoTest -Configuration $Configuration -NoFormat
    if ($LASTEXITCODE -ne 0) {
        Exit-WithFailureMessage $ScriptName "sarif-pattern-matcher failed."
    }

    Invoke-Build
}

if (-not $NoTest) {
    & $PSScriptRoot\Run-Tests.ps1 -Configuration $Configuration
    if (-not $?) {
        Exit-WithFailureMessage $ScriptName "RunTests failed."
    }
}

if (-not $NoFormat) {
    dotnet tool update --global dotnet-format --version 4.1.131201
    dotnet-format --folder --exclude .\src\sarif-pattern-matcher\
}

if (-not $NoSigningDirectory) {
    New-SigningDirectory
}

if (-not $NoPackage) {
    New-NuGetPackages $Configuration $Projects $Frameworks
}

if ($Install) {
    Install-SarifExtension
    Set-SarifFileAssociationRegistrySettings
}

Write-Information "$ScriptName SUCCEEDED."
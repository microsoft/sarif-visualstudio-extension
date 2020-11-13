<#
.SYNOPSIS
    Runs tests in all test projects.

.DESCRIPTION
    This script runs the tests in each test project. This is done in a separate script,
    rather than inline in BuildAndTest.ps1, because AppVeyor cannot run BuildAndTest.
    AppVeyor runs the tests by invoking a separate script, and this is it.

.PARAMETER Configuration
    The build configuration: Release or Debug. Default=Release
#>

[CmdletBinding()]
param(
    [string]
    [ValidateSet("Debug", "Release")]
    $Configuration="Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

Import-Module $PSScriptRoot\ScriptUtilities.psm1 -Force
Import-Module $PSScriptRoot\Projects.psm1 -Force

$ScriptName = $([io.Path]::GetFileNameWithoutExtension($PSCommandPath))

$TestRunnerRootPath = "$NuGetPackageRoot\xunit.runner.console\2.4.1\tools\"

foreach ($project in $Projects.Test) {
    Write-Information "Running tests in ${project}..."
    Push-Location $BinRoot\${Platform}_$Configuration\$project
    $dll = "$project" + ".dll"
    & ${TestRunnerRootPath}net472\xunit.console.exe $dll -parallel none
    if ($LASTEXITCODE -ne 0) {
        Pop-Location
        Exit-WithFailureMessage $ScriptName "${project}: tests failed."
    }
    Pop-Location
}

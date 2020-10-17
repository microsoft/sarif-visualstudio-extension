<#
.SYNOPSIS
    Performs pre-build actions.
.DESCRIPTION
    This script performs the actions that are required before building the solution file
    src\Everything.sln. These actions are broken out into a separate script, rather than
    being performed inline in BuildAndTest.cmd, because AppVeyor cannot run BuildAndTest.
    AppVeyor only allows you to specify the project to build, and a script to run before
    the build step. So that is how we have factored the build scripts.
.PARAMETER NoClean
    Do not remove the outputs from the previous build.
.PARAMETER NoRestore
    Do not restore NuGet packages.
#>

[CmdletBinding()]
param(
    [switch]
    $NoClean,

    [switch]
    $NoRestore

)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

Import-Module $PSScriptRoot\ScriptUtilities.psm1 -Force
Import-Module $PSScriptRoot\Projects.psm1 -Force

$ScriptName = $([io.Path]::GetFileNameWithoutExtension($PSCommandPath))

if (-not $NoClean) {
    Remove-DirectorySafely $BuildRoot
}

if (-not $NoRestore) {
    $NuGetConfigFile = "$SourceRoot\NuGet.Config"

    foreach ($project in $Projects.All) {
        Write-Information "Restoring NuGet packages for $project..."
        & $RepoRoot\.nuget\NuGet.exe restore $SourceRoot\$project\$project.csproj -ConfigFile "$NuGetConfigFile" -OutputDirectory "$NuGetPackageRoot" -Verbosity quiet
        if ($LASTEXITCODE -ne 0) {
            Exit-WithFailureMessage $ScriptName "NuGet restore failed for $project."
        }
    }
}

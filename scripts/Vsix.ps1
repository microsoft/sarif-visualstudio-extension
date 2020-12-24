<#
.SYNOPSIS
    Install/Uninstall VSIX packages from sarif-visualstudio-extension.
.DESCRIPTION
    Install/Uninstall VSIX generated after build.
.PARAMETER Configuration
    The build configuration: Release or Debug. Default=Release
.PARAMETER Install
    Install the VSIX.
.PARAMETER Uninstall
    Uninstall the VSIX.
#>

[CmdletBinding()]
param(
    [string]
    [ValidateSet("Debug", "Release")]
    $Configuration="Debug",
    
    [switch]
    $Install,
    
    [switch]
    $Uninstall
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

$ScriptName = $([io.Path]::GetFileNameWithoutExtension($PSCommandPath))

Import-Module -Force $PSScriptRoot\ScriptUtilities.psm1

function Install-SarifExtension {
    $vsixInstallerPaths = Get-ChildItem "bld\bin\AnyCPU_$Configuration" "*.vsix" -Recurse
    if (-not $vsixInstallerPaths) {
        Exit-WithFailureMessage $ScriptName "Cannot install VSIX: .vsix file was not found."
    }

    foreach ($vsixInstallerPath in $vsixInstallerPaths) {
        Write-Host "Installing $vsixInstallerPath VSIX"
        Start-Process -Wait -FilePath VSIXInstaller.exe -ArgumentList "/quiet", "/force", $vsixInstallerPath.FullName
    }
}

function Uninstall-SarifExtension {
    Write-Host "Uninstalling Microsoft SARIF Viewer"
    Start-Process -Wait -FilePath VSIXInstaller.exe -ArgumentList "/quiet", "/force", '/uninstall:"Microsoft.Sarif.Viewer.Michael C. Fanning.f17e897a-fd38-4e1f-99db-19fa34a4e184"'
    
    Write-Host "Uninstalling Sarifer"
    Start-Process -Wait -FilePath VSIXInstaller.exe -ArgumentList "/quiet", "/force", '/uninstall:"2ec87711-16c7-4a58-8e1d-453f805ce112"'
}

if ($Install) {
    Install-SarifExtension
}

if ($Uninstall) {
    Uninstall-SarifExtension
}

<#
.SYNOPSIS
    Package the SARIF Viewer using binaries from the signing directory.
.DESCRIPTION
    Builds the SARIF Viewer NuGet packages with signed binaries from
    the signing directory.
.PARAMETER Configuration
    The build configuration: Release or Debug. Default=Release
#>

[CmdletBinding()]
param(
    [string]
    [ValidateSet("Debug", "Release")]
    $Configuration="Release"
)

Import-Module -Force $PSScriptRoot\ScriptUtilities.psm1
Import-Module -Force $PSScriptRoot\Projects.psm1

# Copy signed binaries back into the normal directory structure.
function Copy-FromSigningDirectory {
    Write-Information "Copying signed binaries from signing directory..."
    $SigningDirectory = "$BinRoot\Signing"

    foreach ($project in $Projects.Product) {
        $projectBinDirectory = (Get-ProjectBinDirectory $project $configuration)

        foreach ($framework in $Frameworks) {
            $sourceDirectory = "$SigningDirectory\$framework"
            $destinationDirectory = "$projectBinDirectory\$framework"

            $fileToCopy = "$sourceDirectory\$project.dll"
            if (Test-Path $fileToCopy) {
                Write-Information "$fileToCopy $destinationDirectory"
                Copy-Item -Force -Path $fileToCopy -Destination $destinationDirectory
            }
        }
    }

    # Copy the Viewer assemblies.
    # The names don't fit the pattern binary name == project name, so we copy them by hand.
    foreach ($framework in $Frameworks) {
        Copy-Item -Force -Path $SigningDirectory\$framework\2019\* -Destination $BinRoot\${Platform}_$Configuration\Sarif.Viewer.VisualStudio\
        Copy-Item -Force -Path $SigningDirectory\$framework\2022\* -Destination $BinRoot\${Platform}_$Configuration\Sarif.Viewer.VisualStudio.2022\
    }
}

Copy-FromSigningDirectory

New-NuGetPackages $Configuration $Projects $Frameworks
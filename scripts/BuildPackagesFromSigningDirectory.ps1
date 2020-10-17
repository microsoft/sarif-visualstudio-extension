<#
.SYNOPSIS
    Package the SARIF SDK using binaries from the signing directory.
.DESCRIPTION
    Builds the SARIF SDK NuGet Packages from the signing directory after
    they have been signed.
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
    Write-Information "Copying files from signing directory..."
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

    # Copy the viewer. Its name doesn't fit the pattern binary name == project name,
    # so we copy it by hand.
    foreach ($framework in $Frameworks) {
        Copy-Item -Force -Path $SigningDirectory\$framework\Microsoft.Sarif.Viewer.dll -Destination $BinRoot\${Platform}_$Configuration\Sarif.Viewer.VisualStudio\Microsoft.Sarif.Viewer.dll
    }
}

Copy-FromSigningDirectory

New-NuGetPackages $Configuration $Projects $Frameworks
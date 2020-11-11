<#
.SYNOPSIS
    Utility functions.

.DESCRIPTION
    The ScriptUtilties module exports generally useful functions to PowerShell
    scripts in the SARIF SDK.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

function Remove-DirectorySafely($dir) {
    if (Test-Path $dir) {
        Write-Verbose "Removing directory $dir..."
        Remove-Item -Force -Recurse $dir
    }
}

function New-DirectorySafely($dir) {
    if (-not (Test-Path $dir)) {
        Write-Verbose "Creating directory $dir..."
        New-Item -Type Directory $dir | Out-Null
    } else {
        Write-Verbose "Directory $dir already exists."
    }
}

function Exit-WithFailureMessage($scriptName, $message) {
    Write-Information "${scriptName}: $message"
    Write-Information "$scriptName FAILED."
    exit 1
}

function New-NuGetPackageFromNuspecFile($configuration, $project, $version, $framework, $suffix = "") {
    $nuspecFile = "$SourceRoot\$project\$project.nuspec"

    $arguments=
        "pack", $nuspecFile,
        "-Symbols",
        "-Properties", "platform=$Platform;configuration=$configuration;project=$project;version=$version;framework=$framework",
        "-Verbosity", "Quiet",
        "-BasePath", ".\",
        "-OutputDirectory", (Get-PackageDirectoryName $configuration)

    if ($suffix -ne "") {
        $arguments += "-Suffix", $Suffix
    }

    $nugetExePath = "$RepoRoot\.nuget\NuGet.exe"

    Write-Debug "$nuGetExePath $($arguments -join ' ')"

    &$nuGetExePath $arguments
    if ($LASTEXITCODE -ne 0) {
        Exit-WithFailureMessage $ScriptName "$project NuGet package creation failed."
    }

    Write-Information "Created package '$BinRoot\NuGet\$Configuration\$Project.$version.nupkg' for $framework."
}

function New-NuGetPackages($configuration, $projects, $frameworks) {
    dotnet tool install --global nbgv --version 3.3.37
    $version = nbgv get-version -p src -v Version
    foreach ($project in $Projects.NuGet) {
        Write-Information $project
        foreach ($framework in $frameworks) {
            New-NuGetPackageFromNuSpecFile $configuration $project $version $framework
        }
    }
}

# Get the packaging directory name.
function Get-PackageDirectoryName($configuration) {
    Join-Path $PackageOutputDirectoryRoot $configuration
}

function Get-ProjectBinDirectory($project, $configuration)
{
    "$BinRoot\${Platform}_$configuration\$project\"
}

$RepoRoot = $(Resolve-Path $PSScriptRoot\..).Path
$Platform = "AnyCPU"
$SourceRoot = "$RepoRoot\src"
$NuGetPackageRoot = "$SourceRoot\packages"
$JsonSchemaPath = "$SourceRoot\Sarif\Schemata\Sarif.schema.json"
$BuildRoot = "$RepoRoot\bld"
$BinRoot = "$BuildRoot\bin"
$PackageOutputDirectoryRoot = Join-Path $BinRoot NuGet

$SarifExtension = ".sarif"

Export-ModuleMember -Function `
    Exit-WithFailureMessage, `
    New-DirectorySafely, `
    Remove-DirectorySafely, `
    New-NuGetPackages, `
    New-NuGetPackageFromNuSpecFile, `
    Get-PackageDirectoryName, `
    Get-ProjectBinDirectory

Export-ModuleMember -Variable `
    RepoRoot, `
    SourceRoot, `
    NuGetPackageRoot, `
    JsonSchemaPath, `
    BuildRoot, `
    BinRoot, `
    SarifExtension, `
    Platform
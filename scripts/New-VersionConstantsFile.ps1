<#
.SYNOPSIS
Uses the information in the file CurrentVersion.xml to synthesize a file containing
compilation constants used to set the version attributes of the assembly being built.
#>

param(
    [Parameter(Mandatory=$true)] $OutputDirectory,
    [Parameter(Mandatory=$true)] $Namespace
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

$versionPrefix, $versionSuffix = & "$PSScriptRoot\Get-VersionConstants.ps1"

$versionConstantsFileContents =
@"
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace $Namespace
{
    public static class VersionConstants
    {
        public const string Prerelease = "$versionSuffix";
        public const string AssemblyVersion = "$versionPrefix";
        public const string FileVersion = AssemblyVersion + ".0";
        public const string Version = AssemblyVersion + Prerelease;
    }
}
"@

$outputFile = Join-Path $OutputDirectory Properties\VersionConstants.cs

# We use .NET rather than the PowerShell Set-Content cmdlet because Set-Content
# intermittently fails with "Stream was not readable".
[System.IO.File]::WriteAllText($outputFile, $versionConstantsFileContents)


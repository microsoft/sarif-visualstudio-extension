<#
.SYNOPSIS
    Unzips archives to a specified path.
.DESCRIPTION
    Unzips archives to a specified path.
.PARAMETER Archive
    The path to the archive
.PARAMETER ExtractTo
    The directory to which the archive should be expanded
#>

[CmdletBinding()]
param(
    [string]
    $Archive,

    [string]
    $ExtractTo
)

[System.Console]::WriteLine($Archive)
[System.Console]::WriteLine($ExtractTo)

Add-Type -AssemblyName System.IO.Compression.FileSystem

[System.IO.Compression.ZipFile]::ExtractToDirectory($archive, $extractTo)

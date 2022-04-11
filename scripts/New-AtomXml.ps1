<#
.SYNOPSIS
    Create an atom.xml file for a NuGet feed that offers pre-release versions of the viewer.
.DESCRIPTION
    To make versions of the viewer available to a limited audience before they are published to the
    Marketplace (https://marketplace.visualstudio.com/items?itemName=WDGIS.MicrosoftSarifViewer),
    you can set up a private NuGet feed. This script creates the atom.xml file for such a feed.

    This script writes the atom.xml file contents to the standard output.
.PARAMETER FeedUri
    The feed's URI. The atom.xml file must be made available at "$FeedUri/atom.xml".
.PARAMETER FeedTitle
    The feed's title.
.PARAMETER FeedId
    The feed's unique identifier.
.PARAMETER OutputPath
    The path to the created atom.xml file.
.PARAMETER VsVersion
    The VS version target.
#>

[CmdletBinding()]
param(
    [string]
    [Parameter(Mandatory=$true)]
    [ValidatePattern("^https?://.+")]
    $FeedUri,

    [string]
    [Parameter(Mandatory=$true)]
    $FeedTitle,

    [string]
    [Parameter(Mandatory=$true)]
    $FeedId,

    [string]
    [Parameter(Mandatory=$true)]
    $OutputPath,

    [string]
    [Parameter(Mandatory=$true)]
	[ValidateSet("2019","2022")]
    $VsVersion
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

function Get-Version {  
    dotnet tool update --global nbgv --version 3.3.37
    nbgv get-version --project src --variable Version
}
$now = (Get-Date).ToUniversalTime().ToString("O");
$vsixId = "Microsoft.Sarif.Viewer.Michael C. Fanning.f17e897a-fd38-4e1f-99db-19fa34a4e184";
$version = Get-Version;

@"
<feed xmlns="http://www.w3.org/2005/Atom">
    <title type="text">$FeedTitle $VsVersion</title>
    <id>$FeedId</id>
    <updated>$now</updated>
    <entry>
        <id>$vsixId</id>
        <title type="text">SARIF Viewer VSIX for Visual Studio $VsVersion</title>
        <summary type="text">Extension for viewing SARIF log files.</summary>
        <published>$now</published>
        <updated>$now</updated>
        <author>
            <name>1ES Security Tools Team</name>
        </author>
        <content type="application/octet-stream" src="$FeedUri-$VsVersion/Microsoft.Sarif.Viewer.vsix"/>
        <Vsix xmlns="http://schemas.microsoft.com/developer/vsx-syndication-schema/2010" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
            <Id>$vsixId</Id>
            <Version>$version</Version>
            <References/>
            <Rating xsi:nil="true"/>
            <RatingCount xsi:nil="true"/>
            <DownloadCount xsi:nil="true"/>
        </Vsix>
        <link rel="releasenotes" type="text/markdown" href="https://github.com/microsoft/sarif-visualstudio-extension/blob/master/src/ReleaseHistory.md"/>
        <link rel="icon" href="$FeedUri-$VsVersion/Triskele.ico"/>
    </entry>
</feed>
"@ | Out-File $OutputPath

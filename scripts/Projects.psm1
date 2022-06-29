<#
.SYNOPSIS
    Provides a list of SARIF SDK projects and frameworks.
.DESCRIPTION
    The Projects module exports variables whose properties specify the
    various kinds of projects in the SARIF SDK, and the frameworks for which
    they are built.
#>

# .NET Framework versions for which we build.
$Frameworks = @("net472")

$Projects = @{}
$Projects.Vsix = @(
	"Sarif.Viewer.VisualStudio.2022",
	"Sarif.Viewer.VisualStudio",
	"Sarif.Sarifer.2022",
	"Sarif.Sarifer")
$Projects.NuGet = @("Sarif.Viewer.VisualStudio.Interop")
$Projects.Library = @(
	"Sarif.Viewer.VisualStudio.ResultSources.ACL",
	"Sarif.Viewer.VisualStudio.ResultSources.Domain.2022",
	"Sarif.Viewer.VisualStudio.ResultSources.Domain",
	"Sarif.Viewer.VisualStudio.Shell.2022",
	"Sarif.Viewer.VisualStudio.Shell",
	"Sarif.Viewer.VisualStudio.ResultSources.Factory",
	"Sarif.Viewer.VisualStudio.ResultSources.GitHubAdvancedSecurity.2022",
	"Sarif.Viewer.VisualStudio.ResultSources.GitHubAdvancedSecurity")
$Projects.Product = $Projects.Vsix + $Projects.NuGet + $Projects.Library
$Projects.Test = @(
	"Sarif.Viewer.VisualStudio.UnitTests",
	"Sarif.Sarifer.UnitTests")
$Projects.All = $Projects.Product + $Projects.Test

Export-ModuleMember -Variable Frameworks, Projects
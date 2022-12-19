<#
.SYNOPSIS
    Provides a list of SARIF Viewer projects and frameworks.
.DESCRIPTION
    The Projects module exports variables whose properties specify the
    various kinds of projects in the SARIF Viewer, and the frameworks
	for which they are built.
#>

# .NET Framework versions for which we build.
$Frameworks = @("net472")

$Projects = @{}
$Projects.Vsix = @(
	"Sarif.Viewer.VisualStudio.2022",
	"Sarif.Viewer.VisualStudio")
$Projects.NuGet = @("Sarif.Viewer.VisualStudio.Interop")
$Projects.Library = @(
	"Sarif.Viewer.VisualStudio.GitHelper.2022",
	"Sarif.Viewer.VisualStudio.GitHelper",
	"Sarif.Viewer.VisualStudio.ResultSources.ACL.2022",
	"Sarif.Viewer.VisualStudio.ResultSources.ACL",
	"Sarif.Viewer.VisualStudio.ResultSources.Domain.2022",
	"Sarif.Viewer.VisualStudio.ResultSources.Domain",
	"Sarif.Viewer.VisualStudio.ResultSources.Factory.2022",
	"Sarif.Viewer.VisualStudio.ResultSources.Factory",
	"Sarif.Viewer.VisualStudio.ResultSources.GitHubAdvancedSecurity.2022",
	"Sarif.Viewer.VisualStudio.ResultSources.GitHubAdvancedSecurity",
	"Sarif.Viewer.VisualStudio.ResultSources.GitHubAdvancedSecurity.Resources",
	"Sarif.Viewer.VisualStudio.Shell.2022",
	"Sarif.Viewer.VisualStudio.Shell")
$Projects.Product = $Projects.Vsix + $Projects.NuGet
$Projects.Test = @(
	"Sarif.Viewer.VisualStudio.ResultSources.Factory.UnitTests",
	"Sarif.Viewer.VisualStudio.ResultSources.GitHubAdvancedSecurity.UnitTests",
	"Sarif.Viewer.VisualStudio.UnitTests")
$Projects.All = $Projects.Product + $Projects.Test + $Projects.Library

Export-ModuleMember -Variable Frameworks, Projects
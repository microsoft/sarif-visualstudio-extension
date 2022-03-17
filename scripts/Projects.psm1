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
$Projects.Vsix = @("Sarif.Viewer.VisualStudio.2022", "Sarif.Viewer.VisualStudio", "Sarif.Sarifer.2022", "Sarif.Sarifer", "Sarif.Viewer.VisualStudio.ResultSources.ACL", "Sarif.Viewer.VisualStudio.ResultSources.Domain")
$Projects.NuGet = @("Sarif.Viewer.VisualStudio.Interop")
$Projects.Product = $Projects.Vsix + $Projects.NuGet
$Projects.Test = @("Sarif.Viewer.VisualStudio.UnitTests", "Sarif.Sarifer.UnitTests")
$Projects.All = $Projects.Product + $Projects.Test

Export-ModuleMember -Variable Frameworks, Projects
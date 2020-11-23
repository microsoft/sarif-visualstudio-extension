// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.Design;

using EnvDTE;

using EnvDTE80;

using Microsoft.Sarif.Viewer.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    internal class AnalyzeProjectCommand
    {
        private readonly SarifViewerInterop viewerInterop;

        public AnalyzeProjectCommand(IVsShell vsShell, IMenuCommandService menuCommandService)
        {
            this.viewerInterop = new SarifViewerInterop(vsShell);

            var menuCommand = new MenuCommand(
                new EventHandler(this.MenuCommandCallback),
                new CommandID(Guids.SariferCommandSet, SariferPackageCommandIds.AnalyzeProject));

            menuCommandService.AddCommand(menuCommand);
        }

        /// <summary>
        /// Event handler called when the user selects the Analyze Project command.
        /// </summary>
        private void MenuCommandCallback(object caller, EventArgs args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            object[] activeSolutionProjects = dte.ActiveSolutionProjects as object[];
            if (activeSolutionProjects != null)
            {
                foreach (object item in activeSolutionProjects)
                {
                    var project = item as Project;
                    string message = project != null
                        ? $"Project: {project.FullName}"
                        : $"Not a project: '{item.GetType().FullName}': '{item}'";

                    VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider,
                               message,
                               null, // title
                               OLEMSGICON.OLEMSGICON_INFO,
                               OLEMSGBUTTON.OLEMSGBUTTON_OK,
                               OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                }
            }
        }
    }
}

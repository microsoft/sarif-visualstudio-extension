// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.Design;

using Microsoft.Sarif.Viewer.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    internal class AnalyzeSolutionCommand
    {
        private readonly SarifViewerInterop viewerInterop;

        public AnalyzeSolutionCommand(IVsShell vsShell, IMenuCommandService menuCommandService)
        {
            this.viewerInterop = new SarifViewerInterop(vsShell);

            var menuCommand = new MenuCommand(
                new EventHandler(this.MenuCommandCallback),
                new CommandID(Guids.SariferCommandSet, SariferPackageCommandIds.AnalyzeSolution));

            menuCommandService.AddCommand(menuCommand);
        }

        /// <summary>
        /// Event handler called when the user selects the Analyze Project command.
        /// </summary>
        private void MenuCommandCallback(object caller, EventArgs args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider,
                       "Analyze solution",
                       null, // title
                       OLEMSGICON.OLEMSGICON_INFO,
                       OLEMSGBUTTON.OLEMSGBUTTON_OK,
                       OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}

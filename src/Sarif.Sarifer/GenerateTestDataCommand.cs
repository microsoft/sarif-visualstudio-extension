// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.Design;
using Microsoft.Sarif.Viewer.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    internal class GenerateTestDataCommand : MenuCommand
    {
        private static SarifViewerInterop s_viewerInterop;

        public GenerateTestDataCommand(IVsShell vsShell) :
            base(
                new EventHandler(MenuCommandCallback),
                new CommandID(Guids.SariferCommandSet, SariferPackageCommandIds.GenerateTestData))
        {
            s_viewerInterop = new SarifViewerInterop(vsShell);
        }

        /// <summary>
        /// Event handler called when the user selects the Generate SARIF Test Data command.
        /// </summary>
        private static void MenuCommandCallback(object caller, EventArgs args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // TODO: Why does this never return true?
            if (!s_viewerInterop.IsViewerExtensionLoaded)
            {
                s_viewerInterop.LoadViewerExtension();
            }
        }
    }
}

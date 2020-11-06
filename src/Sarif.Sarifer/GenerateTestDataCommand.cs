// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.Design;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Sarif.Viewer.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    internal class GenerateTestDataCommand
    {
        private const string SendDataToViewerFailureEventName = "SendDataToViewer/Failure";

        private readonly SarifViewerInterop viewerInterop;

        public GenerateTestDataCommand(IVsShell vsShell, IMenuCommandService menuCommandService)
        {
            this.viewerInterop = new SarifViewerInterop(vsShell);

            MenuCommand menuCommand = new MenuCommand(
                new EventHandler(this.MenuCommandCallback),
                new CommandID(Guids.SariferCommandSet, SariferPackageCommandIds.GenerateTestData));

            menuCommandService.AddCommand(menuCommand);
        }

        /// <summary>
        /// Event handler called when the user selects the Generate SARIF Test Data command.
        /// </summary>
        private void MenuCommandCallback(object caller, EventArgs args)
        {
            this.SendDataToViewerAsync().FileAndForget(FileAndForget.EventName(SendDataToViewerFailureEventName));
        }

        private async Task SendDataToViewerAsync()
        {
            Stream testDataStream = GetTestDataStream();

            // TODO: Why does this never return true?
            if (!viewerInterop.IsViewerExtensionLoaded)
            {
                this.viewerInterop.LoadViewerExtension();
            }

            await this.viewerInterop.OpenSarifLogAsync(testDataStream).ConfigureAwait(continueOnCapturedContext: false);
        }

        private static Stream GetTestDataStream() =>
            Assembly.GetExecutingAssembly().GetManifestResourceStream("TestData.ProofOfConcept.sarif");
    }
}

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
            string testDataFilePath = await CreateTestDataFileAsync().ConfigureAwait(continueOnCapturedContext: false);

            // TODO: Why does this never return true?
            if (!viewerInterop.IsViewerExtensionLoaded)
            {
                this.viewerInterop.LoadViewerExtension();
            }

            await this.viewerInterop.OpenSarifLogAsync(testDataFilePath).ConfigureAwait(continueOnCapturedContext: false);
        }

        private static async Task<string> CreateTestDataFileAsync()
        {
            using (Stream testDataResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TestData.ProofOfConcept.sarif"))
            using (TextReader reader = new StreamReader(testDataResourceStream))
            {
                // No need to continue on the UI thread because we're just doing file I/O.
                string testDataFileContents = await reader.ReadToEndAsync().ConfigureAwait(continueOnCapturedContext: false);

                string testDataFilePath = Path.GetTempFileName();
                using (var writer = new StreamWriter(testDataFilePath))
                {
                    await writer.WriteAsync(testDataFileContents).ConfigureAwait(continueOnCapturedContext: false);
                }

                return testDataFilePath;
            }
        }
    }
}

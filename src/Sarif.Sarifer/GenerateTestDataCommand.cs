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
        /// <remarks>
        /// Since this is a menu item callback, it must return void.
        /// </remarks>
#pragma warning disable VSTHRD100 // Avoid async void methods
        private static async void MenuCommandCallback(object caller, EventArgs args)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            string testDataFilePath = await CreateTestDataFileAsync();

            // TODO: Why does this never return true?
            if (!s_viewerInterop.IsViewerExtensionLoaded)
            {
                s_viewerInterop.LoadViewerExtension();
            }

            await s_viewerInterop.OpenSarifLogAsync(testDataFilePath);
        }

        private static async Task<string> CreateTestDataFileAsync()
        {
            using (Stream testDataResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TestData.ProofOfConcept.sarif"))
            using (TextReader reader = new StreamReader(testDataResourceStream))
            {
                string testDataFileContents = await reader.ReadToEndAsync();

                string testDataFilePath = Path.GetTempFileName();
                File.WriteAllText(testDataFilePath, testDataFileContents); // Pity there's no async version.

                return testDataFilePath;
            }
        }
    }
}

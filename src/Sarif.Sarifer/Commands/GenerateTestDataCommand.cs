// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.Design;
using System.IO;
using System.Reflection;

using Microsoft.Sarif.Viewer.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer.Commands
{
    internal class GenerateTestDataCommand
    {
        private const string ProofOfConceptResourceName = "Microsoft.CodeAnalysis.Sarif.Sarifer.TestData.ProofOfConcept.sarif";

        private readonly SarifViewerInterop viewerInterop;

        public GenerateTestDataCommand(IVsShell vsShell, IMenuCommandService menuCommandService)
        {
            this.viewerInterop = new SarifViewerInterop(vsShell);

            var menuCommand = new MenuCommand(
                new EventHandler(this.MenuCommandCallback),
                new CommandID(Guids.SariferCommandSet, SariferPackageCommandIds.GenerateTestData));

            menuCommandService.AddCommand(menuCommand);
        }

        /// <summary>
        /// Event handler called when the user selects the Generate SARIF Test Data command.
        /// </summary>
        private void MenuCommandCallback(object caller, EventArgs args)
        {
            Stream testDataStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ProofOfConceptResourceName);

            // TODO: Why does this never return true?
            if (!this.viewerInterop.IsViewerExtensionLoaded)
            {
                this.viewerInterop.LoadViewerExtension();
            }

            this.viewerInterop.OpenSarifLogAsync(testDataStream).FileAndForget(FileAndForgetEventName.SendDataToViewerFailure);
        }
    }
}

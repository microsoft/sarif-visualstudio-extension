// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.Composition;
using System.IO;

using Microsoft.Sarif.Viewer.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Newtonsoft.Json;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// A sink for background analysis results that sends the results to the SARIF viewer through
    /// its interop API.
    /// </summary>
    [Export(typeof(IBackgroundAnalysisSink))]
    internal class SarifViewerBackgroundAnalysisSink : IBackgroundAnalysisSink
    {
        // TODO: LazyCreate sarifViewerInterop.
        /// <inheritdoc/>
        public void Receive(SarifLog log)
        {
            // TODO: This is TEMPORARY. We write to a file because the PR that provides a stream-
            // based interop API is not yet approved.
            // https://github.com/microsoft/sarif-visualstudio-extension/pull/259
            string tempPath = Path.GetTempFileName();
            File.WriteAllText(tempPath, JsonConvert.SerializeObject(log, Formatting.Indented));

            OpenSarifLogAsync(tempPath).FileAndForget(FileAndForgetEventName.SendDataToViewerFailure);
        }

        private async static Task OpenSarifLogAsync(string tempPath)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IVsShell shell = Package.GetGlobalService(typeof(SVsShell)) as IVsShell;

            var sarifViewerInterop = new SarifViewerInterop(shell);

            await sarifViewerInterop.OpenSarifLogAsync(tempPath).ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}

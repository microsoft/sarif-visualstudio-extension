// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;

using Microsoft.Sarif.Viewer.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;

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
        private readonly ReaderWriterLockSlimWrapper interopLock = new ReaderWriterLockSlimWrapper(new ReaderWriterLockSlim());
        private SarifViewerInterop sarifViewerInterop;

        /// <inheritdoc/>
        public async Task ReceiveAsync(Stream logStream, string logId)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            SarifViewerInterop sarifViewerInterop = await this.GetInteropObjectAsync().ConfigureAwait(continueOnCapturedContext: true);

            await sarifViewerInterop.OpenSarifLogAsync(logStream, logId).ConfigureAwait(continueOnCapturedContext: false);
        }

        /// <inheritdoc/>
        public async Task CloseAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            SarifViewerInterop sarifViewerInterop = await this.GetInteropObjectAsync().ConfigureAwait(continueOnCapturedContext: true);

            await sarifViewerInterop.CloseAllSarifLogsAsync().ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task CloseAsync(IEnumerable<string> paths)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            SarifViewerInterop sarifViewerInterop = await this.GetInteropObjectAsync().ConfigureAwait(continueOnCapturedContext: true);

            await sarifViewerInterop.CloseSarifLogAsync(paths).ConfigureAwait(false);
        }

        private async System.Threading.Tasks.Task<SarifViewerInterop> GetInteropObjectAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            using (this.interopLock.EnterWriteLock())
            {
                if (this.sarifViewerInterop == null)
                {
                    var shell = Package.GetGlobalService(typeof(SVsShell)) as IVsShell;
                    this.sarifViewerInterop = new SarifViewerInterop(shell);
                }
            }

            return this.sarifViewerInterop;
        }
    }
}

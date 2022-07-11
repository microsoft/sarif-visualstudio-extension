// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.Sarif.Viewer.Services
{
    /// <inheritdoc/>
    public class DataService : SDataService, IDataService
    {
        /// <inheritdoc/>
        public void SendEnhancedResultData(SarifLog sarifLog)
        {
            this.SendEnhancedResultDataAsync(sarifLog).FileAndForget(nameof(DataService));
        }

#pragma warning disable IDE0060 // Remove unused parameter
        private async Task SendEnhancedResultDataAsync(SarifLog sarifLog)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            /*
             * SarifExplorerWindow.ResetSelection
             * SarifExplorerWindow.Show
             * SarifErrorListEventProcessor.PreprocessNavigate
             * How to switch to code flows tab
                Parse the SARIF fragment provided
                Bring the Sarif Viewer window to the view and present the warning details, including the Key Events (aka code flow)
                Bring the corresponding source code to the view in source code editor window, navigate to the corresponding source line, and add adornment for Key Events
             */

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            SarifExplorerWindow.Find()?.Show();
        }
    }
}

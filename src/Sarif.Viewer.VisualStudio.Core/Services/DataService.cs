// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;

using EnvDTE;

using EnvDTE80;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.Sarif.Viewer.Telemetry;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

using Newtonsoft.Json;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.Sarif.Viewer.Services
{
    /// <inheritdoc/>
    public class DataService : SDataService, IDataService
    {
        public const string EnhancedResultDataLogName = "EnhancedResultData";

        private readonly IComponentModel componentModel;

        private readonly ISarifErrorListEventSelectionService sarifErrorListEventSelectionService;

        private readonly KeyEventTelemetry keyEventTelemetry;

        public DataService()
        {
            this.componentModel = (IComponentModel)AsyncPackage.GetGlobalService(typeof(SComponentModel));
            this.sarifErrorListEventSelectionService = this.componentModel.GetService<ISarifErrorListEventSelectionService>();
            this.keyEventTelemetry = new KeyEventTelemetry();
        }

        /// <inheritdoc/>
        public int SendEnhancedResultData(Stream stream)
        {
            Assumes.NotNull(stream);

            SarifLog sarifLog = null;
            try
            {
                sarifLog = SarifLog.Load(stream);
            }
            catch (JsonException) { }

            if (sarifLog != null)
            {
                return this.SendEnhancedResultData(sarifLog);
            }

            return -1;
        }

        /// <inheritdoc/>
        public int SendEnhancedResultData(SarifLog sarifLog)
        {
            Assumes.NotNull(sarifLog);

            int cookie = -1;

            ThreadHelper.JoinableTaskFactory.Run(async () => cookie = await this.SendEnhancedResultDataAsync(sarifLog));

            return cookie;
        }

        /// <inheritdoc/>
        public void CloseEnhancedResultData(int cookie)
        {
            this.CloseEnhancedResultDataAsync().FileAndForget(Constants.FileAndForgetFaultEventNames.SendEnhancedData);
        }

        private async System.Threading.Tasks.Task<int> SendEnhancedResultDataAsync(SarifLog sarifLog)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            Assumes.NotNull(sarifLog);
            Assumes.True(sarifLog.Runs?.Count == 1);

            int runIndex = -1;

            if (this.componentModel != null)
            {
                await ErrorListService.CloseSarifLogItemsAsync(new string[] { EnhancedResultDataLogName });

                runIndex = CodeAnalysisResultManager.Instance.GetNextRunIndex();
                var dataCache = new RunDataCache(EnhancedResultDataLogName, sarifLog);
                CodeAnalysisResultManager.Instance.RunIndexToRunDataCache.Add(runIndex, dataCache);

                var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
                var projectNameCache = new ProjectNameCache(dte?.Solution);
                Run run = sarifLog.Runs.First();

                foreach (Result r in run.Results)
                {
                    var sarifErrorListItem = new SarifErrorListItem(run, runIndex, r, null, projectNameCache);
                    sarifErrorListItem.PopulateAdditionalPropertiesIfNot();
                    dataCache.AddSarifResult(sarifErrorListItem);
                }

                this.sarifErrorListEventSelectionService.NavigatedItem = dataCache.SarifErrors[0];
                this.sarifErrorListEventSelectionService.SelectedItem = dataCache.SarifErrors[0];

                dataCache.SarifErrors[0].Locations?.FirstOrDefault()?.NavigateTo(usePreviewPane: false, moveFocusToCaretLocation: true);

                this.keyEventTelemetry.TrackEvent(KeyEventTelemetry.EventNames.DisplayKeyEventData, item: dataCache.SarifErrors[0], pathIndex: null);
            }

            SarifExplorerWindow.Find()?.Show();

            return runIndex;
        }

        private async Task CloseEnhancedResultDataAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            await ErrorListService.CloseSarifLogItemsAsync(new string[] { EnhancedResultDataLogName });

            this.sarifErrorListEventSelectionService.NavigatedItem = null;
            this.sarifErrorListEventSelectionService.SelectedItem = null;

            SarifExplorerWindow.Find()?.Close();
        }
    }
}

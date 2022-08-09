// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using EnvDTE;

using EnvDTE80;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

using Newtonsoft.Json;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.Sarif.Viewer.Services
{
    /// <inheritdoc/>
    public class DataService : SDataService, IDataService
    {
        private readonly IComponentModel componentModel = (IComponentModel)AsyncPackage.GetGlobalService(typeof(SComponentModel));

        /// <inheritdoc/>
        public void SendEnhancedResultData(Stream stream)
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
                this.SendEnhancedResultData(sarifLog);
            }
        }

        /// <inheritdoc/>
        public void SendEnhancedResultData(SarifLog sarifLog)
        {
            SendEnhancedResultDataAsync(sarifLog).FileAndForget(Constants.FileAndForgetFaultEventNames.SendEnhancedData);
        }

        private async Task SendEnhancedResultDataAsync(SarifLog sarifLog)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            Assumes.NotNull(sarifLog);
            Assumes.True(sarifLog.Runs?.Count == 1);

            if (this.componentModel != null)
            {
                int runIndex = CodeAnalysisResultManager.Instance.GetNextRunIndex();
                var dataCache = new RunDataCache(runIndex, "EnhancedResultData", sarifLog);
                CodeAnalysisResultManager.Instance.RunIndexToRunDataCache.Add(runIndex, dataCache);

                var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
                var projectNameCache = new ProjectNameCache(dte?.Solution);
                var items = new List<SarifErrorListItem>();
                Run run = sarifLog.Runs.First();

                foreach (Result r in run.Results)
                {
                    var sarifErrorListItem = new SarifErrorListItem(run, 0, r, string.Empty, projectNameCache);
                    sarifErrorListItem.PopulateAdditionalPropertiesIfNot();
                    items.Add(sarifErrorListItem);
                    dataCache.SarifErrors.Add(sarifErrorListItem);
                }

                ISarifErrorListEventSelectionService sarifErrorListEventSelectionService = componentModel.GetService<ISarifErrorListEventSelectionService>();
                sarifErrorListEventSelectionService.NavigatedItem = items[0];
                sarifErrorListEventSelectionService.SelectedItem = items[0];

                items[0].Locations?.FirstOrDefault()?.NavigateTo(usePreviewPane: false, moveFocusToCaretLocation: true);
            }

            SarifExplorerWindow.Find()?.Show();
        }
    }
}

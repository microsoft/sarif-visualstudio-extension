// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using EnvDTE;

using EnvDTE80;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.Sarif.Viewer.Services
{
    /// <inheritdoc/>
    public class DataService : SDataService, IDataService
    {
        private readonly HashSet<Result> keyEventsResultCache;

        public DataService()
        {
            this.keyEventsResultCache = new HashSet<Result>();
        }

        /// <inheritdoc/>
        public void SendEnhancedResultData(SarifLog sarifLog)
        {
            this.SendEnhancedResultDataAsync(sarifLog).FileAndForget(Constants.FileAndForgetFaultEventNames.SendEnhancedData);
        }

        private async Task SendEnhancedResultDataAsync(SarifLog sarifLog)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            Assumes.NotNull(sarifLog);
            Assumes.True(sarifLog.Runs?.Count == 1);

            var componentModel = (IComponentModel)AsyncPackage.GetGlobalService(typeof(SComponentModel));
            if (componentModel != null)
            {
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
                var projectNameCache = new ProjectNameCache(dte?.Solution);

                Result result = sarifLog.Runs.First().Results.First();
                var sarifErrorListItem = new SarifErrorListItem(sarifLog.Runs.First(), 0, result, string.Empty, projectNameCache);
                sarifErrorListItem.PopulateAdditionalPropertiesIfNot();

                ISarifErrorListEventSelectionService sarifErrorListEventSelectionService = componentModel.GetService<ISarifErrorListEventSelectionService>();
                sarifErrorListEventSelectionService.NavigatedItem = sarifErrorListItem;

                sarifErrorListItem.Locations?.FirstOrDefault()?.NavigateTo(usePreviewPane: false, moveFocusToCaretLocation: true);
            }

            SarifExplorerWindow.Find()?.Show();
        }
    }
}

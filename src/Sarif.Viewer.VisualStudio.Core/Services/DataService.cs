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

            string logPath = $"{Guid.NewGuid()}.sarif";

            await ErrorListService.ProcessSarifLogAsync(sarifLog, logPath, cleanErrors: false, openInEditor: false, monitorSarifFile: false);

            SarifErrorListItem sarifItem = null;
            IList<SarifErrorListItem> sarifErrorListItems = CodeAnalysisResultManager.Instance.CurrentRunDataCache.SarifErrors;

            if (sarifErrorListItems?.Any() == true)
            {
                sarifErrorListItems.ToList().ForEach(item => item?.PopulateAdditionalPropertiesIfNot());
                sarifItem = sarifErrorListItems.First();
            }

            if (sarifItem != null)
            {
                sarifItem.PopulateAdditionalPropertiesIfNot();

                ISarifErrorListEventSelectionService sarifErrorListEventSelectionService = this.componentModel.GetService<ISarifErrorListEventSelectionService>();

                sarifErrorListEventSelectionService.NavigatedItem = sarifItem;
                sarifErrorListEventSelectionService.SelectedItem = sarifItem;

                sarifItem.Locations?.FirstOrDefault()?.NavigateTo(usePreviewPane: false, moveFocusToCaretLocation: true);
            }

            SarifExplorerWindow.Find()?.Show();
        }
    }
}

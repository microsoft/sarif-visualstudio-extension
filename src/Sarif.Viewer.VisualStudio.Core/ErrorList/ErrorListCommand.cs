// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Controls;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Models;
using Microsoft.Sarif.Viewer.Sarif;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

using ResultSourcesConstants = Microsoft.Sarif.Viewer.ResultSources.Domain.Models.Constants;

namespace Microsoft.Sarif.Viewer.ErrorList
{
    /// <summary>
    /// Command handler.
    /// </summary>
    internal sealed class ErrorListCommand
    {
        /// <summary>
        /// Command id for "Clear all SARIF results".
        /// </summary>
        public const int ClearSarifResultsCommandId = 0x0300;

        public const int ProvideFeedbackCommandId = 0x0301;

        /// <summary>
        /// Command id for "Useful result".
        /// </summary>
        public const int UsefulResultCommandId = 0x0302;

        /// <summary>
        /// Command id for "False positive result".
        /// </summary>
        public const int FalsePositiveResultCommandId = 0x0303;

        /// <summary>
        /// Command id for "Non-actionable result".
        /// </summary>
        public const int NonActionableResultCommandId = 0x0304;

        /// <summary>
        /// Command id for "Low value result".
        /// </summary>
        public const int LowValueResultCommandId = 0x0305;

        /// <summary>
        /// Command id for "Non-shipping code result".
        /// </summary>
        public const int NonShippingCodeResultCommandId = 0x0306;

        /// <summary>
        /// Command id for "Other result".
        /// </summary>
        public const int OtherResultCommandId = 0x0307;

        /// <summary>
        /// Command id for "Suppress result in log file".
        /// </summary>
        public const int SuppressResultCommandId = 0x0308;

        /// <summary>
        /// Command id for "I Fixed This!".
        /// </summary>
        public const int IFixedThisCommandId = 0x0309;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("76648814-13bf-4ecf-ad5c-2a7e2953e62f");

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid ResultSourceServiceCommandSet = new Guid("b04424d9-49bc-4e04-9ecc-ad5b68cce4bc");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Service for accessing menu commands.
        /// </summary>
        private readonly IMenuCommandService menuCommandService;

        /// <summary>
        /// Service that provides access to the currently selected Error List item, if any.
        /// </summary>
        private readonly ISarifErrorListEventSelectionService selectionService;

        /// <summary>
        /// Task list service.
        /// </summary>
        private readonly IVsTaskList2 vsTaskList2Service;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorListCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file).
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private ErrorListCommand(Package package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.package = package ?? throw new ArgumentNullException(nameof(package));

            this.menuCommandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as IMenuCommandService;
            Assumes.Present(this.menuCommandService);

            this.AddMenuItem(ClearSarifResultsCommandId);
            this.AddMenuItem(ProvideFeedbackCommandId);
            this.AddMenuItem(UsefulResultCommandId);
            this.AddMenuItem(FalsePositiveResultCommandId);
            this.AddMenuItem(NonActionableResultCommandId);
            this.AddMenuItem(LowValueResultCommandId);
            this.AddMenuItem(NonShippingCodeResultCommandId);
            this.AddMenuItem(OtherResultCommandId);
            this.AddMenuItem(SuppressResultCommandId);
            this.AddMenuItem(IFixedThisCommandId);

            // hide by default
            this.SetCommandVisibility(ProvideFeedbackCommandId, false);
            this.SetCommandVisibility(ClearSarifResultsCommandId, false);
            this.SetCommandVisibility(SuppressResultCommandId, false);
            this.SetCommandVisibility(IFixedThisCommandId, false);

            var componentModel = this.ServiceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
            Assumes.Present(componentModel);

            this.selectionService = componentModel.GetService<ISarifErrorListEventSelectionService>();
            this.selectionService.SelectedItemChanged += this.SelectionService_SelectedItemChanged;
            Assumes.Present(this.selectionService);

            if (this.ServiceProvider.GetService(typeof(SVsErrorList)) is IVsTaskList2 taskListService)
            {
                this.vsTaskList2Service = taskListService;
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static ErrorListCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider => this.package;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new ErrorListCommand(package);
        }

        internal void ResultSourceServiceMenuCommand_Invoke(object sender)
        {
            // This handler extracts the SARIF logs from the error list items and passes them to the result source service.
            // This is necessary becuse the service can't circular-reference the Viewer project.
            if (this.selectionService.SelectedItems is List<SarifErrorListItem> selectedItems
                && sender is OleMenuCommand menuCommand)
            {
                if (menuCommand.Properties.Contains(ResultSourcesConstants.ResultSourceServiceMenuCommandInvokeCallbackKey)
                    && menuCommand.Properties[ResultSourcesConstants.ResultSourceServiceMenuCommandInvokeCallbackKey] is Func<MenuCommandInvokedEventArgs, Task<ResultSourceServiceAction>> callback)
                {
                    foreach (SarifErrorListItem item in selectedItems)
                    {
                        if (item.SarifResult != null)
                        {
                            var eventArgs = new MenuCommandInvokedEventArgs(new[] { item.SarifResult }, menuCommand);
                            ResultSourceServiceAction action = ThreadHelper.JoinableTaskFactory.Run(async () => await callback(eventArgs));

                            if (action == ResultSourceServiceAction.DismissSelectedItem)
                            {
                                SarifTableDataSource.Instance.RemoveError(item);
                            }
                        }
                    }
                }
            }
        }

        internal void ResultSourceServiceMenuItem_BeforeQueryStatus(object sender, ErrorListMenuItem menuItem)
        {
            if (sender is OleMenuCommand menuCommand)
            {
                // We'd prefer to disable a flyout, but it doesn't seem possible.
                // So we just have to hide it instead.
                menuCommand.Text = menuItem.Text;
                menuCommand.Enabled = false;
                menuCommand.Visible = false;

                if (this.selectionService.SelectedItems is List<SarifErrorListItem> selectedItems)
                {
                    // At least one SARIF item is selected.
                    int selectedItemCount = ThreadHelper.JoinableTaskFactory.Run(async () => await this.GetSelectedErrorListItemCountAsync());
                    var sarifResults = selectedItems.Select(o => o.SarifResult).ToList();
                    var eventArgs = new MenuCommandBeforeQueryStatusEventArgs(sarifResults, selectedItemCount);

                    ResultSourceServiceAction action = ThreadHelper.JoinableTaskFactory.Run(async () => await menuItem.BeforeQueryStatusMenuCommand(eventArgs));
                    menuCommand.Enabled = menuCommand.Visible = action != ResultSourceServiceAction.DisableMenuCommand;
                }
            }
        }

        private async Task<int> GetSelectedErrorListItemCountAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            int count = 0;
            this.vsTaskList2Service.GetSelectionCount(out count);
            return count;
        }

        private void SelectionService_SelectedItemChanged(object sender, SarifErrorListSelectionChangedEventArgs e)
        {
            bool visible = e.NewItem != null || (this.selectionService.SelectedItems != null && this.selectionService.SelectedItems.Any());
            this.SetCommandVisibility(ProvideFeedbackCommandId, visible);
            this.SetCommandVisibility(SuppressResultCommandId, visible);
            this.SetCommandVisibility(IFixedThisCommandId, visible);
        }

        private void SetCommandVisibility(int cmdID, bool visible)
        {
            var newCmdID = new CommandID(CommandSet, cmdID);
            MenuCommand mc = menuCommandService.FindCommand(newCmdID);
            if (mc != null)
            {
                mc.Visible = visible;
            }
        }

        private void AddMenuItem(int commandId)
        {
            var menuCommandID = new CommandID(CommandSet, commandId);
            var menuItem = new OleMenuCommand(this.MenuItemCallback, menuCommandID);
            menuItem.BeforeQueryStatus += this.MenuItem_BeforeQueryStatus;
            this.menuCommandService.AddCommand(menuItem);
        }

        private void MenuItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            var menuCommand = (MenuCommand)sender;
            if (menuCommand.CommandID.ID == ClearSarifResultsCommandId)
            {
                this.SetCommandVisibility(ClearSarifResultsCommandId,
                    visible: CodeAnalysisResultManager.Instance.RunIndexToRunDataCache.Count != 0);
            }
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var menuCommand = (MenuCommand)sender;

            // Clear Sarif Result command should be function no matter if any selected item
            IEnumerable<SarifErrorListItem> selectedItems = this.selectionService.SelectedItems;
            if ((selectedItems == null || !selectedItems.Any())
                && menuCommand.CommandID.ID != ClearSarifResultsCommandId)
            {
                return;
            }

            switch (menuCommand.CommandID.ID)
            {
                case ClearSarifResultsCommandId:
                    ErrorListService.CleanAllErrors();
                    break;

                case UsefulResultCommandId:
                    var feedback = new FeedbackModel(
                        selectedItems.GetCombinedRuleIds(),
                        selectedItems.GetCombinedToolNames(),
                        selectedItems.GetCombinedToolVersions(),
                        selectedItems.GetCombinedSnippets(),
                        FeedbackType.UsefulResult,
                        null,
                        CodeAnalysisResultManager.Instance.GetPartitionedLog(selectedItems));
                    ErrorListService.SendFeedback(feedback);
                    break;

                case FalsePositiveResultCommandId:
                case NonActionableResultCommandId:
                case LowValueResultCommandId:
                case NonShippingCodeResultCommandId:
                case OtherResultCommandId:
                    DisplayFeedbackDialog(menuCommand.CommandID.ID, selectedItems);
                    break;

                case SuppressResultCommandId:
                    SuppressResults(selectedItems);
                    break;

                case IFixedThisCommandId:
                    foreach (SarifErrorListItem item in selectedItems)
                    {
                        SarifTableDataSource.Instance.RemoveError(item);
                    }

                    break;

                default:
                    // Unrecognized command; do nothing.
                    break;
            }
        }

        private struct FeedbackInfo
        {
            public readonly string Description;
            public readonly FeedbackType FeedbackType;
            public readonly string Summary;

            public FeedbackInfo(string description, FeedbackType feedbackType, string summary)
            {
                this.Description = description;
                this.FeedbackType = feedbackType;
                this.Summary = summary;
            }
        }

        private static readonly ReadOnlyDictionary<int, FeedbackInfo> s_commandToResultDescriptionDictionary = new ReadOnlyDictionary<int, FeedbackInfo>(
            new Dictionary<int, FeedbackInfo>
            {
                [FalsePositiveResultCommandId] = new FeedbackInfo(Resources.FalsePositiveResult, FeedbackType.FalsePositiveResult, Resources.FalsePositiveSummary),
                [NonActionableResultCommandId] = new FeedbackInfo(Resources.NonActionableResult, FeedbackType.NonActionableResult, Resources.NonActionableSummary),
                [LowValueResultCommandId] = new FeedbackInfo(Resources.LowValueResult, FeedbackType.LowValueResult, Resources.LowValueSummary),
                [NonShippingCodeResultCommandId] = new FeedbackInfo(Resources.NonShippingCodeResult, FeedbackType.NonShippingCodeResult, Resources.NonShippingCodeSummary),
                [OtherResultCommandId] = new FeedbackInfo(Resources.OtherResult, FeedbackType.OtherResult, Resources.OtherSummary),
            });

        private static void DisplayFeedbackDialog(int commandId, IEnumerable<SarifErrorListItem> sarifErrorListItems)
        {
            FeedbackInfo feedbackInfo = s_commandToResultDescriptionDictionary[commandId];
            string title = string.Format(Resources.ReportResultTitle, feedbackInfo.Description);

            var feedback = new FeedbackModel(
                sarifErrorListItems.GetCombinedRuleIds(),
                sarifErrorListItems.GetCombinedToolNames(),
                sarifErrorListItems.GetCombinedToolVersions(),
                sarifErrorListItems.GetCombinedSnippets(),
                feedbackInfo.FeedbackType,
                feedbackInfo.Summary,
                CodeAnalysisResultManager.Instance.GetPartitionedLog(sarifErrorListItems));

            var feedbackDialog = new FeedbackDialog(title, feedback);
            feedbackDialog.ShowModal();
        }

        private static void SuppressResults(IEnumerable<SarifErrorListItem> sarifErrorListItems)
        {
            // only added Accepted suppression now
            CodeAnalysisResultManager.Instance.AddSuppressionToSarifLog(
                new SuppressionModel(sarifErrorListItems)
                {
                    Status = SuppressionStatus.Accepted,
                    Kind = SuppressionKind.External,
                });
        }
    }
}

﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Linq;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Controls;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.Sarif.Viewer.Sarif;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

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
        /// Command id for dynamic command 1.
        /// </summary>
        public const int DynamicCommand1 = 0x0301;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("76648814-13bf-4ecf-ad5c-2a7e2953e62f");

        // VS Package that provides this command, not null.
        private readonly Package package;

        // Service for accessing menu commands.
        private readonly IMenuCommandService menuCommandService;

        // Service that provides access to the currently selected Error List item, if any.
        private readonly ISarifErrorListEventSelectionService selectionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorListCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file).
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private ErrorListCommand(Package package)
        {
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
            this.AddMenuItem(DynamicCommand1);

            // hide by default
            this.SetCommandVisibility(ProvideFeedbackCommandId, false);
            this.SetCommandVisibility(ClearSarifResultsCommandId, false);
            this.SetCommandVisibility(SuppressResultCommandId, false);
            this.SetCommandVisibility(IFixedThisCommandId, false);
            this.SetCommandVisibility(DynamicCommand1, false);

            var componentModel = this.ServiceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
            Assumes.Present(componentModel);

            this.selectionService = componentModel.GetService<ISarifErrorListEventSelectionService>();
            this.selectionService.SelectedItemChanged += this.SelectionService_SelectedItemChanged;
            Assumes.Present(this.selectionService);
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
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new ErrorListCommand(package);
        }

        private void SelectionService_SelectedItemChanged(object sender, SarifErrorListSelectionChangedEventArgs e)
        {
            bool visible = e.NewItem != null || (this.selectionService.SelectedItems != null && this.selectionService.SelectedItems.Any());
            this.SetCommandVisibility(ProvideFeedbackCommandId, visible);
            this.SetCommandVisibility(SuppressResultCommandId, visible);
            this.SetCommandVisibility(IFixedThisCommandId, visible);

            // Need to provide an event for result sources to enable/disable their menu items
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

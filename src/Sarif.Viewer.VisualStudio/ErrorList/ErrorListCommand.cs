﻿// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;

using Microsoft.Sarif.Viewer.Controls;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.Sarif.Viewer.ErrorList
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class ErrorListCommand
    {
        /// <summary>
        /// Command id for "Clear all SARIF results".
        /// </summary>
        public const int ClearSarifResultsCommandId = 0x0300;

        /// <summary>
        /// Command id for "Useful result".
        /// </summary>
        public const int UsefulResultCommandId = 0x0302;

        /// <summary>
        /// Command id for "False positive"
        /// </summary>
        public const int FalsePositiveResultCommandId = 0x0303;

        /// <summary>
        /// Command id for "Non-actionable"
        /// </summary>
        public const int NonActionableResultCommandId = 0x0304;

        /// <summary>
        /// Command id for "Low value"
        /// </summary>
        public const int LowValueResultCommandId = 0x0305;

        /// <summary>
        /// Command id for "Non-shipping code"
        /// </summary>
        public const int NonShippingCodeResultCommandId = 0x0306;

        /// <summary>
        /// Command id for "Other"
        /// </summary>
        public const int OtherResultCommandId = 0x0307;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("76648814-13bf-4ecf-ad5c-2a7e2953e62f");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Service for accessing menu commands.
        /// </summary>
        private readonly IMenuCommandService menuCommandService;

        // Service that provides access to the currently selected Error List item, if any.
        private readonly ISarifErrorListEventSelectionService selectionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorListCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private ErrorListCommand(Package package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));

            this.menuCommandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as IMenuCommandService;
            Assumes.Present(this.menuCommandService);

            this.AddMenuItem(ClearSarifResultsCommandId);
            this.AddMenuItem(UsefulResultCommandId);
            this.AddMenuItem(FalsePositiveResultCommandId);
            this.AddMenuItem(NonActionableResultCommandId);
            this.AddMenuItem(LowValueResultCommandId);
            this.AddMenuItem(NonShippingCodeResultCommandId);
            this.AddMenuItem(OtherResultCommandId);

            var componentModel = this.ServiceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
            Assumes.Present(componentModel);

            this.selectionService = componentModel.GetService<ISarifErrorListEventSelectionService>();
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

        private void AddMenuItem(int commandId)
        {
            var menuCommandID = new CommandID(CommandSet, commandId);
            var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
            this.menuCommandService.AddCommand(menuItem);
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

            if (this.selectionService.SelectedItem == null)
            {
                return;
            }

            var menuCommand = (MenuCommand)sender;
            switch (menuCommand.CommandID.ID)
            {
                case ClearSarifResultsCommandId:
                    ErrorListService.CleanAllErrors();
                    break;

                case UsefulResultCommandId:
                    VsShellUtilities.ShowMessageBox(Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider,
                               "\"Useful result\" menu item clicked",
                               null, // title
                               OLEMSGICON.OLEMSGICON_INFO,
                               OLEMSGBUTTON.OLEMSGBUTTON_OK,
                               OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    break;

                case FalsePositiveResultCommandId:
                case NonActionableResultCommandId:
                case LowValueResultCommandId:
                case NonShippingCodeResultCommandId:
                case OtherResultCommandId:
                    DisplayFeedbackDialog(menuCommand.CommandID.ID);
                    break;

                default:
                    // Unrecognized command; do nothing.
                    break;
            }
        }

        private static readonly ReadOnlyDictionary<int, string> s_feedbackTypeDictionary = new ReadOnlyDictionary<int, string>(
            new Dictionary<int, string>
            {
                [FalsePositiveResultCommandId] = Resources.FalsePositiveResult,
                [NonActionableResultCommandId] = Resources.NonActionableResult,
                [LowValueResultCommandId] = Resources.LowValueResult,
                [NonShippingCodeResultCommandId] = Resources.NonShippingCodeResult,
                [OtherResultCommandId] = Resources.OtherResult
            });

        private static void DisplayFeedbackDialog(int commandId)
        {
            string feedbackType = s_feedbackTypeDictionary[commandId];
            string title = string.Format(Resources.ReportResultTitle, feedbackType);
            var feedbackDialog = new FeedbackDialog(title);
            feedbackDialog.ShowModal();
        }
    }
}

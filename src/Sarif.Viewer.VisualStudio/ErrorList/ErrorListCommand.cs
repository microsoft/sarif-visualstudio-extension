// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;

using Microsoft.Sarif.Viewer.Controls;
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
        /// Command id for "Not actionable"
        /// </summary>
        public const int NonactionableResultCommandId = 0x0304;

        /// <summary>
        /// Command id for "Low value"
        /// </summary>
        public const int LowValueResultCommandId = 0x0305;

        /// <summary>
        /// Command id for "Non-shipping code"
        /// </summary>
        public const int NonshippingCodeResultCommandId = 0x0306;

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
        /// Initializes a new instance of the <see cref="ErrorListCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private ErrorListCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, ClearSarifResultsCommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);

                menuCommandID = new CommandID(CommandSet, UsefulResultCommandId);
                menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);

                menuCommandID = new CommandID(CommandSet, FalsePositiveResultCommandId);
                menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);

                menuCommandID = new CommandID(CommandSet, NonactionableResultCommandId);
                menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);

                menuCommandID = new CommandID(CommandSet, LowValueResultCommandId);
                menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);

                menuCommandID = new CommandID(CommandSet, NonshippingCodeResultCommandId);
                menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);

                menuCommandID = new CommandID(CommandSet, OtherResultCommandId);
                menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
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
            MenuCommand menuCommand = (MenuCommand)sender;

            switch (menuCommand.CommandID.ID)
            {
                case ClearSarifResultsCommandId:
                    ErrorListService.CleanAllErrors();
                    break;

                case UsefulResultCommandId:
                    VsShellUtilities.ShowMessageBox(Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider,
                               "\"Yes\" menu item clicked",
                               null, // title
                               OLEMSGICON.OLEMSGICON_INFO,
                               OLEMSGBUTTON.OLEMSGBUTTON_OK,
                               OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    break;

                case FalsePositiveResultCommandId:
                case NonactionableResultCommandId:
                case LowValueResultCommandId:
                case NonshippingCodeResultCommandId:
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
                [FalsePositiveResultCommandId] = Resources.FalsePositiveFeedbackType,
                [NonactionableResultCommandId] = Resources.NotActionableFeedbackType,
                [LowValueResultCommandId] = Resources.LowValueFeedbackType,
                [NonshippingCodeResultCommandId] = Resources.CodeDoesNotShipFeedbackType,
                [OtherResultCommandId] = Resources.OtherFeedbackType
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

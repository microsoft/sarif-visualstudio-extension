// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows.Forms;

using Microsoft.CodeAnalysis.Sarif.Converters;
using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.Sarif.Viewer.Services;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.Sarif.Viewer
{
    /// <summary>
    /// Command handler.
    /// </summary>
    internal sealed class OpenLogFileCommands
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int ImportAnalysisLogCommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("a236a757-af66-4cf0-a3c8-facbb61d5cf1");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// The prefix for the resources that give the names of the filters in the open log dialog.
        /// </summary>
        /// <remarks>
        /// The resources are in the form of 'Import{ToolFormat}Filter'.
        /// </remarks>
        internal const string FilterResourceNamePrefix = "Import";

        /// <summary>
        /// The suffix for the resources that give the names of the filters in the open log dialog.
        /// </summary>
        /// <remarks>
        /// The resources are in the form of 'Import{ToolFormat}Filter'.
        /// </remarks>
        internal const string FilterResourceNameSuffix = "Filter";

        /// <summary>
        /// The name of the setting used to store the user's last selected open log format.
        /// </summary>
        internal const string ToolFormatSettingName = "OpenLogFileToolFormat";

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenLogFileCommands"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file).
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private OpenLogFileCommands(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            this.package = package;

            var commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var oleCommand = new OleMenuCommand(
                        this.MenuItemCallback,
                        new CommandID(CommandSet, ImportAnalysisLogCommandId));
                oleCommand.ParametersDescription = "$";

                commandService.AddCommand(oleCommand);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static OpenLogFileCommands Instance
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
            Instance = new OpenLogFileCommands(package);
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
            this.MenuItemCallbackAsync(sender, e).FileAndForget(Constants.FileAndForgetFaultEventNames.OpenSarifLogMenu);
        }

        private async System.Threading.Tasks.Task MenuItemCallbackAsync(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var menuCommand = (OleMenuCommand)sender;
            var menuCmdEventArgs = (OleMenuCmdEventArgs)e;

            string inputFile = menuCmdEventArgs.InValue as string;
            string logFile = null;

            if (!string.IsNullOrWhiteSpace(inputFile))
            {
                // If the input file is a URL, download the file.
                if (Uri.IsWellFormedUriString(inputFile, UriKind.Absolute))
                {
                    TryDownloadFile(inputFile, out logFile);
                }
                else
                {
                    // Verify if the input file is valid. i.e. it exists and has a valid file extension.
                    string logFileExtension = Path.GetExtension(inputFile);

                    // Since we don't have a tool format, only accept *.sarif and *.json files as command input files.
                    if (logFileExtension.Equals(".sarif", StringComparison.OrdinalIgnoreCase) || logFileExtension.Equals(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        if (File.Exists(inputFile))
                        {
                            logFile = inputFile;
                        }
                    }
                }
            }

            string toolFormat = ToolFormat.None;

            if (logFile == null)
            {
                FieldInfo[] toolFormatFieldInfos = typeof(ToolFormat).GetFields();
                var fieldInfoToOpenFileDialogFilterDisplayString = new List<KeyValuePair<FieldInfo, string>>(toolFormatFieldInfos.Length);

                // Note that "ImportNoneFilter" represents the SARIF file filter (which matches what the code logic does below as well).
                foreach (FieldInfo fieldInfo in toolFormatFieldInfos)
                {
                    string resourceName = string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", FilterResourceNamePrefix, fieldInfo.Name, FilterResourceNameSuffix);
                    string openFileDialogFilterString = Resources.ResourceManager.GetString(resourceName, CultureInfo.CurrentCulture);
                    fieldInfoToOpenFileDialogFilterDisplayString.Add(new KeyValuePair<FieldInfo, string>(fieldInfo, openFileDialogFilterString));
                }

                // Sort the filters by their display strings so the user has a nice alphabetized list with import SARIF at the top.
                KeyValuePair<FieldInfo, string> noneFieldInfo = fieldInfoToOpenFileDialogFilterDisplayString.
                    Single(kvp => kvp.Key.Name.Equals(nameof(ToolFormat.None), StringComparison.OrdinalIgnoreCase));

                // Linq's OrderBy does the right sorting..
                // It ultimately does CultureInfo.CurrentCulture.CompareInfo.Compare(this, strB, CompareOptions.None);
                IEnumerable<KeyValuePair<FieldInfo, string>> orderedFilters =
                    Enumerable.Repeat(noneFieldInfo, 1).Concat(
                        fieldInfoToOpenFileDialogFilterDisplayString.Where(kvp => kvp.Key != noneFieldInfo.Key).
                            OrderBy(kvp => kvp.Value));

                var openFileDialog = new OpenFileDialog()
                {
                    Title = Resources.ImportLogOpenFileDialogTitle,
                    Filter = string.Join("|", orderedFilters.Select(kvp => kvp.Value)),
                    RestoreDirectory = true,
                    Multiselect = false,
                };

                if (!string.IsNullOrWhiteSpace(inputFile))
                {
                    openFileDialog.FileName = Path.GetFileName(inputFile);
                    openFileDialog.InitialDirectory = Path.GetDirectoryName(inputFile);
                }

                // Read the user's last tool format selection.
                int collectionExists;
                var vsSettingsManager = Package.GetGlobalService(typeof(SVsSettingsManager)) as IVsSettingsManager;
                if (vsSettingsManager != null &&
                    vsSettingsManager.GetReadOnlySettingsStore((uint)__VsEnclosingScopes.EnclosingScopes_UserSettings, out IVsSettingsStore vsSettingsStore) == VSConstants.S_OK &&
                    vsSettingsStore.CollectionExists(nameof(SarifViewerPackage), out collectionExists) == VSConstants.S_OK &&
                    collectionExists != 0 &&
                    vsSettingsStore.GetString(nameof(SarifViewerPackage), ToolFormatSettingName, out string openLogFileToolFormat) == VSConstants.S_OK)
                {
                    int? filterIndex = null;
                    int currentIndex = 0;

                    foreach (FieldInfo fieldInfo in orderedFilters.Select(kvp => kvp.Key))
                    {
                        if (fieldInfo.Name.Equals(openLogFileToolFormat, StringComparison.Ordinal))
                        {
                            filterIndex = currentIndex;
                            break;
                        }

                        currentIndex++;
                    }

                    if (filterIndex.HasValue)
                    {
                        // The filter index in the open file dialog is 1 base.
                        openFileDialog.FilterIndex = filterIndex.Value + 1;
                    }
                }

                if (openFileDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                // The filter index in the open file dialog is 1 base.
                toolFormat = orderedFilters.Skip(openFileDialog.FilterIndex - 1).First().Key.GetValue(null) as string;

                // Write the user's last tool format selection.
                if (vsSettingsManager != null &&
                    vsSettingsManager.GetWritableSettingsStore((uint)__VsEnclosingScopes.EnclosingScopes_UserSettings, out IVsWritableSettingsStore vsWritableSettingsStore) == VSConstants.S_OK)
                {
                    if (vsWritableSettingsStore.CollectionExists(nameof(SarifViewerPackage), out collectionExists) != VSConstants.S_OK ||
                        collectionExists == 0)
                    {
                        vsWritableSettingsStore.CreateCollection(nameof(SarifViewerPackage));
                    }

                    vsWritableSettingsStore.SetString(nameof(SarifViewerPackage), ToolFormatSettingName, toolFormat);
                }

                logFile = openFileDialog.FileName;
            }

            try
            {
                await ErrorListService.ProcessLogFileAsync(logFile, toolFormat, promptOnLogConversions: true, cleanErrors: true, openInEditor: true).ConfigureAwait(continueOnCapturedContext: false);
                new DataService().CloseEnhancedResultData(cookie: 0);
            }
            catch (InvalidOperationException)
            {
                VsShellUtilities.ShowMessageBox(Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider,
                                                string.Format(Resources.LogOpenFail_InvalidFormat_DialogMessage, Path.GetFileName(logFile)),
                                                null, // title
                                                OLEMSGICON.OLEMSGICON_CRITICAL,
                                                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                                                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        private static string ConvertSarifProtocol(string inputUrl)
        {
            int sarifProtocolLength;
            string replacementProtocol;

            // sarif:/// ==> file://
            // sarif://  ==> http://
            if (inputUrl.StartsWith("sarif:///", StringComparison.OrdinalIgnoreCase))
            {
                sarifProtocolLength = 9;
                replacementProtocol = "file://";
            }
            else if (inputUrl.StartsWith("sarif://", StringComparison.OrdinalIgnoreCase))
            {
                sarifProtocolLength = 8;
                replacementProtocol = "http://";
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(inputUrl), $"The input URL does not use a known protocol. {inputUrl}");
            }

            string newUrl = inputUrl.Substring(sarifProtocolLength);
            newUrl = replacementProtocol + newUrl;
            return newUrl;
        }

        private static bool TryDownloadFile(string inputUrl, out string downloadedFilePath)
        {
            var inputUri = new Uri(inputUrl, UriKind.Absolute);
            downloadedFilePath = Path.GetTempFileName();

            string downloadUrl;
            if (inputUri.Scheme.Equals("sarif", StringComparison.OrdinalIgnoreCase))
            {
                downloadUrl = ConvertSarifProtocol(inputUrl);
            }
            else if (inputUri.Scheme.Equals("http://", StringComparison.OrdinalIgnoreCase) || inputUri.Scheme.Equals("file://", StringComparison.OrdinalIgnoreCase))
            {
                downloadUrl = inputUrl;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(inputUrl), $"The input URL does not use a known protocol. {inputUrl}");
            }

            if (downloadUrl != null)
            {
                try
                {
                    using (var webClient = new WebClient())
                    {
                        webClient.UseDefaultCredentials = true;
                        webClient.DownloadFile(downloadUrl, downloadedFilePath);
                    }
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.ToString());
                    File.Delete(downloadedFilePath);
                    downloadedFilePath = null;
                }
            }

            return File.Exists(downloadedFilePath);
        }
    }
}

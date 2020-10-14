// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.CodeAnalysis.Sarif.Converters;
using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.Sarif.Viewer
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class OpenLogFileCommands
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int ImportAnalysLogCommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("a236a757-af66-4cf0-a3c8-facbb61d5cf1");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenLogFileCommands"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private OpenLogFileCommands(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                OleMenuCommand oleCommand = new OleMenuCommand(
                        this.MenuItemCallback,
                        new CommandID(CommandSet, ImportAnalysLogCommandId));
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

            OleMenuCommand menuCommand = (OleMenuCommand)sender;
            OleMenuCmdEventArgs menuCmdEventArgs = (OleMenuCmdEventArgs)e;

            string inputFile = menuCmdEventArgs.InValue as string;
            string logFile = null;

            if (!String.IsNullOrWhiteSpace(inputFile))
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

                // Need two additional entries for "SARIF" and "All" which are at the beginning
                // and end of the list respectively.
                List<string> toolFormatFilters = new List<string>(toolFormatFieldInfos.Length + 2)
                {
                    Resources.ImportSARIFFilter,
                };

                // Keep a dictionary of filter index to the tool format string
                // just in case the SDK and supported converters gets out of sync
                // with this extension.
                // We have a "test" to "try" to catch this, but that test is only good IF
                // the extension solution and the test solution are using the same
                // converter nuget package.
                Dictionary<int, string> filterIndexToToolFormat = new Dictionary<int, string>(toolFormatFieldInfos.Length)
                {
                    { 0, ToolFormat.None }
                };

                foreach (FieldInfo fieldInfo in toolFormatFieldInfos)
                {
                    if (fieldInfo.Name.Equals(ToolFormat.None, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string filterString = Resources.ResourceManager.GetString(string.Format(CultureInfo.InvariantCulture, "Import{0}Filter", fieldInfo.Name), CultureInfo.CurrentCulture);
                    Debug.Assert(!string.IsNullOrEmpty(filterString), "Why should have resources for the filters for all types of conversions we support");

                    if (!string.IsNullOrEmpty(filterString))
                    {
                        // We want the list to start with "import SARIF" and end with "All file"
                        // so insert this after "import SARIF".
                        toolFormatFilters.Add(filterString);
                        filterIndexToToolFormat.Add(toolFormatFilters.Count, fieldInfo.Name);
                    }
                }

                OpenFileDialog openFileDialog = new OpenFileDialog();

                openFileDialog.Title = Resources.ImportLogOpenFileDialogTitle;
                openFileDialog.Filter = string.Join("|", toolFormatFilters);
                openFileDialog.RestoreDirectory = true;

                if (!String.IsNullOrWhiteSpace(inputFile))
                {
                    openFileDialog.FileName = Path.GetFileName(inputFile);
                    openFileDialog.InitialDirectory = Path.GetDirectoryName(inputFile);
                }

                if (openFileDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                toolFormat = filterIndexToToolFormat[openFileDialog.FilterIndex];
                logFile = openFileDialog.FileName;
            }

            TelemetryProvider.WriteMenuCommandEvent(toolFormat);

            try
            {
                await ErrorListService.ProcessLogFileAsync(logFile, toolFormat, promptOnLogConversions: true, cleanErrors: true).ConfigureAwait(continueOnCapturedContext: false);
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

        string ConvertSarifProtocol(string inputUrl)
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

        bool TryDownloadFile(string inputUrl, out string downloadedFilePath)
        {
            Uri inputUri = new Uri(inputUrl, UriKind.Absolute);
            downloadedFilePath = Path.GetTempFileName();
            string downloadUrl = null;

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
                    using (WebClient webClient = new WebClient())
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

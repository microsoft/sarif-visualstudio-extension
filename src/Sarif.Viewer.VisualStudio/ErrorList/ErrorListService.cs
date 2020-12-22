// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

using EnvDTE;

using EnvDTE80;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Converters;
using Microsoft.CodeAnalysis.Sarif.Readers;
using Microsoft.CodeAnalysis.Sarif.VersionOne;
using Microsoft.CodeAnalysis.Sarif.Visitors;
using Microsoft.CodeAnalysis.Sarif.Writers;
using Microsoft.Sarif.Viewer.Controls;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.Sarif.Viewer.Sarif;
using Microsoft.Sarif.Viewer.Tags;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TaskStatusCenter;

using Newtonsoft.Json;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.Sarif.Viewer.ErrorList
{
    public class ErrorListService
    {
        private readonly ColumnFilterer columnFilterer = new ColumnFilterer();

        public static readonly ErrorListService Instance = new ErrorListService();

        internal static event EventHandler<LogProcessedEventArgs> LogProcessed;

        static ErrorListService()
        {
            LogProcessed += ErrorListService_LogProcessed;
        }

        public static void ProcessLogFile(string filePath, string toolFormat, bool promptOnLogConversions, bool cleanErrors, bool openInEditor)
        {
            ThreadHelper.JoinableTaskFactory.Run(() => ProcessLogFileWrapperAsync(filePath, toolFormat, promptOnLogConversions, cleanErrors, openInEditor));
        }

        public static async Task ProcessLogFileWrapperAsync(string filePath, string toolFormat, bool promptOnLogConversions, bool cleanErrors, bool openInEditor)
        {
            var taskStatusCenterService = (IVsTaskStatusCenterService)Package.GetGlobalService(typeof(SVsTaskStatusCenterService));
            var taskProgressData = new TaskProgressData
            {
                CanBeCanceled = false,
                ProgressText = null,
            };

            string fileName = Path.GetFileName(filePath);

            var taskHandlerOptions = new TaskHandlerOptions
            {
                ActionsAfterCompletion = CompletionActions.None,
                TaskSuccessMessage = string.Format(CultureInfo.CurrentCulture, Resources.CompletedProcessingLogFileFormat, fileName),
                Title = string.Format(CultureInfo.CurrentCulture, Resources.ProcessingLogFileFormat, fileName),
            };

            ITaskHandler taskHandler = taskStatusCenterService.PreRegister(taskHandlerOptions, taskProgressData);

            Task task = ProcessLogFileAsync(filePath, toolFormat, promptOnLogConversions, cleanErrors, openInEditor);
            taskHandler.RegisterTask(task);

            await task.ConfigureAwait(continueOnCapturedContext: false);
        }

        public static async Task ProcessLogFileAsync(string filePath, string toolFormat, bool promptOnLogConversions, bool cleanErrors, bool openInEditor)
        {
            try
            {
                await ProcessLogFileCoreAsync(filePath, toolFormat, promptOnLogConversions, cleanErrors, openInEditor);
            }
            catch (JsonReaderException)
            {
                RaiseLogProcessed(ExceptionalConditions.InvalidJson);
            }
        }

        public static async Task ProcessLogFileCoreAsync(string filePath, string toolFormat, bool promptOnLogConversions, bool cleanErrors, bool openInEditor)
        {
            SarifLog log = null;
            string logText = null;
            string outputPath = null;
            bool saveOutputFile = true;

            if (toolFormat.MatchesToolFormat(ToolFormat.None))
            {
                using (var logStreamReader = new StreamReader(filePath, Encoding.UTF8))
                {
                    logText = await logStreamReader.ReadToEndAsync().ConfigureAwait(continueOnCapturedContext: false);
                }

                Match match = MatchVersionProperty(logText);
                if (match.Success)
                {
                    string inputVersion = match.Groups["version"].Value;

                    if (inputVersion == SarifUtilities.V1_0_0)
                    {
                        // They're opening a v1 log, so we need to transform it.
                        // Ask if they'd like to save the v2 log.
                        MessageDialogCommand response = promptOnLogConversions ?
                            await PromptToSaveProcessedLogAsync(Resources.TransformV1_DialogMessage).ConfigureAwait(continueOnCapturedContext: false) :
                            MessageDialogCommand.No;

                        if (response == MessageDialogCommand.Cancel)
                        {
                            return;
                        }

                        var settingsV1 = new JsonSerializerSettings()
                        {
                            ContractResolver = SarifContractResolverVersionOne.Instance
                        };

                        SarifLogVersionOne v1Log = JsonConvert.DeserializeObject<SarifLogVersionOne>(logText, settingsV1);
                        var transformer = new SarifVersionOneToCurrentVisitor();
                        transformer.VisitSarifLogVersionOne(v1Log);
                        log = transformer.SarifLog;

                        if (response == MessageDialogCommand.Yes)
                        {
                            // Prompt for a location to save the transformed log.
                            outputPath = await PromptForFileSaveLocationAsync(Resources.SaveTransformedV1Log_DialogTitle, filePath).ConfigureAwait(continueOnCapturedContext: false);

                            if (string.IsNullOrEmpty(outputPath))
                            {
                                return;
                            }
                        }

                        logText = JsonConvert.SerializeObject(log);
                    }
                    else if (inputVersion != VersionConstants.StableSarifVersion)
                    {
                        // It's an older v2 version, so send it through the pre-release compat transformer.
                        // Ask if they'd like to save the transformed log.
                        MessageDialogCommand response = promptOnLogConversions ?
                            await PromptToSaveProcessedLogAsync(string.Format(Resources.TransformPrereleaseV2_DialogMessage, VersionConstants.StableSarifVersion)).ConfigureAwait(continueOnCapturedContext: false) :
                            MessageDialogCommand.No;

                        if (response == MessageDialogCommand.Cancel)
                        {
                            return;
                        }

                        log = PrereleaseCompatibilityTransformer.UpdateToCurrentVersion(logText, Formatting.Indented, out logText);

                        if (response == MessageDialogCommand.Yes)
                        {
                            // Prompt for a location to save the transformed log.
                            outputPath = await PromptForFileSaveLocationAsync(Resources.SaveTransformedPrereleaseV2Log_DialogTitle, filePath).ConfigureAwait(continueOnCapturedContext: false);

                            if (string.IsNullOrEmpty(outputPath))
                            {
                                return;
                            }
                        }
                    }
                    else
                    {
                        // Since we didn't do any pre-processing, we don't need to write to a temp location.
                        outputPath = filePath;
                        saveOutputFile = false;
                    }
                }
                else
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    // The version property wasn't found within the first 100 characters.
                    // Per the spec, it should appear first in the sarifLog object.
                    VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider,
                                                    Resources.VersionPropertyNotFound_DialogTitle,
                                                    null, // title
                                                    OLEMSGICON.OLEMSGICON_QUERY,
                                                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                                                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return;
                }
            }
            else
            {
                // They're opening a non-SARIF log, so we need to convert it.
                // Ask if they'd like to save the converted log.
                MessageDialogCommand response = promptOnLogConversions ?
                    await PromptToSaveProcessedLogAsync(Resources.ConvertNonSarifLog_DialogMessage).ConfigureAwait(continueOnCapturedContext: false) :
                    MessageDialogCommand.No;

                if (response == MessageDialogCommand.Cancel)
                {
                    return;
                }

                // The converter doesn't have async methods, so spin
                // up a task to do this.
                await System.Threading.Tasks.Task.Run(() =>
                {
                    var sb = new StringBuilder();
                    using (FileStream fileStream = File.OpenRead(filePath))
                    {
                        using (var outputTextWriter = new StringWriter(sb))
                        using (var outputJson = new JsonTextWriter(outputTextWriter))
                        using (var output = new ResultLogJsonWriter(outputJson))
                        {
                            var converter = new ToolFormatConverter();
                            converter.ConvertToStandardFormat(toolFormat, fileStream, output);
                        }

                        logText = sb.ToString();

                        if (response == MessageDialogCommand.Yes)
                        {
                            // Prompt for a location to save the converted log.
                            outputPath = PromptForFileSaveLocationAsync(Resources.SaveConvertedLog_DialogTitle, filePath).Result;
                        }
                    }
                }).ConfigureAwait(continueOnCapturedContext: false);
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = Path.GetTempFileName() + ".sarif";
            }

            if (saveOutputFile)
            {
                await SaveLogFileAsync(outputPath, logText).ConfigureAwait(continueOnCapturedContext: false);
            }

            if (log == null)
            {
                log = JsonConvert.DeserializeObject<SarifLog>(logText);
            }

            await ProcessSarifLogAsync(log, outputPath, cleanErrors: cleanErrors, openInEditor: openInEditor).ConfigureAwait(continueOnCapturedContext: false);
        }

        /// <summary>
        /// Closes the specified SARIF log in the viewer.
        /// </summary>
        /// <param name="logFiles">The complete path to the SARIF log file.</param>
        public static void CloseSarifLogs(IEnumerable<string> logFiles)
        {
            SarifTableDataSource.Instance.ClearErrorsForLogFiles(logFiles);

            var runIdsToClear = new List<int>();

            foreach (string logFile in logFiles)
            {
                // The null conditional operator in the Where clause is necessary because log files
                // that come in through the API ILoadSarifLogService.LoadSarifLog(Stream) don't have
                // a file name. The good news is, we never close such a log file. If in future we
                // do need to close such a log file, we'll need to synthesize a log file name so we
                // know which runs belong to that file.
                runIdsToClear.AddRange(CodeAnalysisResultManager.Instance.RunIndexToRunDataCache.
                    Where(runDataCacheKvp => runDataCacheKvp.Value.LogFilePath?.Equals(logFile, StringComparison.OrdinalIgnoreCase) == true).
                    Select(runDataCacheKvp => runDataCacheKvp.Key));

                if (CodeAnalysisResultManager.Instance.RunIndexToRunDataCache
                    .Any(kvp => kvp.Value.SarifErrors
                        .Any(error => error.FileName?.Equals(logFile, StringComparison.OrdinalIgnoreCase) == true)))
                {
                    KeyValuePair<int, RunDataCache> cache = CodeAnalysisResultManager.Instance.RunIndexToRunDataCache
                        .First(kvp => kvp.Value.SarifErrors
                            .Any(error => error.FileName?.Equals(logFile, StringComparison.OrdinalIgnoreCase) == true));
                    SarifErrorListItem sarifError = cache.Value.SarifErrors.First(error => error.FileName?.Equals(logFile, StringComparison.OrdinalIgnoreCase) == true);
                    cache.Value.SarifErrors.Remove(sarifError);

                    CodeAnalysisResultManager.Instance.RunIndexToRunDataCache[cache.Key] = cache.Value;
                }
            }

            foreach (int runIdToClear in runIdsToClear)
            {
                CodeAnalysisResultManager.Instance.RunIndexToRunDataCache.Remove(runIdToClear);
            }

            SarifLocationTagHelpers.RefreshTags();
        }

        /// <summary>
        /// Closes all SARIF logs opened in the viewer.
        /// </summary>
        public static void CloseAllSarifLogs()
        {
            CleanAllErrors();
        }

        private const string VersionRegexPattern = @"""version""\s*:\s*""(?<version>[\d.]+)""";
        private const int HeadSegmentLength = 200;

        internal static Match MatchVersionProperty(string logText)
        {
            int headSegmentLength = Math.Min(logText.Length, HeadSegmentLength);
            string headSegment = logText.Substring(0, headSegmentLength);
            return Regex.Match(headSegment, VersionRegexPattern, RegexOptions.Compiled);
        }

        private static async Task<MessageDialogCommand> PromptToSaveProcessedLogAsync(string dialogMessage)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            int result = VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider,
                                                         dialogMessage,
                                                         null, // title
                                                         OLEMSGICON.OLEMSGICON_QUERY,
                                                         OLEMSGBUTTON.OLEMSGBUTTON_YESNOCANCEL,
                                                         OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            return (MessageDialogCommand)Enum.Parse(typeof(MessageDialogCommand), result.ToString());
        }

        private static async Task<string> PromptForFileSaveLocationAsync(string dialogTitle, string inputFilePath)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var saveFileDialog = new SaveFileDialog
            {
                Title = dialogTitle,
                Filter = Resources.SaveDialogFileFilter,
                RestoreDirectory = true,
                InitialDirectory = Path.GetDirectoryName(inputFilePath)
            };

            inputFilePath = Path.GetFileNameWithoutExtension(inputFilePath) + ".v2.sarif";
            saveFileDialog.FileName = Path.GetFileName(inputFilePath);

            return saveFileDialog.ShowDialog() == DialogResult.OK ?
                saveFileDialog.FileName :
                null;
        }

        private static async System.Threading.Tasks.Task SaveLogFileAsync(string filePath, string logText)
        {
            string error = null;

            try
            {
                using (StreamWriter streamWriter = File.CreateText(filePath))
                {
                    await streamWriter.WriteAsync(logText).ConfigureAwait(continueOnCapturedContext: false);
                }
            }
            catch (UnauthorizedAccessException)
            {
                error = string.Format(Resources.SaveLogFail_Access_DialogMessage, filePath);
            }
            catch (SecurityException)
            {
                error = string.Format(Resources.SaveLogFail_Access_DialogMessage, filePath);
            }
            catch (Exception ex)
            {
                error = string.Format(Resources.SaveLogFail_General_Dialog, ex.Message);
            }

            if (error != null)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider,
                                                error,
                                                null, // title
                                                OLEMSGICON.OLEMSGICON_CRITICAL,
                                                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                                                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        internal static async Task ProcessSarifLogAsync(Stream stream, string logId, bool cleanErrors, bool openInEditor)
        {
            SarifLog sarifLog = null;
            try
            {
                sarifLog = SarifLog.Load(stream);
            }
            catch (JsonReaderException)
            {
                RaiseLogProcessed(ExceptionalConditions.InvalidJson);
            }

            if (sarifLog != null)
            {
                await ProcessSarifLogAsync(sarifLog, logFilePath: logId, cleanErrors: cleanErrors, openInEditor: openInEditor);
            }
        }

        internal static async Task ProcessSarifLogAsync(SarifLog sarifLog, string logFilePath, bool cleanErrors, bool openInEditor)
        {
            // The creation of the data models must be done on the UI thread (for now).
            // VS's table data source constructs are indeed thread safe.
            // The object model (which is eventually handed to WPF\XAML) could also
            // be constructed on any thread as well.
            // However the current implementation of the data model and
            // the "run data cache" have not been augmented to support this
            // and are not thread safe.
            // This work could be done in the future to do even less work on the UI
            // thread if needed.
            if (!SarifViewerPackage.IsUnitTesting)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            }

            // Clear previous data
            if (cleanErrors)
            {
                CleanAllErrors();
            }

            bool hasResults = false;

            foreach (Run run in sarifLog.Runs)
            {
                // run.tool is required, add one if it's missing
                if (run.Tool == null)
                {
                    run.Tool = new Tool
                    {
                        Driver = new ToolComponent
                        {
                            Name = Resources.UnknownToolName
                        }
                    };
                }

                if (Instance.WriteRunToErrorList(run, logFilePath) > 0)
                {
                    hasResults = true;
                }
            }

            if (openInEditor && !SarifViewerPackage.IsUnitTesting)
            {
                SdkUIUtilities.OpenDocument(ServiceProvider.GlobalProvider, logFilePath, usePreviewPane: false);
            }

            if (hasResults)
            {
                if (!SarifViewerPackage.IsUnitTesting) // We cannot show UI during unit-tests.
                {
                    SdkUIUtilities.ShowToolWindowAsync(new Guid(ToolWindowGuids80.ErrorList), activate: false).FileAndForget(Constants.FileAndForgetFaultEventNames.ShowErrorList);
                }
            }

            RaiseLogProcessed(ExceptionalConditionsCalculator.Calculate(sarifLog));
        }

        public static void CleanAllErrors()
        {
            SarifTableDataSource.Instance.CleanAllErrors();
            CodeAnalysisResultManager.Instance.RunIndexToRunDataCache.Clear();
            SarifLocationTagHelpers.RefreshTags();
        }

        private int WriteRunToErrorList(Run run, string logFilePath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            int runIndex = CodeAnalysisResultManager.Instance.GetNextRunIndex();
            var dataCache = new RunDataCache(runIndex, logFilePath);
            CodeAnalysisResultManager.Instance.RunIndexToRunDataCache.Add(runIndex, dataCache);
            CodeAnalysisResultManager.Instance.CacheUriBasePaths(run);
            var sarifErrors = new List<SarifErrorListItem>();

            var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;

            var projectNameCache = new ProjectNameCache(dte?.Solution);

            this.StoreFileDetails(run.Artifacts);

            if (run.Results != null)
            {
                foreach (Result result in run.Results)
                {
                    result.Run = run;
                    var sarifError = new SarifErrorListItem(run, runIndex, result, logFilePath, projectNameCache);
                    sarifErrors.Add(sarifError);
                }
            }

            if (run.Invocations != null)
            {
                foreach (Invocation invocation in run.Invocations)
                {
                    if (invocation.ToolConfigurationNotifications != null)
                    {
                        foreach (Notification configurationNotification in invocation.ToolConfigurationNotifications)
                        {
                            var sarifError = new SarifErrorListItem(run, runIndex, configurationNotification, logFilePath, projectNameCache);
                            sarifErrors.Add(sarifError);
                        }
                    }

                    if (invocation.ToolExecutionNotifications != null)
                    {
                        foreach (Notification toolNotification in invocation.ToolExecutionNotifications)
                        {
                            if (toolNotification.Level != FailureLevel.Note)
                            {
                                var sarifError = new SarifErrorListItem(run, runIndex, toolNotification, logFilePath, projectNameCache);
                                sarifErrors.Add(sarifError);
                            }
                        }
                    }
                }
            }

            if (run.HasAbsentResults())
            {
                this.ShowFilteredCategoryColumn();
            }

            if (run.HasSuppressedResults())
            {
                this.ShowFilteredSuppressionStateColumn();
            }

            (dataCache.SarifErrors as List<SarifErrorListItem>).AddRange(sarifErrors);
            SarifTableDataSource.Instance.AddErrors(sarifErrors);

            // This causes already open "text views" to be tagged when SARIF logs are processed after a view is opened.
            SarifLocationTagHelpers.RefreshTags();

            return sarifErrors.Count;
        }

        // Show the Suppression State column. The first time it is shown, filter out "Suppressed"
        // results. If the user ever adjusts the filter to show Suppressed results, don't ever
        // filter them out again during the current VS session. The ColumnFilterer class
        // implements that behavior.
        private void ShowFilteredSuppressionStateColumn()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Creating this table source adds "Suppression State" to the list of available columns.
            SuppressionStateTableDataSource _ = SuppressionStateTableDataSource.Instance;

            this.columnFilterer.FilterOut(
                columnName: SarifResultTableEntry.SuppressionStateColumnName,
                filteredValue: nameof(VSSuppressionState.Suppressed));
        }

        // Show the Category column, which we currently overload to show Baseline State.
        // The first time it is shown, filter out "Absent" results. If the user ever adjusts
        // the filter to show Suppressed results, don't ever filter them out again during
        // the current VS session. The ColumnFilterer class implements that behavior.
        private void ShowFilteredCategoryColumn()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Creating this table source adds "Category" to the list of available columns.
            // (Actually, it appears to be there by default, so this might not be necessary:)
            BaselineStateTableDataSource _ = BaselineStateTableDataSource.Instance;

            this.columnFilterer.FilterOut(
                columnName: StandardTableKeyNames.ErrorCategory,
                filteredValue: nameof(BaselineState.Absent));
        }

        private void EnsureHashExists(Artifact artifact)
        {
            if (artifact.Hashes == null)
            {
                artifact.Hashes = new Dictionary<string, string>();
            }

            if (!artifact.Hashes.ContainsKey("sha-256"))
            {
                byte[] data = null;
                if (artifact.Contents?.Binary != null)
                {
                    data = Convert.FromBase64String(artifact.Contents.Binary);
                }
                else if (artifact.Contents?.Text != null)
                {
                    data = Encoding.UTF8.GetBytes(artifact.Contents.Text);
                }

                if (data != null)
                {
                    string hashString = this.GenerateHash(data);
                    artifact.Hashes.Add("sha-256", hashString);
                }
            }
        }

        internal string GenerateHash(byte[] data)
        {
            var hashFunction = new SHA256Managed();
            byte[] hash = hashFunction.ComputeHash(data);
            return hash.Aggregate(string.Empty, (current, x) => current + $"{x:x2}");
        }

        private void StoreFileDetails(IList<Artifact> artifacts)
        {
            if (artifacts == null)
            {
                return;
            }

            foreach (Artifact file in artifacts)
            {
                Uri uri = file.Location?.Uri;
                if (uri != null)
                {
                    if (file.Contents != null)
                    {
                        this.EnsureHashExists(file);
                        var fileDetails = new ArtifactDetailsModel(file);
                        CodeAnalysisResultManager.Instance.CurrentRunDataCache.FileDetails.Add(uri.ToPath(), fileDetails);
                    }
                }
            }
        }

        private static void RaiseLogProcessed(ExceptionalConditions conditions)
        {
            LogProcessed?.Invoke(Instance, new LogProcessedEventArgs(conditions));
        }

        private static void ErrorListService_LogProcessed(object sender, LogProcessedEventArgs e)
        {
            if (!SarifViewerPackage.IsUnitTesting)
            {
                InfoBar.CreateInfoBarsForExceptionalConditionsAsync(e.ExceptionalConditions).FileAndForget(FileAndForgetEventName.InfoBarOpenFailure);
            }
        }
    }
}

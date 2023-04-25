// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

using CSharpFunctionalExtensions;

using EnvDTE;

using EnvDTE80;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Converters;
using Microsoft.CodeAnalysis.Sarif.Readers;
using Microsoft.CodeAnalysis.Sarif.VersionOne;
using Microsoft.CodeAnalysis.Sarif.Visitors;
using Microsoft.CodeAnalysis.Sarif.Writers;
using Microsoft.Sarif.Viewer.CodeFinder;
using Microsoft.Sarif.Viewer.Controls;
using Microsoft.Sarif.Viewer.FileMonitor;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.Sarif.Viewer.Sarif;
using Microsoft.Sarif.Viewer.Tags;
using Microsoft.Sarif.Viewer.Telemetry;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TaskStatusCenter;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Workspace.VSIntegration.Contracts;

using Newtonsoft.Json;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.Sarif.Viewer.ErrorList
{
    /// <summary>
    /// Primarily responsible for creating <see cref="SarifErrorListItem"/>s.
    /// </summary>
    public class ErrorListService
    {
        private const string VersionRegexPattern = @"""version""\s*:\s*""(?<version>[\d.]+)""";

        private const string Sha256HashKey = "sha-256";

        private const int HeadSegmentLength = 200;

        public static readonly ErrorListService Instance = new ErrorListService();

        internal IColumnFilterer ColumnFilterer = new ColumnFilterer();

        static ErrorListService()
        {
            LogProcessed += ErrorListService_LogProcessed;
        }

        internal static event EventHandler<LogProcessedEventArgs> LogProcessed;

        /// <summary>
        /// Processes a log file on a seperate thread and fills the caches responsible for different functionality.
        /// Blocks the caller of this method.
        /// </summary>
        /// <param name="filePath">The file path of the log being processed.</param>
        /// <param name="toolFormat">The format of the tool that created the log. Types are listed in <see cref="ToolFormat"/>.</param>
        /// <param name="promptOnLogConversions">Whether to ask the user if they want to convert logs to the sarif format.</param>
        /// <param name="cleanErrors">Whether we need to clean out existing errors before refilling the caches.</param>
        /// <param name="openInEditor">Whether we need to open the file in-editor.</param>
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

            Task task = ProcessLogFileWithTracesAsync(filePath, toolFormat, promptOnLogConversions, cleanErrors, openInEditor);
            taskHandler.RegisterTask(task);

            await task.ConfigureAwait(continueOnCapturedContext: false);
        }

        /// <summary>
        /// Processes the log file asynchronously.
        /// Handles tracing related information to the output window.
        /// </summary>
        /// <param name="filePath">The file path of the log to process.</param>
        /// <param name="toolFormat">The type of tool that produces the log file.</param>
        /// <param name="promptOnLogConversions">Whether there is a need to prompt the user before converting files to the sarif format. </param>
        /// <param name="cleanErrors">Whether we need to clean out caches before processing the new log file.</param>
        /// <param name="openInEditor">Whether we need ot open the file in-editor.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task ProcessLogFileWithTracesAsync(string filePath, string toolFormat, bool promptOnLogConversions, bool cleanErrors, bool openInEditor)
        {
            try
            {
                Trace.WriteLine(string.Format(Resources.TraceLog_ProcessingSarifFile, filePath));
                await ProcessLogFileCoreAsync(filePath, toolFormat, promptOnLogConversions, cleanErrors, openInEditor);
                Trace.WriteLine(string.Format(Resources.TraceLog_SarifFileProcessed, filePath));
            }
            catch (JsonException je)
            {
                Trace.WriteLine(string.Format(Resources.TraceLog_OpenSarifFileException, filePath, je.Message));
                RaiseLogProcessed(ExceptionalConditions.InvalidJson);
            }
            catch (Exception ex)
            {
                // for all other exceptions e.g. IO exception. throw it here will crash VS.
                Trace.WriteLine(string.Format(Resources.TraceLog_OpenSarifFileException, filePath, ex.Message));
            }
        }

        /// <summary>
        /// Converts an arbitrary log file to the stable sarif log file format.
        /// </summary>
        /// <param name="toolFormat">Format of the tool that created the log file.</param>
        /// <param name="filePath">The file path of the log to convert.</param>
        /// <param name="promptOnLogConversions">Whether to prompt log conversions.</param>
        /// <returns>An asynchronous task that returns a sarif log of the lrepresenting the log file as well as where it was saved.</returns>
        private static async Task<(SarifLog log, string outputPath)> ConvertLogsToSarifStableAsync(string toolFormat, string filePath, bool promptOnLogConversions)
        {
            SarifLog log = null;
            string logText = null;
            string outputPath = null;
            bool saveOutputFile = true;

            if (toolFormat.MatchesToolFormat(ToolFormat.None))
            {
                await RetryInvokeAsync(
                    async () =>
                    {
                        using (var logStreamReader = new StreamReader(filePath, Encoding.UTF8))
                        {
                            logText = await logStreamReader.ReadToEndAsync().ConfigureAwait(continueOnCapturedContext: false);
                        }
                    },
                    retryInterval: TimeSpan.FromMilliseconds(300),
                    maxAttemptCount: 5);

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
                            throw new TaskCanceledException();
                        }

                        var settingsV1 = new JsonSerializerSettings()
                        {
                            ContractResolver = SarifContractResolverVersionOne.Instance,
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
                                throw new TaskCanceledException();
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
                            throw new TaskCanceledException();
                        }

                        log = PrereleaseCompatibilityTransformer.UpdateToCurrentVersion(logText, Formatting.Indented, out logText);

                        if (response == MessageDialogCommand.Yes)
                        {
                            // Prompt for a location to save the transformed log.
                            outputPath = await PromptForFileSaveLocationAsync(Resources.SaveTransformedPrereleaseV2Log_DialogTitle, filePath).ConfigureAwait(continueOnCapturedContext: false);

                            if (string.IsNullOrEmpty(outputPath))
                            {
                                throw new TaskCanceledException();
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
                                                    OLEMSGICON.OLEMSGICON_WARNING,
                                                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                                                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    throw new TaskCanceledException();
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
                    throw new TaskCanceledException();
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

            return (log, outputPath);
        }

        /// <summary>
        /// Processes a log file asynchronously. Will convert non-sarif logs to the sarif format.
        /// </summary>
        /// <param name="filePath">The file path of the log to convert.</param>
        /// <param name="toolFormat">The format of the tool that created the log.</param>
        /// <param name="promptOnLogConversions">Whether to prompt the user about log conversions.</param>
        /// <param name="cleanErrors">Whether we need to delete the existing cache of data.</param>
        /// <param name="openInEditor">Whether we need to open the file in-editor.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task ProcessLogFileCoreAsync(string filePath, string toolFormat, bool promptOnLogConversions, bool cleanErrors, bool openInEditor)
        {
            try
            {
                (SarifLog log, string outputPath) logTuple = await ConvertLogsToSarifStableAsync(toolFormat, filePath, promptOnLogConversions);
                await ProcessSarifLogAsync(logTuple.log, logTuple.outputPath, cleanErrors: cleanErrors, openInEditor: openInEditor).ConfigureAwait(continueOnCapturedContext: false);
            }
            catch (Exception)
            {
                // swallow to prevent crashing.
            }
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

                SarifLogsMonitor.Instance.StopWatching(logFile);
            }

            foreach (int runIdToClear in runIdsToClear)
            {
                CodeAnalysisResultManager.Instance.RunIndexToRunDataCache.Remove(runIdToClear);
            }

            SarifLocationTagHelpers.RefreshTags();
        }

        public static async Task CloseSarifLogItemsAsync(IEnumerable<string> logFiles)
        {
            if (!SarifViewerPackage.IsUnitTesting)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            }

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
        }

        public static bool IsSarifLogOpened(string logFile)
        {
            return SarifTableDataSource.Instance.HasErrorsFromLog(logFile) ||
                CodeAnalysisResultManager.Instance.RunIndexToRunDataCache.
                    Any(runDataCacheKvp => runDataCacheKvp.Value.LogFilePath?.Equals(logFile, StringComparison.OrdinalIgnoreCase) == true);
        }

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
                InitialDirectory = Path.GetDirectoryName(inputFilePath),
            };

            inputFilePath = Path.GetFileNameWithoutExtension(inputFilePath) + ".v2.sarif";
            saveFileDialog.FileName = Path.GetFileName(inputFilePath);

            return saveFileDialog.ShowDialog() == DialogResult.OK ?
                saveFileDialog.FileName :
                null;
        }

        /// <summary>
        /// Saves a string to the file path provided, handling logging.
        /// </summary>
        /// <param name="filePath">The file on the device to save the text to.</param>
        /// <param name="logText">The text to save into the file.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private static async Task SaveLogFileAsync(string filePath, string logText)
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
            catch (JsonException)
            {
                RaiseLogProcessed(ExceptionalConditions.InvalidJson);
            }

            if (sarifLog != null)
            {
                await ProcessSarifLogAsync(sarifLog, logFilePath: logId, cleanErrors: cleanErrors, openInEditor: openInEditor);
            }
        }

        /// <summary>
        /// Processes the sarif log, puttng the informtion into the appropriate caches.
        /// </summary>
        /// <param name="sarifLog">The log to process and load into caches.</param>
        /// <param name="logFilePath">The file path of the log being loaded in.</param>
        /// <param name="cleanErrors">If true, will wipe previous errors from caches before processing new sarif log.</param>
        /// <param name="openInEditor">If true, will open the file in editor. </param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
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
            bool resultsFiltered = false;

            foreach (Run run in sarifLog.Runs)
            {
                // run.tool is required, add one if it's missing
                run.Tool ??= new Tool
                {
                    Driver = new ToolComponent
                    {
                        Name = Resources.UnknownToolName,
                    },
                };

                if (Instance.WriteRunToErrorList(run, logFilePath, sarifLog, out int runIndex) > 0)
                {
                    hasResults = true;

                    if (!resultsFiltered)
                    {
                        resultsFiltered = AreResultsFilteredBySeverity(
                            CodeAnalysisResultManager.Instance.RunIndexToRunDataCache[runIndex],
                            Instance.ColumnFilterer);
                    }
                }
            }

            if (openInEditor && !SarifViewerPackage.IsUnitTesting)
            {
                SdkUIUtilities.OpenDocument(ServiceProvider.GlobalProvider, logFilePath, usePreviewPane: false);
            }

            if (hasResults)
            {
                if (!SarifViewerPackage.IsUnitTesting)
                {
                    // We cannot show UI during unit-tests.
                    SdkUIUtilities.ShowToolWindowAsync(new Guid(ToolWindowGuids80.ErrorList), activate: false).FileAndForget(Constants.FileAndForgetFaultEventNames.ShowErrorList);
                }
            }

            SarifLogsMonitor.Instance.StartWatching(logFilePath);

            RaiseLogProcessed(ExceptionalConditionsCalculator.Calculate(sarifLog, resultsFiltered));
        }

        public static void CleanAllErrors()
        {
            SarifTableDataSource.Instance.CleanAllErrors();
            CodeAnalysisResultManager.Instance.RunIndexToRunDataCache.Clear();
            SarifLocationTagHelpers.RefreshTags();
        }

        public static void SendFeedback(FeedbackModel feedback)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                FeedbackTelemetryEvent.SendFeedbackTelemetryEvent(feedback);
            }
            catch (Exception)
            {
                VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider,
                       Resources.SendFeedbackFailed,
                       null, // title
                       OLEMSGICON.OLEMSGICON_CRITICAL,
                       OLEMSGBUTTON.OLEMSGBUTTON_OK,
                       OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        /// <summary>
        /// Adds information about the sarif errors to the <see cref="CodeAnalysisResultManager"/> cache as well as to the <see cref="SarifTableDataSource"/> instance.
        /// </summary>
        /// <param name="run">The run being logged.</param>
        /// <param name="logFilePath">The file path of the log.</param>
        /// <param name="sarifLog">The sarif log the run originated from.</param>
        /// <param name="runIndex">The index of the run in the <see cref="CodeAnalysisResultManager"/>.</param>
        /// <returns>The number of errors in the cache item added.</returns>
        private int WriteRunToErrorList(Run run, string logFilePath, SarifLog sarifLog, out int runIndex)
        {
            if (!SarifViewerPackage.IsUnitTesting)
            {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108
            }

            runIndex = CodeAnalysisResultManager.Instance.GetNextRunIndex();
            var dataCache = new RunDataCache(logFilePath, sarifLog);
            CodeAnalysisResultManager.Instance.RunIndexToRunDataCache.Add(runIndex, dataCache);
            CodeAnalysisResultManager.Instance.CacheUriBasePaths(run);

            var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;

            var projectNameCache = new ProjectNameCache(dte?.Solution);

            this.StoreFileDetails(run.Artifacts);
            if (run.Results != null)
            {
                foreach (CodeAnalysis.Sarif.Result result in run.Results)
                {
                    result.Run = run;
                    dataCache.AddSarifResult(new SarifErrorListItem(run, runIndex, result, logFilePath, projectNameCache));
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
                            dataCache.AddSarifResult(sarifError);
                        }
                    }

                    if (invocation.ToolExecutionNotifications != null)
                    {
                        foreach (Notification toolNotification in invocation.ToolExecutionNotifications)
                        {
                            if (toolNotification.Level != FailureLevel.Note)
                            {
                                var sarifError = new SarifErrorListItem(run, runIndex, toolNotification, logFilePath, projectNameCache);
                                dataCache.AddSarifResult(sarifError);
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

            IEnumerable<string> relativeFilePaths = dataCache.SarifErrors.Select(x => x.FileName);
            IEnumerable<string> uriBaseIds = dataCache.SarifErrors.Select(x => x.Locations?.FirstOrDefault()?.UriBaseId);

            // now we need to map from relative file path to absolute.
            string workingDirectory = dataCache.SarifErrors.FirstOrDefault().WorkingDirectory;

            // find the mapped path with codeanalysisresultmanager
            List<string> resolvedFilePaths = CodeAnalysisResultManager.Instance.TryResolveFilePaths(dataCache, workingDirectory, logFilePath, uriBaseIds.ToList(), relativeFilePaths.ToList());
            CodeAnalysisResultManager.Instance.RemapFilePaths(dataCache.SarifErrors, relativeFilePaths.ToList(), resolvedFilePaths);

            // remap regions nad lineNumber of the sarif error list items
            Dictionary<string, CodeFinder.CodeFinder> codeFinderCache = new Dictionary<string, CodeFinder.CodeFinder>(); // local file path -> codefinder
            foreach (SarifErrorListItem item in dataCache.SarifErrors)
            {
                List<(Uri filePath, MatchQuery query)?> queries = item.GetMatchQueries();
                if (queries != null)
                {
                    // try to do codefinding now, then modify the existing fields so we can treat as normal
                    for (int i = 0; i < queries.Count; i++)
                    {
                        (Uri filePath, MatchQuery query)? queryTuple = queries[i];
                        string resolvedPath = queryTuple?.filePath.AbsolutePath;

                        // string x = queryTuple?.filePath.AbsolutePath;
                        // string resolvedpath = queryTuple?.filePath.ToString();
                        MatchQuery query = queryTuple.Value.query;
                        if (!codeFinderCache.ContainsKey(resolvedPath))
                        {
                            string fileContent = SdkUIUtilities.TryGetFileContent(resolvedPath);
                            codeFinderCache[resolvedPath] = new CodeFinder.CodeFinder(resolvedPath, fileContent);
                        }

                        CodeFinder.CodeFinder finder = codeFinderCache[resolvedPath];
                        List<MatchResult> results = finder.FindMatchesWithFunction(query);
                        MatchResult bestResult = MatchResult.GetBestMatch(results, preferStringLiterals: false);
                        if (bestResult != null)
                        {
                            // if it's the first, we want to change the line number of the error list item too
                            if (i == 0)
                            {
                                item.LineNumber = bestResult.LineNumber;
                                item.Region.StartLine = bestResult.LineNumber;
                                item.Region.EndLine = bestResult.LineNumber;
                            }

                            item.Locations[i].Region.StartLine = bestResult.LineNumber;
                            item.Locations[i].Region.EndLine = bestResult.LineNumber;
                        }
                    }
                }
            }

            SarifTableDataSource.Instance.AddErrors(dataCache.SarifErrors);

            Trace.WriteLine($"{dataCache.SarifErrors.Count} results loaded from SARIF log file {logFilePath}");

            // This causes already open "text views" to be tagged when SARIF logs are processed after a view is opened.
            SarifLocationTagHelpers.RefreshTags();

            return dataCache.SarifErrors.Count;
        }

        /// <summary>
        /// Shows the Suppression State column. The first time it is shown, filter out "Suppressed"
        /// results. If the user ever adjusts the filter to show Suppressed results, don't ever
        /// filter them out again during the current VS session. The ColumnFilterer class
        /// implements that behavior.
        /// </summary>
        private void ShowFilteredSuppressionStateColumn()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Creating this table source adds "Suppression State" to the list of available columns.
            SuppressionStateTableDataSource dataSource = SuppressionStateTableDataSource.Instance;

            this.ColumnFilterer.FilterOut(
                columnName: SarifResultTableEntry.SuppressionStateColumnName,
                filteredValue: nameof(VSSuppressionState.Suppressed));
        }

        /// <summary>
        /// Show the Category column, which we currently overload to show Baseline State.
        /// The first time it is shown, filter out "Absent" results. If the user ever adjusts
        /// the filter to show Suppressed results, don't ever filter them out again during
        /// the current VS session. The ColumnFilterer class implements that behavior.
        /// </summary>
        private void ShowFilteredCategoryColumn()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Creating this table source adds "Category" to the list of available columns.
            // (Actually, it appears to be there by default, so this might not be necessary:)
            BaselineStateTableDataSource dataSource = BaselineStateTableDataSource.Instance;

            this.ColumnFilterer.FilterOut(
                columnName: StandardTableKeyNames.ErrorCategory,
                filteredValue: nameof(BaselineState.Absent));
        }

        private void EnsureHashExists(Artifact artifact)
        {
            if (artifact.Hashes == null)
            {
                artifact.Hashes = new Dictionary<string, string>();
            }

            if (!artifact.Hashes.ContainsKey(Sha256HashKey))
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
                    string hashString = HashHelper.GenerateHash(data);
                    artifact.Hashes.Add(Sha256HashKey, hashString);
                }
            }
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
                    // cache both artifact has file content and artifact has hash code
                    if (file.Contents != null || (file.Hashes != null && file.Hashes.ContainsKey(Sha256HashKey)))
                    {
                        this.EnsureHashExists(file);
                        var fileDetails = new ArtifactDetailsModel(file);
                        CodeAnalysisResultManager.Instance.CurrentRunDataCache.FileDetails.Add(uri.ToPath(), fileDetails);
                    }
                }
            }
        }

        internal static bool AreResultsFilteredBySeverity(RunDataCache runData, IColumnFilterer filterer)
        {
            IEnumerable<string> excludedValues = filterer.GetFilteredValues(StandardTableKeyNames.ErrorSeverity);

            if (excludedValues?.Any() != true)
            {
                return false;
            }

            return (runData.RunSummary.MessageResultsCount > 0 && excludedValues.Contains("message", StringComparer.OrdinalIgnoreCase)) ||
                (runData.RunSummary.WarningResultsCount > 0 && excludedValues.Contains("warning", StringComparer.OrdinalIgnoreCase)) ||
                (runData.RunSummary.ErrorResultsCount > 0 && excludedValues.Contains("error", StringComparer.OrdinalIgnoreCase));
        }

        private static void RaiseLogProcessed(ExceptionalConditions conditions)
        {
            LogProcessed?.Invoke(Instance, new LogProcessedEventArgs(conditions));
        }

        internal static void ErrorListService_LogProcessed(object sender, LogProcessedEventArgs e)
        {
            if (!SarifViewerPackage.IsUnitTesting)
            {
                ThreadHelper.JoinableTaskFactory.Run(async () => await ShowInfoBarAsync(e.ExceptionalConditions));
            }
        }

        private static async Task ShowInfoBarAsync(ExceptionalConditions conditions)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            InfoBar.CreateInfoBarsForExceptionalConditionsAsync(conditions).FileAndForget(FileAndForgetEventName.InfoBarOpenFailure);

            // After Sarif results loaded to Error List, make sure Viewer package is loaded
            SarifViewerPackage.LoadViewerPackage();
        }

        private static async Task RetryInvokeAsync(Func<Task> func, TimeSpan retryInterval, int maxAttemptCount = 3)
        {
            var exceptions = new List<Exception>();
            for (int attempted = 0; attempted < maxAttemptCount; attempted++)
            {
                try
                {
                    if (attempted > 0)
                    {
                        await Task.Delay(retryInterval);
                    }

                    await func();
                    return;
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            throw new AggregateException(exceptions);
        }
    }
}

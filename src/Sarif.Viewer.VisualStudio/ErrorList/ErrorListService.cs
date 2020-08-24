// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Converters;
using Microsoft.CodeAnalysis.Sarif.Readers;
using Microsoft.CodeAnalysis.Sarif.VersionOne;
using Microsoft.CodeAnalysis.Sarif.Visitors;
using Microsoft.CodeAnalysis.Sarif.Writers;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.Sarif.Viewer.Sarif;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;

namespace Microsoft.Sarif.Viewer.ErrorList
{
    public class ErrorListService
    {
        public static readonly ErrorListService Instance = new ErrorListService();

        public static void ProcessLogFile(string filePath, Solution solution, string toolFormat, bool promptOnLogConversions, bool cleanErrors)
        {
            SarifLog log = null;

            string logText;
            string outputPath = null;
            bool saveOutputFile = true;

            if (toolFormat.MatchesToolFormat(ToolFormat.None))
            {
                logText = File.ReadAllText(filePath);

                Match match = MatchVersionProperty(logText);
                if (match.Success)
                {
                    string inputVersion = match.Groups["version"].Value;

                    if (inputVersion == SarifUtilities.V1_0_0)
                    {
                        // They're opening a v1 log, so we need to transform it.
                        // Ask if they'd like to save the v2 log.
                        MessageDialogCommand response = promptOnLogConversions ? PromptToSaveProcessedLog(Resources.TransformV1_DialogMessage) : MessageDialogCommand.No;

                        if (response == MessageDialogCommand.Cancel)
                        {
                            return;
                        }

                        JsonSerializerSettings settingsV1 = new JsonSerializerSettings()
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
                            outputPath = PromptForFileSaveLocation(Resources.SaveTransformedV1Log_DialogTitle, filePath);

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
                        MessageDialogCommand response = promptOnLogConversions ? PromptToSaveProcessedLog(string.Format(Resources.TransformPrereleaseV2_DialogMessage, VersionConstants.StableSarifVersion)) : MessageDialogCommand.No;

                        if (response == MessageDialogCommand.Cancel)
                        {
                            return;
                        }

                        log = PrereleaseCompatibilityTransformer.UpdateToCurrentVersion(logText, Formatting.Indented, out logText);

                        if (response == MessageDialogCommand.Yes)
                        {
                            // Prompt for a location to save the transformed log.
                            outputPath = PromptForFileSaveLocation(Resources.SaveTransformedPrereleaseV2Log_DialogTitle, filePath);

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
                    // The version property wasn't found within the first 100 characters.
                    // Per the spec, it should appear first in the sarifLog object.
                    VsShellUtilities.ShowMessageBox(SarifViewerPackage.ServiceProvider,
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
                MessageDialogCommand response = promptOnLogConversions ? PromptToSaveProcessedLog(Resources.ConvertNonSarifLog_DialogMessage) : MessageDialogCommand.No;

                if (response == MessageDialogCommand.Cancel)
                {
                    return;
                }

                var converter = new ToolFormatConverter();
                var sb = new StringBuilder();

                using (var input = new MemoryStream(File.ReadAllBytes(filePath)))
                {
                    var outputTextWriter = new StringWriter(sb);                
                    var outputJson = new JsonTextWriter(outputTextWriter);
                    var output = new ResultLogJsonWriter(outputJson);

                    input.Seek(0, SeekOrigin.Begin);
                    converter.ConvertToStandardFormat(toolFormat, input, output);

                    // This is serving as a flush mechanism.
                    output.Dispose();

                    logText = sb.ToString();

                    if (response == MessageDialogCommand.Yes)
                    {
                        // Prompt for a location to save the converted log.
                        outputPath = PromptForFileSaveLocation(Resources.SaveConvertedLog_DialogTitle, filePath);
                    }
                }
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = Path.GetTempFileName() + ".sarif";
            }

            if (saveOutputFile)
            {
                SaveLogFile(outputPath, logText);
            }

            if (log == null)
            {
                log = JsonConvert.DeserializeObject<SarifLog>(logText);
            }

            ProcessSarifLog(log, outputPath, solution, showMessageOnNoResults: promptOnLogConversions, cleanErrors: cleanErrors);

            SarifViewerPackage.Dte.ExecuteCommand("View.ErrorList");

        }

        /// <summary>
        /// Closes the specified SARIF log in the viewer.
        /// </summary>
        /// <param name="logFiles">The complete path to the SARIF log file.</param>
        public static void CloseSarifLogs(IEnumerable<string> logFiles)
        {
            SarifTableDataSource.Instance.ClearErrorsForLogFiles(logFiles);
        }

        /// <summary>
        /// Closes all SARIF logs opened in the viewer.
        /// </summary>
        public static void CloseAllSarifLogs()
        {
            SarifTableDataSource.Instance.CleanAllErrors();
        }

        private const string VersionRegexPattern = @"""version""\s*:\s*""(?<version>[\d.]+)""";
        private const int HeadSegmentLength = 200;

        internal static Match MatchVersionProperty(string logText)
        {
            int headSegmentLength = Math.Min(logText.Length, HeadSegmentLength);
            string headSegment = logText.Substring(0, headSegmentLength);
            return Regex.Match(headSegment, VersionRegexPattern, RegexOptions.Compiled);
        }

        private static MessageDialogCommand PromptToSaveProcessedLog(string dialogMessage)
        {
            int result = VsShellUtilities.ShowMessageBox(SarifViewerPackage.ServiceProvider,
                                                         dialogMessage,
                                                         null, // title
                                                         OLEMSGICON.OLEMSGICON_QUERY,
                                                         OLEMSGBUTTON.OLEMSGBUTTON_YESNOCANCEL,
                                                         OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            return (MessageDialogCommand)Enum.Parse(typeof(MessageDialogCommand), result.ToString());
        }

        private static string PromptForFileSaveLocation(string dialogTitle, string inputFilePath)
        {
            var saveFileDialog = new SaveFileDialog();

            saveFileDialog.Title = dialogTitle;
            saveFileDialog.Filter = Resources.SaveDialogFileFilter;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.InitialDirectory = Path.GetDirectoryName(inputFilePath);

            inputFilePath = Path.GetFileNameWithoutExtension(inputFilePath) + ".v2.sarif";
            saveFileDialog.FileName = Path.GetFileName(inputFilePath);

            return saveFileDialog.ShowDialog() == DialogResult.OK ?
                saveFileDialog.FileName :
                null;
        }

        private static void SaveLogFile(string filePath, SarifLog log)
        {
            SaveLogFile(filePath, JsonConvert.SerializeObject(log));
        }

        private static void SaveLogFile(string filePath, string logText)
        {
            string error = null;

            try
            {
                File.WriteAllText(filePath, logText);
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
                VsShellUtilities.ShowMessageBox(SarifViewerPackage.ServiceProvider,
                                                error,
                                                null, // title
                                                OLEMSGICON.OLEMSGICON_CRITICAL,
                                                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                                                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        internal static void ProcessSarifLog(SarifLog sarifLog, string logFilePath, Solution solution, bool showMessageOnNoResults, bool cleanErrors)
        {
            // Clear previous data
            if (cleanErrors)
            {
                CodeAnalysisResultManager.Instance.ClearCurrentMarkers();
                SarifTableDataSource.Instance.CleanAllErrors();
                CodeAnalysisResultManager.Instance.RunDataCaches.Clear();
                CodeAnalysisResultManager.Instance.CurrentRunId = -1;
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

                TelemetryProvider.WriteEvent(TelemetryEvent.LogFileRunCreatedByToolName,
                                             TelemetryProvider.CreateKeyValuePair("ToolName", run.Tool.Driver.Name));
                if (Instance.WriteRunToErrorList(run, logFilePath, solution) > 0)
                {
                    hasResults = true;
                }
            }

            if (!hasResults && showMessageOnNoResults)
            {
                VsShellUtilities.ShowMessageBox(SarifViewerPackage.ServiceProvider,
                                                string.Format(Resources.NoResults_DialogMessage, logFilePath),
                                                null, // title
                                                OLEMSGICON.OLEMSGICON_INFO,
                                                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                                                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        private ErrorListService()
        {
        }

        private int WriteRunToErrorList(Run run, string logFilePath, Solution solution)
        {
            RunDataCache dataCache = new RunDataCache(run);
            CodeAnalysisResultManager.Instance.RunDataCaches.Add(++CodeAnalysisResultManager.Instance.CurrentRunId, dataCache);
            CodeAnalysisResultManager.Instance.CacheUriBasePaths(run);
            List<SarifErrorListItem> sarifErrors = new List<SarifErrorListItem>();

            var projectNameCache = new ProjectNameCache(solution);

            StoreFileDetails(run.Artifacts);

            if (run.Results != null)
            {
                foreach (Result result in run.Results)
                {
                    result.Run = run;
                    var sarifError = new SarifErrorListItem(run, result, logFilePath, projectNameCache);
                    sarifErrors.Add(sarifError);
                }
            }

            if (run.Invocations != null)
            {
                foreach (var invocation in run.Invocations)
                {
                    if (invocation.ToolConfigurationNotifications != null)
                    {
                        foreach (Notification configurationNotification in invocation.ToolConfigurationNotifications)
                        {
                            var sarifError = new SarifErrorListItem(run, configurationNotification, logFilePath, projectNameCache);
                            sarifErrors.Add(sarifError);
                        }
                    }

                    if (invocation.ToolExecutionNotifications != null)
                    {
                        foreach (Notification toolNotification in invocation.ToolExecutionNotifications)
                        {
                            if (toolNotification.Level != FailureLevel.Note)
                            {
                                var sarifError = new SarifErrorListItem(run, toolNotification, logFilePath, projectNameCache);
                                sarifErrors.Add(sarifError);
                            }
                        }
                    }
                }
            }

            (dataCache.SarifErrors as List<SarifErrorListItem>).AddRange(sarifErrors);
            SarifTableDataSource.Instance.AddErrors(sarifErrors);
            return sarifErrors.Count;
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
                    string hashString = GenerateHash(data);
                    artifact.Hashes.Add("sha-256", hashString);
                }
            }
        }

        internal string GenerateHash(byte[] data)
        {
            SHA256Managed hashFunction = new SHA256Managed();
            byte[] hash = hashFunction.ComputeHash(data);
            return hash.Aggregate(string.Empty, (current, x) => current + $"{x:x2}");
        }
      
        private void StoreFileDetails(IList<Artifact> artifacts)
        {
            if (artifacts == null)
            {
                return;
            }

            foreach (var file in artifacts)
            {
                Uri uri = file.Location?.Uri;
                if (uri != null)
                {
                    if (file.Contents != null)
                    {
                        EnsureHashExists(file);
                        var fileDetails = new ArtifactDetailsModel(file);
                        CodeAnalysisResultManager.Instance.CurrentRunDataCache.FileDetails.Add(uri.ToPath(), fileDetails);
                    }
                }
            }
        }
    }
}
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

        private static JsonSerializerSettings SettingsV2 = new JsonSerializerSettings()
        {
            ContractResolver = SarifContractResolver.Instance
        };

        public static void ProcessLogFile(string filePath, Solution solution, string toolFormat = ToolFormat.None)
        {
            SarifLog log = null;

            string logText;

            if (toolFormat.MatchesToolFormat(ToolFormat.None))
            {
                logText = File.ReadAllText(filePath);
                string pattern = @"""version""\s*:\s*""1.0.0""";
                Match match = Regex.Match(logText, pattern, RegexOptions.Compiled | RegexOptions.Multiline);

                if (match.Success)
                {
                    // They're opening a v1 log, so we need to transform it.
                    // Ask if they'd like to save the v2 log.
                    MessageDialogCommand response = PromptToSaveProcessedLog(Resources.TransformV1_DialogMessage);

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
                        filePath = PromptForFileSaveLocation(Resources.SaveTransformedV1Log_DialogTitle, filePath);

                        if (string.IsNullOrEmpty(filePath))
                        {
                            return;
                        }
                    }
                    else
                    {
                        // Save to a temp file.
                        filePath = Path.GetTempFileName() + ".sarif";
                    }

                    SaveLogFile(filePath, log);
                }
            }
            else
            {
                // They're opening a non-SARIF log, so we need to convert it.
                // Ask if they'd like to save the converted log.
                MessageDialogCommand response = PromptToSaveProcessedLog(Resources.ConvertNonSarifLog_DialogMessage);

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
                    log = JsonConvert.DeserializeObject<SarifLog>(logText, SettingsV2);

                    if (response == MessageDialogCommand.Yes)
                    {
                        // Prompt for a location to save the converted log.
                        string saveFilePath = PromptForFileSaveLocation(Resources.SaveConvertedLog_DialogTitle, filePath);
                        
                        if (!string.IsNullOrEmpty(saveFilePath))
                        {
                            // The user chose a location.
                            filePath = saveFilePath;
                        }
                        else
                        {
                            // Save to a temp file.
                            filePath = Path.GetTempFileName() + ".sarif";
                        }
                    }
                    else
                    {
                        // Save to a temp file.
                        filePath = Path.GetTempFileName() + ".sarif";
                    }

                    SaveLogFile(filePath, logText);
                }
            }

            ProcessSarifLog(log, filePath, solution);

            SarifTableDataSource.Instance.BringToFront();
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

            saveFileDialog.Title = Resources.SaveTransformedV1Log_DialogTitle;
            saveFileDialog.Filter = "SARIF log files (*.sarif)|*.sarif";
            saveFileDialog.RestoreDirectory = true;

            inputFilePath = Path.GetFileNameWithoutExtension(inputFilePath) + ".v2.sarif";
            saveFileDialog.FileName = Path.GetFileName(inputFilePath);
            saveFileDialog.InitialDirectory = Path.GetDirectoryName(inputFilePath);

            return saveFileDialog.ShowDialog() == DialogResult.OK ?
                saveFileDialog.FileName :
                null;
        }

        private static void SaveLogFile(string filePath, SarifLog log)
        {
            SaveLogFile(filePath, JsonConvert.SerializeObject(log, SettingsV2));
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

        internal static void ProcessSarifLog(SarifLog sarifLog, string logFilePath, Solution solution)
        {
            // Clear previous data
            SarifTableDataSource.Instance.CleanAllErrors();
            CodeAnalysisResultManager.Instance.SarifErrors.Clear();
            CodeAnalysisResultManager.Instance.FileDetails.Clear();

            bool hasResults = false;

            foreach (Run run in sarifLog.Runs)
            {
                // run.tool is required, add one if it's missing
                if (run.Tool == null)
                {
                    run.Tool = new Tool
                    {
                        Name = Resources.UnknownToolName
                    };
                }

                TelemetryProvider.WriteEvent(TelemetryEvent.LogFileRunCreatedByToolName,
                                             TelemetryProvider.CreateKeyValuePair("ToolName", run.Tool.Name));
                if (Instance.WriteRunToErrorList(run, logFilePath, solution) > 0)
                {
                    hasResults = true;
                }
            }

            if (!hasResults)
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
            List<SarifErrorListItem> sarifErrors = new List<SarifErrorListItem>();
            var projectNameCache = new ProjectNameCache(solution);

            StoreFileDetails(run.Files);

            if (run.Results != null)
            {
                foreach (Result result in run.Results)
                {
                    var sarifError = new SarifErrorListItem(run, result, logFilePath, projectNameCache);
                    sarifErrors.Add(sarifError);
                }
            }

            if (run.Invocations != null)
            {
                foreach (var invocation in run.Invocations)
                {
                    if (invocation.ConfigurationNotifications != null)
                    {
                        foreach (Notification configurationNotification in invocation.ConfigurationNotifications)
                        {
                            var sarifError = new SarifErrorListItem(run, configurationNotification, logFilePath, projectNameCache);
                            sarifErrors.Add(sarifError);
                        }
                    }

                    if (invocation.ToolNotifications != null)
                    {
                        foreach (Notification toolNotification in invocation.ToolNotifications)
                        {
                            if (toolNotification.Level != NotificationLevel.Note)
                            {
                                var sarifError = new SarifErrorListItem(run, toolNotification, logFilePath, projectNameCache);
                                sarifErrors.Add(sarifError);
                            }
                        }
                    }
                }
            }

            foreach (var error in sarifErrors)
            {
                CodeAnalysisResultManager.Instance.SarifErrors.Add(error);
            }

            SarifTableDataSource.Instance.AddErrors(sarifErrors);
            return sarifErrors.Count;
        }

        private void EnsureHashExists(FileData file)
        {
            if (file.Hashes == null)
            {
                file.Hashes = new List<Hash>();
            }
            
            var hasSha256Hash = file.Hashes.Any(x => x.Algorithm == "sha-256");
            if (!hasSha256Hash)
            {
                byte[] data = null;
                if (file.Contents?.Binary != null)
                {
                    data = Convert.FromBase64String(file.Contents.Binary);
                }
                else if (file.Contents?.Text != null)
                {
                    data = Encoding.UTF8.GetBytes(file.Contents.Text);
                }

                if (data != null)
                {
                    string hashString = GenerateHash(data);
                    file.Hashes.Add(new Hash(hashString, "sha-256"));
                }
            }
        }

        internal string GenerateHash(byte[] data)
        {
            SHA256Managed hashFunction = new SHA256Managed();
            byte[] hash = hashFunction.ComputeHash(data);
            return hash.Aggregate(string.Empty, (current, x) => current + $"{x:x2}");
        }
      
        private void StoreFileDetails(IDictionary<string, FileData> files)
        {
            if (files == null)
            {
                return;
            }

            foreach (var file in files)
            {
                Uri key;
                var isValid = Uri.TryCreate(file.Key, UriKind.RelativeOrAbsolute, out key);

                if (!isValid)
                {
                    continue;
                }

                var contents = file.Value.Contents;
                if (contents != null)
                {
                    EnsureHashExists(file.Value);
                    var fileDetails = new FileDetailsModel(file.Value);
                    CodeAnalysisResultManager.Instance.FileDetails.Add(key.ToPath(), fileDetails);
                }
            }
        }
    }
}
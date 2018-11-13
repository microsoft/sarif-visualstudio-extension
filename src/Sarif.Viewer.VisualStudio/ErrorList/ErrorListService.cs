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

        public static void ProcessLogFile(string filePath, Solution solution, string toolFormat = ToolFormat.None)
        {
            SarifLog log = null;

            JsonSerializerSettings settingsV2 = new JsonSerializerSettings()
            {
                ContractResolver = SarifContractResolver.Instance,
            };

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
                    int result = VsShellUtilities.ShowMessageBox(SarifViewerPackage.ServiceProvider,
                                                                 Resources.TransformV1_DialogMessage,
                                                                 null, // title
                                                                 OLEMSGICON.OLEMSGICON_QUERY,
                                                                 OLEMSGBUTTON.OLEMSGBUTTON_YESNOCANCEL,
                                                                 OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    MessageDialogCommand response = (MessageDialogCommand)Enum.Parse(typeof(MessageDialogCommand), result.ToString());

                    if (response == MessageDialogCommand.Cancel)
                    {
                        return;
                    }

                    JsonSerializerSettings settingsV1 = new JsonSerializerSettings()
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
                        var saveFileDialog = new SaveFileDialog();

                        saveFileDialog.Title = Resources.SaveTransformedV1Log_DialogTitle;
                        saveFileDialog.Filter = "SARIF log files (*.sarif)|*.sarif";
                        saveFileDialog.RestoreDirectory = true;

                        filePath = Path.GetFileNameWithoutExtension(filePath) + ".v2.sarif";
                        saveFileDialog.FileName = Path.GetFileName(filePath);
                        saveFileDialog.InitialDirectory = Path.GetDirectoryName(filePath);

                        if (saveFileDialog.ShowDialog() != DialogResult.OK)
                        {
                            return;
                        }

                        filePath = saveFileDialog.FileName;
                        string error = null;

                        try
                        {
                            File.WriteAllText(filePath, JsonConvert.SerializeObject(log, settingsV2));
                        }
                        catch (UnauthorizedAccessException)
                        {
                            error = string.Format(Resources.SaveTransformedV1LogFail_Access_DialogMessage, filePath);
                        }
                        catch (SecurityException)
                        {
                            error = string.Format(Resources.SaveTransformedV1LogFail_Access_DialogMessage, filePath);
                        }
                        catch (Exception ex)
                        {
                            error = string.Format(Resources.SaveTransformedV1LogFail_General_Dialog, ex.Message);
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
                }
            }
            else
            {
                // We have conversion to do
                var converter = new ToolFormatConverter();
                var sb = new StringBuilder();

                using (var input = new MemoryStream(File.ReadAllBytes(filePath)))
                {
                    var outputTextWriter = new StringWriter(sb);                
                    var outputJson = new JsonTextWriter(outputTextWriter);
                    var output = new ResultLogJsonWriter(outputJson);

                    input.Seek(0, SeekOrigin.Begin);
                    converter.ConvertToStandardFormat(toolFormat, input, output);

                    // This is serving as a flush mechanism
                    output.Dispose();

                    logText = sb.ToString();
                }
            }

            if (log == null)
            {
                log = JsonConvert.DeserializeObject<SarifLog>(logText, settingsV2);
            }

            ProcessSarifLog(log, filePath, solution);

            SarifTableDataSource.Instance.BringToFront();
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
                // Make sure each run has a uid which we'll use to silo cached data
                if (string.IsNullOrWhiteSpace(run.InstanceGuid))
                {
                    run.InstanceGuid = Guid.NewGuid().ToString();
                }

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

            CodeAnalysisResultManager.Instance.FileDetails.Add(run.InstanceGuid, new Dictionary<string, FileDetailsModel>());
            StoreFileDetails(run.InstanceGuid, run.Files);

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
      
        private void StoreFileDetails(string runId, IDictionary<string, FileData> files)
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
                    CodeAnalysisResultManager.Instance.FileDetails[runId].Add(key.ToPath(), fileDetails);
                }
            }
        }
    }
}
﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Sarif.Converters;
using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TaskStatusCenter;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.Sarif.Viewer.Services
{
    /// <summary>
    /// Provides an interface through which other extensions can interact with the this extension,
    /// in particular, to ask this extension to load a log file.
    /// </summary>
    public class LoadSarifLogService : SLoadSarifLogService, ILoadSarifLogService
    {
        /// <inheritdoc/>
        public void LoadSarifLog(string path, bool promptOnSchemaUpgrade = true)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            ErrorListService.ProcessLogFile(path, ToolFormat.None, promptOnSchemaUpgrade, cleanErrors: true, openInEditor: false);
        }

        /// <inheritdoc/>
        public void LoadSarifLog(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            ErrorListService.ProcessLogFile(path, ToolFormat.None, promptOnLogConversions: true, cleanErrors: true, openInEditor: false);
        }

        /// <inheritdoc/>
        public void LoadSarifLogs(IEnumerable<string> paths)
        {
            this.LoadSarifLogs(paths, promptOnSchemaUpgrade: false);
        }

        /// <inheritdoc/>
        public void LoadSarifLogs(IEnumerable<string> paths, bool promptOnSchemaUpgrade)
        {
            LoadSarifLogAsync(paths).FileAndForget(Constants.FileAndForgetFaultEventNames.LoadSarifLogs);
        }

        /// <inheritdoc/>
        public void LoadSarifLog(Stream stream)
        {
            LoadSarifLogAsync(stream).FileAndForget(Constants.FileAndForgetFaultEventNames.LoadSarifLogs);
        }

        private async Task LoadSarifLogAsync(IEnumerable<string> paths)
        {
            List<string> validPaths = paths.Where(path => !string.IsNullOrEmpty(path)).ToList();
            if (validPaths.Count == 0)
            {
                return;
            }

            var taskStatusCenterService = (IVsTaskStatusCenterService)Package.GetGlobalService(typeof(SVsTaskStatusCenterService));
            var taskProgressData = new TaskProgressData
            {
                CanBeCanceled = true,
                ProgressText = null,
            };

            var taskHandlerOptions = new TaskHandlerOptions
            {
                ActionsAfterCompletion = CompletionActions.None,
                TaskSuccessMessage = Resources.ProcessLogFilesComplete,
                Title = Resources.ProcessLogFiles,
            };

            var taskCompletionSource = new TaskCompletionSource<bool>();
            ITaskHandler taskHandler = taskStatusCenterService.PreRegister(taskHandlerOptions, taskProgressData);
            taskHandler.RegisterTask(taskCompletionSource.Task);

            try
            {
                for (int validPathIndex = 0; validPathIndex < validPaths.Count; validPathIndex++)
                {
                    taskHandler.UserCancellation.ThrowIfCancellationRequested();

                    taskHandler.Progress.Report(new TaskProgressData
                    {
                        PercentComplete = validPathIndex * 100 / validPaths.Count,
                        ProgressText = string.Format(CultureInfo.CurrentCulture, Resources.ProcessingLogFileFormat, validPaths[validPathIndex])
                    }); ;

                    // We should not clean errors here. If the user wants to clear errors, they can call ICloseSarifLogService.CloseAllSarifLogs.
                    await ErrorListService.ProcessLogFileAsync(validPaths[validPathIndex], ToolFormat.None, promptOnLogConversions: false, cleanErrors: false, openInEditor: false).ConfigureAwait(continueOnCapturedContext: false);

                    taskHandler.Progress.Report(new TaskProgressData
                    {
                        PercentComplete = (validPathIndex + 1) * 100 / validPaths.Count,
                    });
                }
            }
            finally
            {
                taskCompletionSource.SetResult(true);
            }
        }

        private async Task LoadSarifLogAsync(Stream stream)
        {
            await ErrorListService.ProcessSarifLogAsync(stream, showMessageOnNoResults: false, cleanErrors: false, openInEditor: false).ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
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
        public void LoadSarifLog(string path, bool promptOnLogConversions = true, bool cleanErrors = true, bool openInEditor = false)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            ErrorListService.ProcessLogFile(path, ToolFormat.None, promptOnLogConversions, cleanErrors, openInEditor);
        }

        /// <inheritdoc/>
        public void LoadSarifLogs(IEnumerable<string> paths, bool promptOnSchemaUpgrade = false)
        {
            this.LoadSarifLogAsync(paths).FileAndForget(Constants.FileAndForgetFaultEventNames.LoadSarifLogs);
        }

        /// <inheritdoc/>
        public void LoadSarifLog(Stream stream, string logId = null)
        {
            this.LoadSarifLogAsync(stream, logId).FileAndForget(Constants.FileAndForgetFaultEventNames.LoadSarifLogs);
        }

        /// <inheritdoc/>
        public void LoadSarifLog(IEnumerable<Stream> streams)
        {
            this.LoadSarifLogAsync(streams).FileAndForget(Constants.FileAndForgetFaultEventNames.LoadSarifLogs);
        }

        private async Task LoadSarifLogAsync(IEnumerable<string> paths)
        {
            var validPaths = paths.Where(path => !string.IsNullOrEmpty(path)).ToList();
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
                        ProgressText = string.Format(CultureInfo.CurrentCulture, Resources.ProcessingLogFileFormat, validPaths[validPathIndex]),
                    });

                    // We should not clean errors here. If the user wants to clear errors, they can call ICloseSarifLogService.CloseAllSarifLogs.
                    await ErrorListService.ProcessLogFileWithTracesAsync(validPaths[validPathIndex], ToolFormat.None, promptOnLogConversions: false, cleanErrors: false, openInEditor: false).ConfigureAwait(continueOnCapturedContext: false);

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

        private async Task LoadSarifLogAsync(Stream stream, string logId)
        {
            await ErrorListService.ProcessSarifLogAsync(stream, logId: logId, cleanErrors: false, openInEditor: false).ConfigureAwait(continueOnCapturedContext: false);
        }

        public async Task LoadSarifLogAsync(IEnumerable<Stream> streams)
        {
            foreach (Stream stream in streams)
            {
                await ErrorListService.ProcessSarifLogAsync(stream, logId: null, cleanErrors: false, openInEditor: false).ConfigureAwait(continueOnCapturedContext: false);
            }
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Converters;
using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.Sarif.Viewer.FileMonitor
{
    /// <summary>
    /// Watches a sarif log file in the file system, firing events when the fileis changed or renamed.
    /// </summary>
    internal class SarifLogsMonitor : IDisposable
    {
        private readonly IFileSystem fileSystem;

        private IDictionary<string, Shell.IFileWatcher> FileWatcherMap { get; } =
            new ConcurrentDictionary<string, Shell.IFileWatcher>(StringComparer.OrdinalIgnoreCase);

        internal SarifLogsMonitor(IFileSystem fs)
        {
            this.fileSystem = fs;
        }

        internal static SarifLogsMonitor Instance = new SarifLogsMonitor(new FileSystem());

        internal void StartWatching(string logFilePath)
        {
            if (!fileSystem.FileExists(logFilePath))
            {
                return;
            }

            if (!FileWatcherMap.ContainsKey(logFilePath))
            {
                var watcher = new Shell.FileWatcher(Path.GetDirectoryName(logFilePath), Path.GetFileName(logFilePath));
                watcher.FileChanged += this.Watcher_SarifLogFileChanged;
                watcher.FileRenamed += this.Watcher_SarifLogFileRenamed;
                FileWatcherMap.Add(logFilePath, watcher);
                watcher.Start();
            }
        }

        internal void Clear()
        {
            foreach (Shell.IFileWatcher watcher in FileWatcherMap.Values)
            {
                (watcher as IDisposable)?.Dispose();
            }

            FileWatcherMap.Clear();
        }

        private void Watcher_SarifLogFileRenamed(object sender, System.IO.RenamedEventArgs e)
        {
            /*
             * When updating a file in VS, it saves file content to a new temp file and rename current file to another temp file,
             * then rename 1st temp file with latest content to current file name and delete 2nd temp file.
             * Here we need to catch the event the 1st temp file is renamed to current file name
             * and ignore event current file is renamed to 2nd temp file.
             */
            if (FileWatcherMap.ContainsKey(e.FullPath))
            {
                this.RefreshSarifErrors(e.FullPath);
            }
        }

        private void Watcher_SarifLogFileChanged(object sender, System.IO.FileSystemEventArgs e)
        {
            this.RefreshSarifErrors(e.FullPath);
        }

        private void RefreshSarifErrors(string filePath)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ErrorListService.CloseSarifLogItemsAsync(new string[] { filePath });
                await ErrorListService.ProcessLogFileWithTracesAsync(filePath, ToolFormat.None, promptOnLogConversions: true, cleanErrors: false, openInEditor: false);
            });
        }

        internal void StopWatching(string logFilePath)
        {
            if (FileWatcherMap.TryGetValue(logFilePath, out Shell.IFileWatcher watcher))
            {
                watcher.Stop();
                FileWatcherMap.Remove(logFilePath);
            }
        }

        public void Dispose()
        {
            this.Clear();
        }
    }
}

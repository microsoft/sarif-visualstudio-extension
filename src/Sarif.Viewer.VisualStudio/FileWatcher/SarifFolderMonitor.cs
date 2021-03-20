// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using EnvDTE;

using EnvDTE80;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Options;
using Microsoft.Sarif.Viewer.Services;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.Sarif.Viewer.FileWatcher
{
    /// <summary>
    /// Handles loading & monitoring sarif logs under solution directory .sarif folder.
    /// </summary>
    internal class SarifFolderMonitor : IDisposable
    {
        public const string SarifFolderName = ".sarif";
        public const string SarifFileExtensionName = ".sarif";

        private readonly IFileSystem fileSystem;
        private IFileWatcher fileWatcher;
        private readonly ILoadSarifLogService sarifLoadLogService;
        private readonly ICloseSarifLogService sarifCloseLogService;

        private string solutionFolder = null;

        internal SarifFolderMonitor(
            IFileSystem fs = null,
            IFileWatcher watcher = null,
            ILoadSarifLogService sarifLoadLogService = null,
            ICloseSarifLogService closeSarifLogService = null)
        {
            this.fileSystem = fs ?? new FileSystem();
            this.fileWatcher = watcher ?? new FileWatcher();
            this.sarifLoadLogService = sarifLoadLogService ?? new LoadSarifLogService();
            this.sarifCloseLogService = closeSarifLogService ?? new CloseSarifLogService();
        }

        public void Dispose()
        {
            this.fileWatcher?.Dispose();
        }

        internal void StartWatch(string solutionPath = null)
        {
            this.solutionFolder = solutionPath ?? this.GetSolutionDirectory();
            if (string.IsNullOrEmpty(this.solutionFolder) || !fileSystem.DirectoryExists(this.solutionFolder))
            {
                return;
            }

            string sarifLogFolder = Path.Combine(this.solutionFolder, SarifFolderName);
            if (!fileSystem.DirectoryExists(sarifLogFolder))
            {
                return;
            }

            // load existing sarif logs
            this.LoadExistingSarifLogs(sarifLogFolder);

            this.fileWatcher ??= new FileWatcher();
            this.fileWatcher.FilePath = sarifLogFolder;
            this.fileWatcher.Filter = Constants.SarifFileSearchPattern;

            // here no need to watch for sarif file log updates
            // because when load the sarif log file in viewer, its already monitored by viewer's file watcher
            this.fileWatcher.SarifLogFileCreated += this.Watcher_SarifLogFileCreated;
            this.fileWatcher.SarifLogFileDeleted += this.Watcher_SarifLogFileDeleted;
            this.fileWatcher.Start();
        }

        internal void StopWatch()
        {
            if (this.fileWatcher != null)
            {
                this.fileWatcher.Stop();
                this.fileWatcher.SarifLogFileCreated -= this.Watcher_SarifLogFileCreated;
                this.fileWatcher.SarifLogFileDeleted -= this.Watcher_SarifLogFileDeleted;
                this.fileWatcher = null;
            }

            if (!string.IsNullOrEmpty(this.solutionFolder))
            {
                this.CloseExistingSarifLogs(Path.Combine(this.solutionFolder, SarifFolderName));
                this.solutionFolder = null;
            }
        }

        internal void LoadExistingSarifLogs(string targetFolderPath)
        {
            if (!string.IsNullOrEmpty(targetFolderPath) && this.fileSystem.DirectoryExists(targetFolderPath))
            {
                IEnumerable<string> sarifLogFiles = this.fileSystem.DirectoryGetFiles(targetFolderPath, Constants.SarifFileSearchPattern);
                this.sarifLoadLogService.LoadSarifLogs(sarifLogFiles);
            }
        }

        internal void CloseExistingSarifLogs(string targetFolderPath)
        {
            if (!string.IsNullOrEmpty(targetFolderPath) && this.fileSystem.DirectoryExists(targetFolderPath))
            {
                IEnumerable<string> sarifLogFiles = this.fileSystem.DirectoryGetFiles(targetFolderPath, Constants.SarifFileSearchPattern);
                this.sarifCloseLogService.CloseSarifLogs(sarifLogFiles);
            }
        }

        private void Watcher_SarifLogFileCreated(object sender, FileSystemEventArgs e)
        {
            if (!SarifViewerOption.Instance.ShouldMonitorSarifFolder)
            {
                return;
            }

            if (SarifViewerPackage.IsUnitTesting)
            {
                this.sarifLoadLogService.LoadSarifLog(e.FullPath, promptOnLogConversions: false, cleanErrors: false, openInEditor: false);
            }
            else
            {
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    this.sarifLoadLogService.LoadSarifLog(e.FullPath, promptOnLogConversions: false, cleanErrors: false, openInEditor: false);
                });
            }
        }

        private void Watcher_SarifLogFileDeleted(object sender, FileSystemEventArgs e)
        {
            if (!SarifViewerOption.Instance.ShouldMonitorSarifFolder)
            {
                return;
            }

            if (SarifViewerPackage.IsUnitTesting)
            {
                this.sarifCloseLogService.CloseSarifLogs(new string[] { e.FullPath });
            }
            else
            {
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    this.sarifCloseLogService.CloseSarifLogs(new string[] { e.FullPath });
                });
            }
        }

        /// <summary>
        /// Returns the solution directory, or null if no solution is open.
        /// </summary>
        private string GetSolutionDirectory()
        {
            var dte = (DTE2)Package.GetGlobalService(typeof(DTE));
            string solutionFilePath = dte.Solution?.FullName;
            return string.IsNullOrEmpty(solutionFilePath) ? null : Path.GetDirectoryName(solutionFilePath);
        }
    }
}

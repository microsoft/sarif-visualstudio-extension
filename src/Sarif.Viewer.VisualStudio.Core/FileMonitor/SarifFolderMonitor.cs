// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using EnvDTE;

using EnvDTE80;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Services;
using Microsoft.Sarif.Viewer.Shell;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.Sarif.Viewer.FileMonitor
{
    /// <summary>
    /// Handles loading & monitoring sarif logs under solution directory .sarif folder.
    /// </summary>
    internal class SarifFolderMonitor : IDisposable
    {
        public const string SarifFolderName = ".sarif";
        public const string SarifFileExtensionName = ".sarif";

        private readonly IFileSystem fileSystem;
        private readonly ILoadSarifLogService sarifLoadLogService;
        private readonly ICloseSarifLogService sarifCloseLogService;

        private IFileWatcher fileWatcher;
        private bool filesLoaded;
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

        /// <summary>
        /// Starts watching the .sarif folder under a particular path for files being created or destroyed. If the path does not exist it will create a new directory of that path.
        /// </summary>
        /// <param name="solutionPath">(Optional). The solution path to watch. Of no path is provided it will use the default solution directory if exists.</param>
        internal void StartWatching(string solutionPath = null)
        {
            this.solutionFolder = solutionPath ?? this.GetSolutionDirectory();
            if (string.IsNullOrEmpty(this.solutionFolder) || !fileSystem.DirectoryExists(this.solutionFolder))
            {
                return;
            }

            string sarifLogFolder = Path.Combine(this.solutionFolder, SarifFolderName);
            if (!fileSystem.DirectoryExists(sarifLogFolder))
            {
                try
                {
                    fileSystem.DirectoryCreateDirectory(sarifLogFolder);
                }
                catch
                {
                    // failed to create .sarif folder, exit method
                    return;
                }
            }

            // load existing sarif logs
            this.LoadExistingSarifLogs(sarifLogFolder);

            try
            {
                this.fileWatcher ??= new Shell.FileWatcher();
                this.fileWatcher.FilePath = sarifLogFolder;
                this.fileWatcher.Filter = Constants.SarifFileSearchPattern;

                // no need to watch for the sarif file log updates
                // because when we load the sarif log file in the viewer, it's already monitored by the viewer's file watcher
                this.fileWatcher.FileCreated += this.Watcher_SarifLogFileCreated;
                this.fileWatcher.FileDeleted += this.Watcher_SarifLogFileDeleted;
                this.fileWatcher.Start();
            }
            catch
            {
            }
        }

        internal void StopWatching()
        {
            if (this.fileWatcher != null)
            {
                this.fileWatcher.Stop();
                this.fileWatcher.FileCreated -= this.Watcher_SarifLogFileCreated;
                this.fileWatcher.FileDeleted -= this.Watcher_SarifLogFileDeleted;
                this.fileWatcher = null;
            }

            if (!string.IsNullOrEmpty(this.solutionFolder))
            {
                this.CloseExistingSarifLogs(Path.Combine(this.solutionFolder, SarifFolderName));
                this.solutionFolder = null;
            }
        }

        /// <summary>
        /// Loads the files into the <see cref="SarifFolderMonitor.sarifLoadLogService"/> if they have not been loaded already.
        /// </summary>
        /// <param name="targetFolderPath">The folder to load the files from.</param>
        internal void LoadExistingSarifLogs(string targetFolderPath)
        {
            if (!string.IsNullOrEmpty(targetFolderPath) && this.fileSystem.DirectoryExists(targetFolderPath) && !this.filesLoaded)
            {
                IEnumerable<string> sarifLogFiles = this.fileSystem.DirectoryGetFiles(targetFolderPath, Constants.SarifFileSearchPattern);
                this.sarifLoadLogService.LoadSarifLogs(sarifLogFiles);
                this.filesLoaded = true;
            }
        }

        internal void CloseExistingSarifLogs(string targetFolderPath)
        {
            if (!string.IsNullOrEmpty(targetFolderPath) && this.fileSystem.DirectoryExists(targetFolderPath))
            {
                IEnumerable<string> sarifLogFiles = this.fileSystem.DirectoryGetFiles(targetFolderPath, Constants.SarifFileSearchPattern);
                this.sarifCloseLogService.CloseSarifLogs(sarifLogFiles);
                this.filesLoaded = false;
            }
        }

        private void Watcher_SarifLogFileCreated(object sender, FileSystemEventArgs e)
        {
            if (!SarifViewerGeneralOptions.Instance.ShouldMonitorSarifFolder)
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
            if (!SarifViewerGeneralOptions.Instance.ShouldMonitorSarifFolder)
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

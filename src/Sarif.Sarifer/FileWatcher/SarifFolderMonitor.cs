// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using EnvDTE;

using EnvDTE80;

using Microsoft.Sarif.Viewer.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer.FileWatcher
{
    /// <summary>
    /// Handles loading & monitoring sarif logs under solution directory .sarif folder
    /// </summary>
    internal class SarifFolderMonitor : IDisposable
    {
        private readonly IFileSystem fileSystem;

        private readonly SarifViewerInterop viewerInterop;

        private IFileWatcher fileWatcher;

        private string solutionFolder = null;

        internal SarifFolderMonitor(IVsShell vsShell, IFileSystem fs = null)
        {
            this.viewerInterop = new SarifViewerInterop(vsShell);
            this.fileSystem = fs ?? new FileSystem();
        }

        public void Dispose()
        {
            (this.fileWatcher as FileWatcher)?.Dispose();
        }

        internal void StartWatch(string solutionPath = null)
        {
            if (!SariferOption.Instance.ShouldMonitorSarifFolder)
            {
                return;
            }

            this.solutionFolder = solutionPath ?? this.GetSolutionDirectory();
            if (string.IsNullOrEmpty(this.solutionFolder) || !fileSystem.DirectoryExists(this.solutionFolder))
            {
                return;
            }

            string sarifLogFolder = Path.Combine(this.solutionFolder, Constants.SarifFolderName);
            if (!fileSystem.DirectoryExists(sarifLogFolder))
            {
                return;
            }

            // load existing sarif logs
            this.LoadExistingSarifLogs(sarifLogFolder);

            if (this.fileWatcher == null)
            {
                this.fileWatcher = new FileWatcher(sarifLogFolder, Constants.SarifFileSearchPattern);

                // here no need to watch for sarif file log updates
                // because when load the sarif log file in viewer, its already monitored by viewer's file watcher
                this.fileWatcher.SarifLogFileCreated += this.Watcher_SarifLogFileCreated;
                this.fileWatcher.SarifLogFileDeleted += this.Watcher_SarifLogFileDeleted;
                this.fileWatcher.Start();
            }
        }

        internal void StopWatch()
        {
            if (!SariferOption.Instance.ShouldMonitorSarifFolder)
            {
                return;
            }

            if (this.fileWatcher != null)
            {
                this.fileWatcher.Stop();
                this.fileWatcher.SarifLogFileCreated -= this.Watcher_SarifLogFileCreated;
                this.fileWatcher.SarifLogFileDeleted -= this.Watcher_SarifLogFileDeleted;
                this.fileWatcher = null;
            }

            this.CloseExistingSarifLogs(Path.Combine(this.solutionFolder, Constants.SarifFolderName));
            this.solutionFolder = null;
        }

        internal void LoadExistingSarifLogs(string targetFolderPath)
        {
            if (!string.IsNullOrEmpty(targetFolderPath))
            {
                IEnumerable<string> sarifLogFiles = this.fileSystem.DirectoryGetFiles(targetFolderPath, Constants.SarifFileSearchPattern);
                ThreadHelper.JoinableTaskFactory.Run(() => this.viewerInterop.OpenSarifLogAsync(sarifLogFiles));
            }
        }

        internal void CloseExistingSarifLogs(string targetFolderPath)
        {
            if (!string.IsNullOrEmpty(targetFolderPath))
            {
                IEnumerable<string> sarifLogFiles = this.fileSystem.DirectoryGetFiles(targetFolderPath, Constants.SarifFileSearchPattern);
                ThreadHelper.JoinableTaskFactory.Run(() => this.viewerInterop.CloseSarifLogAsync(sarifLogFiles));
            }
        }

        internal async System.Threading.Tasks.Task LoadSarifLogAsync(string path)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            await this.viewerInterop.OpenSarifLogAsync(path, cleanErrors: false, openInEditor: false);
        }

        internal async System.Threading.Tasks.Task CloseSarifLogAsync(string path)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            await this.viewerInterop.CloseSarifLogAsync(new string[] { path });
        }

        private void Watcher_SarifLogFileCreated(object sender, FileSystemEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.Run(() => this.LoadSarifLogAsync(e.FullPath));
        }

        private void Watcher_SarifLogFileDeleted(object sender, FileSystemEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.Run(() => this.CloseSarifLogAsync(e.FullPath));
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

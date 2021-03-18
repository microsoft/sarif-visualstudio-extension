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
    /// Handles loading & monitoring sarif logs under solution directory .sarif folder.
    /// </summary>
    internal class SarifFolderMonitor : IDisposable
    {
        private readonly IFileSystem fileSystem;

        private readonly ISarifViewerInterop viewerInterop;

        private IFileWatcher fileWatcher;

        private string solutionFolder = null;

        internal SarifFolderMonitor(ISarifViewerInterop interop, IFileSystem fs = null, IFileWatcher watcher = null)
        {
            this.viewerInterop = interop;
            this.fileSystem = fs ?? new FileSystem();
            this.fileWatcher = watcher ?? new FileWatcher();
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

            this.fileWatcher ??= new FileWatcher();

            this.fileWatcher.WatcherFilePath = sarifLogFolder;
            this.fileWatcher.WatcherFilter = Constants.SarifFileSearchPattern;

            // here no need to watch for sarif file log updates
            // because when load the sarif log file in viewer, its already monitored by viewer's file watcher
            this.fileWatcher.SarifLogFileCreated += this.Watcher_SarifLogFileCreated;
            this.fileWatcher.SarifLogFileDeleted += this.Watcher_SarifLogFileDeleted;
            this.fileWatcher.Start();
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

            if (!string.IsNullOrEmpty(this.solutionFolder))
            {
                this.CloseExistingSarifLogs(Path.Combine(this.solutionFolder, Constants.SarifFolderName));
                this.solutionFolder = null;
            }
        }

        internal void LoadExistingSarifLogs(string targetFolderPath)
        {
            if (!string.IsNullOrEmpty(targetFolderPath) && this.fileSystem.DirectoryExists(targetFolderPath))
            {
                IEnumerable<string> sarifLogFiles = this.fileSystem.DirectoryGetFiles(targetFolderPath, Constants.SarifFileSearchPattern);
                if (SariferPackage.IsUnitTesting)
                {
                    this.viewerInterop.OpenSarifLogAsync(sarifLogFiles).ConfigureAwait(false);
                }
                else
                {
                    ThreadHelper.JoinableTaskFactory.Run(() => this.viewerInterop.OpenSarifLogAsync(sarifLogFiles));
                }
            }
        }

        internal void CloseExistingSarifLogs(string targetFolderPath)
        {
            if (!string.IsNullOrEmpty(targetFolderPath) && this.fileSystem.DirectoryExists(targetFolderPath))
            {
                IEnumerable<string> sarifLogFiles = this.fileSystem.DirectoryGetFiles(targetFolderPath, Constants.SarifFileSearchPattern);
                if (SariferPackage.IsUnitTesting)
                {
                    this.viewerInterop.CloseSarifLogAsync(sarifLogFiles).ConfigureAwait(false);
                }
                else
                {
                    ThreadHelper.JoinableTaskFactory.Run(() => this.viewerInterop.CloseSarifLogAsync(sarifLogFiles));
                }
            }
        }

        private void Watcher_SarifLogFileCreated(object sender, FileSystemEventArgs e)
        {
            if (SariferPackage.IsUnitTesting)
            {
                this.viewerInterop.OpenSarifLogAsync(e.FullPath, cleanErrors: false, openInEditor: false).ConfigureAwait(false);
            }
            else
            {
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    await this.viewerInterop.OpenSarifLogAsync(e.FullPath, cleanErrors: false, openInEditor: false);
                });
            }
        }

        private void Watcher_SarifLogFileDeleted(object sender, FileSystemEventArgs e)
        {
            if (SariferPackage.IsUnitTesting)
            {
                this.viewerInterop.CloseSarifLogAsync(new string[] { e.FullPath }).ConfigureAwait(false);
            }
            else
            {
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    await this.viewerInterop.CloseSarifLogAsync(new string[] { e.FullPath });
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

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using EnvDTE;

using EnvDTE80;

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    internal class AnalyzeFileCommand : IDisposable
    {
        private DTE2 dte;
        private IComponentModel componentModel;
        private IBackgroundAnalysisService backgroundAnalysisService;
        private readonly IMenuCommandService menuCommandService;
        private CancellationTokenSource cancellationTokenSource;
        private bool disposed;

        public AnalyzeFileCommand(IMenuCommandService menuCommandService)
        {
            var menuCommand = new MenuCommand(
                new EventHandler(this.MenuCommandCallback),
                new CommandID(Guids.SariferCommandSet, SariferPackageCommandIds.AnalyzeFile));

            menuCommandService.AddCommand(menuCommand);
            this.menuCommandService = menuCommandService;
        }

        /// <summary>
        /// Cancel current analysis.
        /// </summary>
        public void Cancel()
        {
            this.backgroundAnalysisService?.ClearResultsAsync()
                .FileAndForget(FileAndForgetEventName.BackgroundAnalysisFailure);
            this.cancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// Event handler called when the user selects the Analyze Project command.
        /// </summary>
        private void MenuCommandCallback(object caller, EventArgs args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Always cancel before start.
            this.cancellationTokenSource?.Cancel();
            this.cancellationTokenSource = new CancellationTokenSource();

            if (this.dte == null)
            {
                this.dte = (DTE2)Package.GetGlobalService(typeof(DTE));
            }

            if (this.componentModel == null)
            {
                this.componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            }

            if (this.backgroundAnalysisService == null)
            {
                this.backgroundAnalysisService = this.componentModel.GetService<IBackgroundAnalysisService>();
                this.backgroundAnalysisService.AnalysisCompleted += this.BackgroundAnalysisService_AnalysisCompleted;
            }

            var targetFiles = new List<string>();

            if (this.dte.SelectedItems != null && this.dte.SelectedItems.Count > 0)
            {
                foreach (SelectedItem selectedItem in this.dte.SelectedItems)
                {
                    targetFiles.AddRange(SariferPackageCommand.GetFiles(selectedItem));
                }

                if (targetFiles.Any())
                {
                    // Disable the menu click when we are analysing.
                    SariferPackageCommand.DisableAnalyzeCommands(this.menuCommandService);
                    string logId = targetFiles.First() + (targetFiles.Count > 1 ? $"~{targetFiles.Count}" : string.Empty);
                    this.backgroundAnalysisService.AnalyzeAsync(logId, targetFiles, this.cancellationTokenSource.Token)
                        .FileAndForget(FileAndForgetEventName.BackgroundAnalysisFailure);
                }
            }
        }

        private void BackgroundAnalysisService_AnalysisCompleted(object sender, EventArgs e)
        {
            SariferPackageCommand.EnableAnalyzeCommands(this.menuCommandService);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.cancellationTokenSource?.Cancel();
                    this.cancellationTokenSource?.Dispose();
                    this.backgroundAnalysisService.AnalysisCompleted -= this.BackgroundAnalysisService_AnalysisCompleted;
                }

                this.disposed = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

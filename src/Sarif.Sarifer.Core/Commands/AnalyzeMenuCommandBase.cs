// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.Design;
using System.Threading;

using EnvDTE;

using EnvDTE80;

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer.Commands
{
    internal class AnalyzeMenuCommandBase : IDisposable
    {
        protected DTE2 dte;
        protected IComponentModel componentModel;
        protected IBackgroundAnalysisService backgroundAnalysisService;
        protected IMenuCommandService menuCommandService;
        protected CancellationTokenSource cancellationTokenSource;
        protected bool disposed;

        public AnalyzeMenuCommandBase(IMenuCommandService menuCommandService, int commandId)
        {
            var menuCommand = new MenuCommand(
                new EventHandler(this.MenuCommandCallback),
                new CommandID(Guids.SariferCommandSet, commandId));

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

        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected void BackgroundAnalysisService_AnalysisCompleted(object sender, EventArgs e)
        {
            SariferPackageCommand.EnableAnalyzeCommands(this.menuCommandService);
        }

        /// <summary>
        /// Event handler called when the user selects the Analyze command.
        /// </summary>
        /// <param name="caller">The source of the event.</param>
        /// <param name="args">An object that contains event data.</param>
        protected virtual void MenuCommandCallback(object caller, EventArgs args)
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

            this.AnalyzeTargets();
        }

        protected virtual void AnalyzeTargets()
        {
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.cancellationTokenSource?.Cancel();
                    this.cancellationTokenSource?.Dispose();
                    if (this.backgroundAnalysisService != null)
                    {
                        this.backgroundAnalysisService.AnalysisCompleted -= this.BackgroundAnalysisService_AnalysisCompleted;
                    }
                }

                this.disposed = true;
            }
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Threading;

using EnvDTE;

using EnvDTE80;

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    internal class AnalyzeSolutionCommand : IDisposable
    {
        private DTE2 dte;
        private IComponentModel componentModel;
        private IBackgroundAnalysisService backgroundAnalysisService;
        private CancellationTokenSource cancellationTokenSource;
        private bool disposed;

        public AnalyzeSolutionCommand(IMenuCommandService menuCommandService)
        {
            var menuCommand = new MenuCommand(
                new EventHandler(this.MenuCommandCallback),
                new CommandID(Guids.SariferCommandSet, SariferPackageCommandIds.AnalyzeSolution));

            menuCommandService.AddCommand(menuCommand);
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
            }

            Solution solution = this.dte.Solution;
            if (solution == null)
            {
                return;
            }

            Projects projects = solution.Projects;
            if (projects == null)
            {
                return;
            }

            var targetFiles = new List<string>();
            foreach (Project project in projects)
            {
                targetFiles.AddRange(project.GetMemberFiles());
            }

            this.backgroundAnalysisService.AnalyzeAsync(solution.FullName, targetFiles, this.cancellationTokenSource.Token)
                .FileAndForget(FileAndForgetEventName.BackgroundAnalysisFailure);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.cancellationTokenSource?.Cancel();
                    this.cancellationTokenSource?.Dispose();
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

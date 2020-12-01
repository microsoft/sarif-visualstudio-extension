// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading;

using EnvDTE;

using EnvDTE80;

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    internal class AnalyzeProjectCommand : IDisposable
    {
        private DTE2 dte;
        private IComponentModel componentModel;
        private IBackgroundAnalysisService backgroundAnalysisService;
        private CancellationTokenSource cancellationTokenSource;
        private bool disposed;

        public AnalyzeProjectCommand(IMenuCommandService menuCommandService)
        {
            var menuCommand = new MenuCommand(
                new EventHandler(this.MenuCommandCallback),
                new CommandID(Guids.SariferCommandSet, SariferPackageCommandIds.AnalyzeProject));

            menuCommandService.AddCommand(menuCommand);
        }

        /// <summary>
        /// Event handler called when the user selects the Analyze Project command.
        /// </summary>
        private void MenuCommandCallback(object caller, EventArgs args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // always cancel before start
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

            IEnumerable<Project> selectedProjects = (this.dte.ActiveSolutionProjects as object[]).OfType<Project>();
            if (selectedProjects != null)
            {
                foreach (Project project in selectedProjects)
                {
                    List<string> targetFiles = project.GetMemberFiles();

                    this.backgroundAnalysisService.AnalyzeAsync(project.FullName, targetFiles, this.cancellationTokenSource.Token)
                        .FileAndForget(FileAndForgetEventName.BackgroundAnalysisFailure);
                }
            }
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

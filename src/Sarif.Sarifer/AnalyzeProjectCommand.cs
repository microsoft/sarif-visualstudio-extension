// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;

using EnvDTE;

using EnvDTE80;

using Microsoft.Sarif.Viewer.Interop;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    internal class AnalyzeProjectCommand
    {
        private readonly SarifViewerInterop viewerInterop;

        private IComponentModel componentModel;
        private DTE2 dte;
        private IBackgroundAnalysisService backgroundAnalysisService;

        public AnalyzeProjectCommand(IVsShell vsShell, IMenuCommandService menuCommandService)
        {
            this.viewerInterop = new SarifViewerInterop(vsShell);

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
                    var projectFiles = new List<string>();

                    ProjectItems projectItems = project.ProjectItems;
                    for (int i = 0; i < projectItems.Count; ++i)
                    {
                        ProjectItem projectItem = projectItems.Item(i + 1); // One-based index.
                        for (short j = 0; j < projectItem.FileCount; ++j)
                        {
                            projectFiles.Add(projectItem.FileNames[j]);
                        }
                    }

                    this.backgroundAnalysisService.StartProjectAnalysisAsync(project.FileName, projectFiles).FileAndForget(FileAndForgetEventName.BackgroundAnalysisFailure);
                }
            }
        }
    }
}

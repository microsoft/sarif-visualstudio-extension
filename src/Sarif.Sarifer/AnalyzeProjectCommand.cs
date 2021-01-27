// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;

using EnvDTE;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    internal class AnalyzeProjectCommand : AnalyzeMenuCommandBase
    {
        public AnalyzeProjectCommand(IMenuCommandService menuCommandService)
            : base(menuCommandService, SariferPackageCommandIds.AnalyzeProject)
        {
        }

        protected override void AnalyzeTargets()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IEnumerable<Project> selectedProjects = (this.dte.ActiveSolutionProjects as object[]).OfType<Project>();
            if (selectedProjects != null)
            {
                foreach (Project project in selectedProjects)
                {
                    // Disable the menu click when we are analysing.
                    SariferPackageCommand.DisableAnalyzeCommands(this.menuCommandService);

                    List<string> targetFiles = SariferPackageCommand.GetFiles(project);

                    this.backgroundAnalysisService.AnalyzeAsync(project.FullName, targetFiles, this.cancellationTokenSource.Token)
                        .FileAndForget(FileAndForgetEventName.BackgroundAnalysisFailure);
                }
            }
        }
    }
}

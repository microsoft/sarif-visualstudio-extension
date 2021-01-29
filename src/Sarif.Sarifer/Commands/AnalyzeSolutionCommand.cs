// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.Design;

using EnvDTE;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer.Commands
{
    internal class AnalyzeSolutionCommand : AnalyzeMenuCommandBase
    {
        public AnalyzeSolutionCommand(IMenuCommandService menuCommandService)
            : base(menuCommandService, SariferPackageCommandIds.AnalyzeSolution)
        {
        }

        protected override void AnalyzeTargets()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

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
                targetFiles.AddRange(SariferPackageCommand.GetFiles(project));
            }

            // Disable the menu click when we are analysing.
            SariferPackageCommand.DisableAnalyzeCommands(this.menuCommandService);
            this.backgroundAnalysisService.AnalyzeAsync(solution.FullName, targetFiles, this.cancellationTokenSource.Token)
                .FileAndForget(FileAndForgetEventName.BackgroundAnalysisFailure);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;

using EnvDTE;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer.Commands
{
    internal class AnalyzeFileCommand : AnalyzeMenuCommandBase
    {
        public AnalyzeFileCommand(IMenuCommandService menuCommandService)
            : base(menuCommandService, SariferPackageCommandIds.AnalyzeFile)
        {
        }

        protected override void AnalyzeTargets()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

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
    }
}

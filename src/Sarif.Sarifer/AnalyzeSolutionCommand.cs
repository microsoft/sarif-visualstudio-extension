// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;

using EnvDTE;

using EnvDTE80;

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    internal class AnalyzeSolutionCommand
    {
        private DTE2 dte;
        private IComponentModel componentModel;
        private IBackgroundAnalysisService backgroundAnalysisService;

        public AnalyzeSolutionCommand(IMenuCommandService menuCommandService)
        {
            var menuCommand = new MenuCommand(
                new EventHandler(this.MenuCommandCallback),
                new CommandID(Guids.SariferCommandSet, SariferPackageCommandIds.AnalyzeSolution));

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
                ProjectItems projectItems = project.ProjectItems;
                for (int i = 0; i < projectItems.Count; ++i)
                {
                    ProjectItem projectItem = projectItems.Item(i + 1); // One-based index.
                    for (short j = 0; j < projectItem.FileCount; ++j)
                    {
                        string projectMemberFile = null;

                        // Certain project items have a FileCount of 1, yet they throw ArgumentException
                        // when you try to index into FileNames. This indexing is known to be fragile,
                        // and whether the index is 1-based or 0-based depends on the file type:
                        // https://stackoverflow.com/questions/34884079/how-to-get-a-file-path-from-a-projectitem-via-the-filenames-property
                        try
                        {
                            projectMemberFile = projectItem.FileNames[j];
                        }
                        catch (ArgumentException ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to index into projectItem.FileNames. index = {j}, exception = {ex}");
                        }

                        // Make sure it's a file and not a directory.
                        if (projectMemberFile != null && File.Exists(projectMemberFile))
                        {
                            targetFiles.Add(projectMemberFile);
                        }
                    }
                }
            }

            this.backgroundAnalysisService.AnalyzeAsync(solution.FullName, targetFiles).FileAndForget(FileAndForgetEventName.BackgroundAnalysisFailure);
        }
    }
}

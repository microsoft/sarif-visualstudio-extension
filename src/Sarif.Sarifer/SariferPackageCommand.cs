// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;

using EnvDTE;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    internal static class SariferPackageCommand
    {
        public static void DisableAnalyzeCommands(IMenuCommandService menuCommandService)
        {
            UpdateAnalyzeCommandsState(menuCommandService, enabled: false);
        }

        public static void EnableAnalyzeCommands(IMenuCommandService menuCommandService)
        {
            UpdateAnalyzeCommandsState(menuCommandService, enabled: true);
        }

        public static List<string> GetFiles(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var targetFiles = new List<string>();

            foreach (ProjectItem projectItem in project.ProjectItems)
            {
                GetFilesRecursive(targetFiles, projectItem);
            }

            return targetFiles;
        }

        public static List<string> GetFiles(SelectedItem selectedItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var targetFiles = new List<string>();
            GetFilesRecursive(targetFiles, selectedItem.ProjectItem);

            return targetFiles;
        }

        private static void GetFilesRecursive(List<string> targetFiles, ProjectItem projectItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (projectItem.GetType().ToString().EndsWith("OAFolderItem", StringComparison.InvariantCulture))
            {
                foreach (ProjectItem projectFolder in projectItem.ProjectItems)
                {
                    GetFilesRecursive(targetFiles, projectFolder);
                }
            }
            else
            {
                string localPath = projectItem.Properties.Item("LocalPath").Value.ToString();
                if (!string.IsNullOrWhiteSpace(localPath))
                {
                    targetFiles.Add(localPath);
                }
            }
        }

        private static void UpdateAnalyzeCommandsState(IMenuCommandService menuCommandService, bool enabled)
        {
            var analyzeProjectCommandId = new CommandID(Guids.SariferCommandSet, SariferPackageCommandIds.AnalyzeProject);
            var analyzeSolutionCommandId = new CommandID(Guids.SariferCommandSet, SariferPackageCommandIds.AnalyzeSolution);
            var analyzeFileCommandId = new CommandID(Guids.SariferCommandSet, SariferPackageCommandIds.AnalyzeFile);

            MenuCommand analyzeProjectCommand = menuCommandService.FindCommand(analyzeProjectCommandId);
            analyzeProjectCommand.Enabled = enabled;

            MenuCommand analyzeSolutionCommand = menuCommandService.FindCommand(analyzeSolutionCommandId);
            analyzeSolutionCommand.Enabled = enabled;

            MenuCommand analyzeFileCommand = menuCommandService.FindCommand(analyzeFileCommandId);
            analyzeFileCommand.Enabled = enabled;
        }
    }
}

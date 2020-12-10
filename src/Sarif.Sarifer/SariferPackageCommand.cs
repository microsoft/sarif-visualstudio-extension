// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.Design;

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

        private static void UpdateAnalyzeCommandsState(IMenuCommandService menuCommandService, bool enabled)
        {
            var analyzeProjectCommandId = new CommandID(Guids.SariferCommandSet, SariferPackageCommandIds.AnalyzeProject);
            var analyzeSolutionCommandId = new CommandID(Guids.SariferCommandSet, SariferPackageCommandIds.AnalyzeSolution);

            MenuCommand analyzeProjectCommand = menuCommandService.FindCommand(analyzeProjectCommandId);
            analyzeProjectCommand.Enabled = enabled;

            MenuCommand analyzeSolutionCommand = menuCommandService.FindCommand(analyzeSolutionCommandId);
            analyzeSolutionCommand.Enabled = enabled;
        }
    }
}

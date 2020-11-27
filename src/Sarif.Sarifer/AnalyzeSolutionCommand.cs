// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.Design;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    internal class AnalyzeSolutionCommand
    {
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
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    internal class GenerateTestDataCommand : OleMenuCommand
    {
        public GenerateTestDataCommand() :
            base(
                new EventHandler(MenuCommandCallback),
                new CommandID(Guids.SariferCommandSet, SariferPackageCommandIds.GenerateTestData))
        {
        }

        /// <summary>
        /// Event handler called when the user selects the Generate SARIF Test Data command.
        /// </summary>
        private static void MenuCommandCallback(object caller, EventArgs args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
        }
    }
}

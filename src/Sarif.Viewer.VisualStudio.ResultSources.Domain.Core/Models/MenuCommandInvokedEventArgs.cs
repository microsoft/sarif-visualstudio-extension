// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Models
{
    /// <summary>
    /// Represents event data for the event fired when a menu command is invoked.
    /// </summary>
    public class MenuCommandInvokedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MenuCommandInvokedEventArgs"/> class.
        /// </summary>
        /// <param name="sarifResults">The <see cref="IList{T}"/> of <see cref="Result"/> instances associated with the menu command invocation.</param>
        /// <param name="menuCommand">The <see cref="OleMenuCommand"/> that was invoked.</param>
        public MenuCommandInvokedEventArgs(IList<Result> sarifResults, OleMenuCommand menuCommand)
        {
            this.SarifResults = sarifResults;
            this.MenuCommand = menuCommand;
        }

        /// <summary>
        /// Gets the <see cref="Result"/>s associated with the selected Error List item(s).
        /// </summary>
        public IList<Result> SarifResults { get; } = new List<Result>();

        /// <summary>
        /// Gets the <see cref="OleMenuCommand"/> that was invoked.
        /// </summary>
        public OleMenuCommand MenuCommand { get; }
    }
}

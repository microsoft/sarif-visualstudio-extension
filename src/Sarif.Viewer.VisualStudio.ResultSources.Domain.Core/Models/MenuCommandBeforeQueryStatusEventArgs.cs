// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis.Sarif;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Models
{
    /// <summary>
    /// Represents event data for the event fired before a menu command is queried for status.
    /// </summary>
    public class MenuCommandBeforeQueryStatusEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MenuCommandBeforeQueryStatusEventArgs"/> class.
        /// </summary>
        /// <param name="sarifResults">The <see cref="IList{T}"/> of <see cref="Result"/> instances associated with the menu command invocation.</param>
        /// <param name="selectedItemsCount">The number of items selected in the Error List.</param>
        public MenuCommandBeforeQueryStatusEventArgs(IList<Result> sarifResults, int selectedItemsCount)
        {
            this.SarifResults = sarifResults;
            this.SelectedItemsCount = selectedItemsCount;
        }

        /// <summary>
        /// Gets or sets the count of items selected in the Error List.
        /// </summary>
        public int SelectedItemsCount { get; set; }

        /// <summary>
        /// Gets the <see cref="SarifLog"/>s associated with the selected Error List item(s).
        /// </summary>
        public IList<Result> SarifResults { get; } = new List<Result>();
    }
}

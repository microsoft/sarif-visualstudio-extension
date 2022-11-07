// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Models
{
    public class ErrorListMenuItem
    {
        public ErrorListMenuItem(string text)
        {
            this.Text = text;
        }

        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Func{MenuCommandBeforeQueryStatusEventArgs, ResultSourceServiceAction}"/> that will be called by the menu command's BeforeQueryStatus event handler.
        /// </summary>
        public Func<MenuCommandBeforeQueryStatusEventArgs, Task<ResultSourceServiceAction>> BeforeQueryStatusMenuCommand { get; set; }
    }
}

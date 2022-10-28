// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Models
{
    public class ErrorListMenuCommand : ErrorListMenuItem
    {
        public ErrorListMenuCommand(string text)
            : base(text)
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="Func{MenuCommandInvokedEventArgs, ResultSourceServiceAction}"/> that will be called by the menu command's Invoke event handler.
        /// </summary>
        public Func<MenuCommandInvokedEventArgs, Task<ResultSourceServiceAction>> InvokeMenuCommand { get; set; }
    }
}

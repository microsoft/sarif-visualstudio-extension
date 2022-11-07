// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Models
{
    public enum ResultSourceServiceAction
    {
        /// <summary>
        /// Indicates that no action should be taken.
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicates that the menu command should be disabled.
        /// </summary>
        DisableMenuCommand = 1,

        /// <summary>
        /// Indicates that the Error List item corresponding to the current request should be dismissed.
        /// </summary>
        DismissSelectedItem = 2,

        /// <summary>
        /// Indicates that the selected Error List items should be dismissed.
        /// </summary>
        DismissAllSelectedItems = 3,
    }
}

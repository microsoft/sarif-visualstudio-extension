// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Models
{
    /// <summary>
    /// Defines well-known events that can be raised by result source services.
    /// </summary>
    public enum ResultSourceServiceEventType
    {
        /// <summary>
        /// None.
        /// </summary>
        None = 0,

        /// <summary>
        /// Analysis results have been updated by the service.
        /// </summary>
        ResultsUpdated = 1,

        /// <summary>
        /// Request to add items to the Error List context menu.
        /// </summary>
        RequestAddMenuItems = 2,
    }
}

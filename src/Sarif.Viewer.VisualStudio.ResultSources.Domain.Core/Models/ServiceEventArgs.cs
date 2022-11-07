// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Models
{
    /// <summary>
    /// Represents the base class for events that are raised by result source services.
    /// </summary>
    public class ServiceEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the well-known type of the service event.
        /// </summary>
        public ResultSourceServiceEventType ServiceEventType { get; set; }
    }
}

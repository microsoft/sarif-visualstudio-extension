// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Models
{
    /// <summary>
    /// Fired when an event in settings menu needs to be transmit to the result source services.
    /// </summary>
    public class SettingsEventArgs : EventArgs
    {
        /// <summary>
        /// An ID for the event that was fired.
        /// </summary>
        public string EventName;

        /// <summary>
        /// Any additional information tranmitted.
        /// </summary>
        public object Value;
    }
}

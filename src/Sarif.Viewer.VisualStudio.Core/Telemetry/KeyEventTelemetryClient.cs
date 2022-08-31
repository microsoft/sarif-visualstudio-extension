// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.Sarif.Viewer.Telemetry
{
    /// <summary>
    /// The class utilizes Visual Studio SDK TelemetryService to send telemetry data.
    /// </summary>
    internal class KeyEventTelemetryClient : ITelemetryClient
    {
        /// <inheritdoc/>
        public void PostEvent(TelemetryEvent eventName)
        {
            TelemetryService.DefaultSession.PostEvent(eventName);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.Sarif.Viewer.Telemetry
{
    public interface ITelemetryClient
    {
        /// <summary>
        /// Queues a telemetry event to be posted to a server.
        /// </summary>
        /// <param name="eventName">A telemetry event that is ready to be posted.</param>
        public void PostEvent(TelemetryEvent eventName);
    }
}

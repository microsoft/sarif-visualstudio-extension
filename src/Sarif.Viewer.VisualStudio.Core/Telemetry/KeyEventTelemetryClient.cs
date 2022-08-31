// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.Sarif.Viewer.Telemetry
{
    internal class KeyEventTelemetryClient : ITelemetryClient
    {
        public void PostEvent(TelemetryEvent eventName)
        {
            TelemetryService.DefaultSession.PostEvent(eventName);
        }
    }
}

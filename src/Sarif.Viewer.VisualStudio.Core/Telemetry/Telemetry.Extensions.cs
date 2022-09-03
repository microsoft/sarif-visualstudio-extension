// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.Sarif.Viewer.Sarif
{
    internal static class TelemetryExtension
    {
        public static void SetValue(this TelemetryEvent telemetryEvent, string key, string value)
        {
            if (!telemetryEvent.Properties.ContainsKey(key))
            {
                telemetryEvent.Properties[key] = value;
            }
        }
    }
}

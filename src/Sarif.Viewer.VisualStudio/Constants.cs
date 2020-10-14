﻿// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer
{
    internal static class Constants
    {
        public const string VSIX_NAME = "SARIF Viewer";

        public static class FileAndForgetFaultEventNames
        {
            /// <summary>
            /// Used when showing the error list fails.
            /// </summary>
            public const string ShowErrorList = "Microsoft/SARIF/Viewer/ShowErrorList";

            /// <summary>
            /// Used when the open SARIF log menu fails.
            /// </summary>
            public const string OpenSarifLogMenu = "Microsoft/SARIF/Viewer/OpenSARIFLogMenu";

            /// <summary>
            /// Used when loading a SARIF log through the load SARIF log service files.
            /// </summary>
            public const string LoadSarifLogs = "Microsoft/SARIF/Viewer/LoadSarifLogs";

            /// <summary>
            /// Indicates a telemetry write failed.
            /// </summary>
            public const string TelemetryWriteEvent = "Microsoft/SARIF/Viewer/Telemetry/WriteEvent";
        }
    }
}

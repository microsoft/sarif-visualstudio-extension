﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer
{
    internal static class Constants
    {
        public const string VSIX_NAME = "SARIF Viewer";

        public const string SarifFileSearchPattern = "*.sarif";

        public static class FileAndForgetFaultEventNames
        {
            /// <summary>
            /// Used when showing the error list fails.
            /// </summary>
            public const string ShowErrorList = "Microsoft/SARIF/Viewer/ShowErrorList/Failed";

            /// <summary>
            /// Used when the open SARIF log menu fails.
            /// </summary>
            public const string OpenSarifLogMenu = "Microsoft/SARIF/Viewer/OpenSARIFLogMenu/Failed";

            /// <summary>
            /// Used when loading a SARIF log through the load SARIF log service files.
            /// </summary>
            public const string LoadSarifLogs = "Microsoft/SARIF/Viewer/LoadSarifLogs/Failed";

            /// <summary>
            /// Used when sending enhanced data through the data service.
            /// </summary>
            public const string SendEnhancedData = "Microsoft/SARIF/Viewer/DataService/SendEnhancedData/Failed";

            /// <summary>
            /// Used when writing telemetry event fails.
            /// </summary>
            public const string TelemetryWriteEvent = "Microsoft/SARIF/Viewer/Telemetry/WriteEvent/Failed";

            /// <summary>
            /// Used when writing log into the output window pane fails.
            /// </summary>
            public const string OutputWindowEvent = "Microsoft/SARIF/Viewer/OutputPane/Write/Failed";
        }
    }
}

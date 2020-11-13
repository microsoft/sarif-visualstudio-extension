// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    internal static class FileAndForgetEventName
    {
        private const string Prefix = "Microsoft/Sarifer/";

        internal const string CloseSarifLogsFailure = Prefix + "CloseSarifLogs/Failure";
        internal const string SendDataToViewerFailure = Prefix + "SendDataToViewer/Failure";
    }
}

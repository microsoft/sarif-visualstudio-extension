// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    internal static class FileAndForgetEventName
    {
        private const string Prefix = "Microsoft/Sarifer/";

        private static readonly Lazy<string> s_sendDataToViewerFailure = new Lazy<string>(() => MakeEventName("SendDataToViewer/Failure"));

        internal static string SendDataToViewerFailure => s_sendDataToViewerFailure.Value;

        private static string MakeEventName(string suffix) => $"{Prefix}{suffix}";
    }
}

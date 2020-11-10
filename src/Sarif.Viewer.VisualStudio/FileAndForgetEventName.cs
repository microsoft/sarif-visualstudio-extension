
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Sarif.Viewer
{
    internal static class FileAndForgetEventName
    {
        private const string Prefix = "Microsoft/VisualStudioSarifViewer/";

        private static readonly Lazy<string> s_infoBarCloseFailure = new Lazy<string>(() => MakeEventName("InfoBarClose/Failure"));

        internal static string InfoBarCloseFailure => s_infoBarCloseFailure.Value;

        private static string MakeEventName(string suffix) => $"{Prefix}{suffix}";
    }
}

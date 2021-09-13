// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer
{
    internal static class FileAndForgetEventName
    {
        private const string Prefix = "Microsoft/VisualStudioSarifViewer/";

        internal const string InfoBarCloseFailure = Prefix + "InfoBarClose/Failure";
        internal const string InfoBarOpenFailure = Prefix + "InfoBarOpen/Failure";
    }
}

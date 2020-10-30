// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    internal static class FileAndForget
    {
        private const string FileAndForgetEventNamePrefix = "Microsoft/Sarifer/";

        internal static string EventName(string suffix) => $"{FileAndForgetEventNamePrefix}{suffix}";
    }
}
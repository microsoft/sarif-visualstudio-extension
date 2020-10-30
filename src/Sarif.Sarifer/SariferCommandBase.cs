// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    internal abstract class SariferCommandBase
    {
        private const string FileAndForgetEventNamePrefix = "Microsoft/Sarifer/";

        protected string GetFileAndForgetEventName(string suffix) => $"{FileAndForgetEventNamePrefix}{suffix}";
    }
}
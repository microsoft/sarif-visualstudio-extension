// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain
{
    internal static class GitHubInfoBarHelper
    {
        internal static ImageMoniker GetInfoBarImageMoniker()
        {
            return KnownMonikers.GitHub;
        }
    }
}

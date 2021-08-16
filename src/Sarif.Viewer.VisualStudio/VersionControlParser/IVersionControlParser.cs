// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Sarif.Viewer
{
    public interface IVersionControlParser
    {
        public Uri GetSourceFileUri(string relativeFilePath);

        public string ConvertToRawPath(string url);

        public string GetLocalRelativePath(Uri uri, string relativeFilePath);
    }
}

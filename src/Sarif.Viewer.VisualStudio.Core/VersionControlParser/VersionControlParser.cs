// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Sarif;

namespace Microsoft.Sarif.Viewer
{
    public abstract class VersionControlParser : IVersionControlParser
    {
        protected readonly VersionControlDetails details;

        internal protected VersionControlParser(VersionControlDetails versionControl)
        {
            this.details = versionControl;
        }

        public abstract string ConvertToRawPath(string url);

        public abstract string GetLocalRelativePath(Uri uri, string relativeFilePath);

        public abstract Uri GetSourceFileUri(string relativeFilePath);

        protected Uri CreateUri(string sourceRelativePath)
        {
            var baseUri = new Uri(this.details.RepositoryUri.ToString());
            if (!baseUri.AbsolutePath.EndsWith("/"))
            {
                UriBuilder builder = new UriBuilder(baseUri);
                builder.Path = baseUri.AbsolutePath + "/";
                baseUri = builder.Uri;
            }

            if (Uri.TryCreate(baseUri, sourceRelativePath, out Uri sourceUri) &&
                sourceUri.IsHttpScheme())
            {
                return new Uri(ConvertToRawPath(sourceUri.ToString()));
            }

            return null;
        }
    }
}

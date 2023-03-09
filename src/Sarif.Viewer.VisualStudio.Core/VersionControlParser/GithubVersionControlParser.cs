// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis.Sarif;

namespace Microsoft.Sarif.Viewer
{
    internal class GithubVersionControlParser : VersionControlParser
    {
        private static readonly Regex regex = new Regex(
            @"^(?<protocol>https?://)(?<site>github\.com)/(?<user>.*?)/(?<repo>.*?)(?<folder>/tree/|/blob/)(?<path>.*?)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        internal GithubVersionControlParser(VersionControlDetails versionControl)
            : base(versionControl) { }

        public override Uri GetSourceFileUri(string relativeFilePath)
        {
            // github link format:
            // https://github.com/<username>/<reponame>/<blob|tree>/<commitmentid|branch>/path/to/file

            string sourceRelativePath = "blob/";
            sourceRelativePath += this.details.Branch ?? this.details.RevisionId;
            sourceRelativePath += "/";
            sourceRelativePath += relativeFilePath;

            return CreateUri(sourceRelativePath);
        }

        public override string ConvertToRawPath(string url)
        {
            // convert github file access page link to file raw content link
            // from https://github.com/<username>/<repo>/<blob|tree>/<branch>/path/to/file
            // to   https://raw.githubusercontent.com/<username>/<repo>/<branch>/path/to/file

            if (string.IsNullOrWhiteSpace(url) ||
                !(url.StartsWith("http://github.com") || url.StartsWith("https://github.com")))
            {
                return url;
            }

            if (!regex.IsMatch(url))
            {
                return url;
            }

            return regex.Replace(url,
                          m => m.Groups["protocol"].Value +
                               "raw.githubusercontent.com" + "/" +
                               m.Groups["user"].Value + "/" +
                               m.Groups["repo"].Value + "/" +
                               m.Groups["path"]);
        }

        public override string GetLocalRelativePath(Uri uri, string relativeFilePath)
        {
            // for Github the file link url has full path to the file.
            // e.g. https://github.com/<username>/<repo>/<blob|tree>/<branch>/path/to/file
            // so return null to let caller construct file path using url
            return null;
        }
    }
}

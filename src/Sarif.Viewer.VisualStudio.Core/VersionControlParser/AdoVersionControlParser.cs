// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Sarif;

namespace Microsoft.Sarif.Viewer
{
    public class AdoVersionControlParser : IVersionControlParser
    {
        private readonly VersionControlDetails details;
        private static readonly Regex regex = new Regex(
            @"^(?<protocol>https?://)(?<site>dev\.azure\.com)/(?<org>.*?)/(?<project>.*?)/(?<api>_git)/(?<repo>.*?)\?path=(?<filepath>.*?)(&version=GB(?<version>.*?))?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        internal AdoVersionControlParser(VersionControlDetails versionControl)
        {
            this.details = versionControl;
        }

        public Uri GetSourceFileUri(string relativeFilePath)
        {
            // github link format:
            // https://dev.azure.com/{org}/{project}/_git/{repo}?path={filepath}

            string sourceRelativePath = $"?path={relativeFilePath}";
            string version = this.details.Branch ?? this.details.RevisionId;
            sourceRelativePath += string.IsNullOrWhiteSpace(version) ? string.Empty : $"version=GB{version}";

            if (Uri.TryCreate(this.details.RepositoryUri, sourceRelativePath, out Uri sourceUri) &&
                sourceUri.IsHttpScheme())
            {
                return new Uri(ConvertToRawPath(sourceUri.ToString()));
            }

            return null;
        }

        public string ConvertToRawPath(string url)
        {
            // convert ado file access page link to file raw content link
            // from https://dev.azure.com/{org}/{project}/_git/{repo}?path={filepath}&version={branch|commitId}
            // to   https://dev.azure.com/{org}/{project}/_apis/git/repositories/{repo}/items?path={filepath}&version={branch|commitId}

            if (string.IsNullOrWhiteSpace(url) ||
                !(url.StartsWith($"http://{VersionControlParserFactory.AdoHost}", StringComparison.OrdinalIgnoreCase) ||
                  url.StartsWith($"https://{VersionControlParserFactory.AdoHost}", StringComparison.OrdinalIgnoreCase)))
            {
                return url;
            }

            if (!regex.IsMatch(url))
            {
                return url;
            }

            return regex.Replace(url,
                          m => m.Groups["protocol"].Value +
                               m.Groups["site"].Value + "/" +
                               m.Groups["org"].Value + "/" +
                               m.Groups["project"].Value + "/" +
                               "_apis/git/repositories" + "/" +
                               m.Groups["repo"].Value + "/" +
                               "items?path=" + m.Groups["filepath"].Value +
                               (m.Groups["version"].Success ?
                                   "&versionDescriptor[version]=" + m.Groups["version"].Value :
                                   string.Empty));
        }

        public string GetLocalRelativePath(Uri uri, string relativeFilePath)
        {
            // for Ado the file link url has not full path to the file.
            // e.g. https://dev.azure.com/{org}/{project}/_git/{repo}?path={filepath}
            // the file path is part of query parameter, not the path.
            // so return the file path to let caller use this path instead.
            return relativeFilePath;
        }
    }
}

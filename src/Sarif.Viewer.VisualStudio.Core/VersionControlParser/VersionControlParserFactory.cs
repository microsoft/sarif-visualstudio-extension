// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Sarif;

namespace Microsoft.Sarif.Viewer
{
    internal class VersionControlParserFactory
    {
        // known version control host
        internal static string GithubHost = "github.com";
        internal static string AdoHost = "dev.azure.com";

        private static readonly Regex s_githubRegex = new Regex(@"^https?://github\.com", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex s_adoRegex = new Regex(@"^https?://dev\.azure\.com", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        internal static bool TryGetVersionControlParser(VersionControlDetails versionControl, out IVersionControlParser parser)
        {
            if (versionControl?.RepositoryUri != null &&
                versionControl.RepositoryUri.IsHttpScheme() &&
                versionControl.RepositoryUri.Host.Equals(GithubHost, StringComparison.OrdinalIgnoreCase))
            {
                parser = new GithubVersionControlParser(versionControl);
                return true;
            }

            // not working due to need credential to access ado/vsts files
            /*
            if (versionControl.RepositoryUri.IsHttpScheme() &&
                versionControl.RepositoryUri.Host.Equals(AdoHost, StringComparison.OrdinalIgnoreCase))
            {
                parser = new AdoVersionControlParser(versionControl);
                return true;
            }
            */

            // no corresponding parser found
            parser = null;
            return false;
        }

        internal static string ConvertToRawFileLink(string url)
        {
            if (!string.IsNullOrWhiteSpace(url) && s_githubRegex.IsMatch(url))
            {
                return new GithubVersionControlParser(null).ConvertToRawPath(url);
            }

            // not working due to need credential to access ado/vsts files
            /*
            if (!string.IsNullOrWhiteSpace(url) && adoRegx.IsMatch(url))
            {
                return new AdoVersionControlParser(null).ConvertToRawPath(url);
            }
            */

            return url;
        }

        internal static Uri ConvertToRawFileLink(Uri url)
        {
            return new Uri(ConvertToRawFileLink(url.ToString()));
        }
    }
}

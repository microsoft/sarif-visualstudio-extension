// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Sarif;

namespace Microsoft.Sarif.Viewer
{
    internal class VersionControlParserFactory
    {
        // known version control host
        internal static string GithubHost = "github.com";
        internal static string AdoHost = "dev.azure.com";

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
            if (!string.IsNullOrWhiteSpace(url) &&
                (url.StartsWith($"http://{GithubHost}") || url.StartsWith($"https://{GithubHost}")))
            {
                return new GithubVersionControlParser(null).ConvertToRawPath(url);
            }

            // not working due to need credential to access ado/vsts files
            /*
            if (!string.IsNullOrWhiteSpace(url) &&
                (url.StartsWith($"http://{AdoHost}") || url.StartsWith($"https://{AdoHost}")))
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

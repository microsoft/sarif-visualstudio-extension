// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;

namespace Microsoft.Sarif.Viewer.Sarif
{
    static class UriExtensions
    {
        // The acceptable URI schemes
        static HashSet<string> s_httpSchemes = new HashSet<string>(new string[] { Uri.UriSchemeHttp, Uri.UriSchemeHttps },
                                                                   StringComparer.OrdinalIgnoreCase);

        public static string ToPath(this Uri uri)
        {
            if (uri == null)
            {
                return null;
            }

            if (uri.IsAbsoluteUri)
            {
                if (IsHttpScheme(uri))
                {
                    return uri.ToString();
                }
                else
                {
                    return uri.LocalPath + uri.Fragment;
                }
            }
            else
            {
                return uri.OriginalString;
            }
        }

        public static bool IsHttpScheme(this Uri uri)
        {
            return s_httpSchemes.Contains(uri.Scheme);
        }

        public static Uri WithTrailingSlash(this Uri uri)
        {
            const string Slash = "/";

            string uriString = uri.ToString();
            if (uriString.EndsWith(Slash))
            {
                return uri;
            }

            return new Uri(uriString.ToString() + Slash, uri.IsAbsoluteUri ? UriKind.Absolute : UriKind.Relative);
        }
    }
}

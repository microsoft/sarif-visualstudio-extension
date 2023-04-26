// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Sarif.Viewer.Sarif
{
    internal static class SarifErrorListItemExtensions
    {
        internal static string GetCombinedRuleIds(this IEnumerable<SarifErrorListItem> sarifErrorListItems)
        {
            return string.Join(";", sarifErrorListItems.Select(item => item.Rule.Id).Distinct());
        }

        internal static string GetCombinedToolNames(this IEnumerable<SarifErrorListItem> sarifErrorListItems)
        {
            return string.Join(";", sarifErrorListItems.Select(item => item.Tool?.Name).Distinct());
        }

        internal static string GetCombinedToolVersions(this IEnumerable<SarifErrorListItem> sarifErrorListItems)
        {
            return string.Join(";", sarifErrorListItems.Select(item => item.Tool?.Version).Distinct());
        }

        internal static IEnumerable<string> GetCombinedSnippets(this IEnumerable<SarifErrorListItem> sarifErrorListItems)
        {
            List<string> snippets = new List<string>();
            foreach (SarifErrorListItem item in sarifErrorListItems)
            {
                snippets.AddRange(item.GetCodeSnippets());
            }

            return snippets;
        }
    }
}

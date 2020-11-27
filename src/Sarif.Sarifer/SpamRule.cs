// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    public class SpamRule
    {
        public string Id { get; }
        public string SearchPattern { get; }
        public string ReplacePattern { get; }
        public string Message { get; }
        public string Description { get; }
        public Regex SearchPatternRegex { get; }

        public SpamRule(string id, string searchPattern, string replacePattern, string description, string message)
        {
            this.Id = id;
            this.SearchPattern = searchPattern;
            this.ReplacePattern = replacePattern;
            this.Description = description;
            this.Message = message;

            this.SearchPatternRegex = new Regex(this.SearchPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
    }
}

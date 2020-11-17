// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    internal class Rule
    {
        public string Id { get; set; }
        public string SearchPattern { get; set; }
        public string ReplacePattern { get; set; }
        public string Message { get; set; }
        public string Description { get; set; }

        public Rule(string id, string searchPattern, string replacePattern, string description, string message)
        {
            this.Id = id;
            this.SearchPattern = searchPattern;
            this.ReplacePattern = replacePattern;
            this.Description = description;
            this.Message = message;
        }

        public Rule()
        {

        }
    }
}

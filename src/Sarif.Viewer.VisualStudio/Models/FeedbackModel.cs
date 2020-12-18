// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.Models
{
    // View model for the feedback dialog
    public class FeedbackModel
    {
        public FeedbackModel(string ruleId)
        {
            this.RuleId = ruleId;
            this.SendSnippet = true;
            this.Comment = string.Empty;
        }

        public string RuleId { get; }
        public bool SendSnippet { get; set; }
        public string Comment { get; set; }
    }
}

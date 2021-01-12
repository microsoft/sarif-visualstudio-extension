// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.Sarif.Viewer.Models;

using Newtonsoft.Json;

namespace Microsoft.Sarif.Viewer.Telemetry
{
    internal class FeedbackTelemetryEvent
    {
        // property names
        private static readonly string Reason = "reason";
        private static readonly string ToolName = "toolName";
        private static readonly string ToolVersion = "toolVersion";
        private static readonly string RuleId = "ruleId";
        private static readonly string Comments = "comments";
        private static readonly string Snippet = "snippet";

        public static void SendFeedbackTelemetryEvent(FeedbackModel feedback)
        {
            Dictionary<string, string> properties = getProperties(feedback);

            TelemetryProvider.TrackEvent<FeedbackTelemetryEvent>(properties, nameof(FeedbackTelemetryEvent));
        }

        private static Dictionary<string, string> getProperties(FeedbackModel feedback)
        {
            var properties = new Dictionary<string, string>
            {
                { Reason, feedback.FeedbackType.ToString() },
                { ToolName, feedback.ToolName },
                { ToolVersion, feedback.ToolVersion },
                { RuleId, feedback.RuleId },
                { Comments, feedback.Comment }
            };

            if (feedback.SendSnippet)
            {
                properties.Add(Snippet, JsonConvert.SerializeObject(feedback.Snippets));
            }

            return properties;
        }
    }
}

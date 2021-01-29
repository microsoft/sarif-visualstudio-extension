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
        private static readonly string SarifLog = "sariflog";
        private static readonly int MaxLength = 8192;

        public static void SendFeedbackTelemetryEvent(FeedbackModel feedback)
        {
            Dictionary<string, string> properties = GetProperties(feedback);

            TelemetryProvider.TrackEvent<FeedbackTelemetryEvent>(properties, nameof(FeedbackTelemetryEvent));
        }

        private static Dictionary<string, string> GetProperties(FeedbackModel feedback)
        {
            var properties = new Dictionary<string, string>
            {
                { Reason, feedback.FeedbackType.ToString() },
                { ToolName, feedback.ToolName },
                { ToolVersion, feedback.ToolVersion },
                { RuleId, feedback.RuleId },
                { Comments, feedback.Comment },
            };

            if (feedback.SendSnippet)
            {
                properties.Add(Snippet, JsonConvert.SerializeObject(feedback.Snippets));
            }

            if (feedback.SarifLog != null)
            {
                // property has limit of 8192 on string length
                string sarifLog = JsonConvert.SerializeObject(feedback.SarifLog);
                if (sarifLog.Length > MaxLength)
                {
                    sarifLog = sarifLog.Substring(0, MaxLength);
                }

                properties.Add(SarifLog, sarifLog);
            }

            return properties;
        }
    }
}

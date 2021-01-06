// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Sarif.Viewer.Models;

namespace Microsoft.Sarif.Viewer.Telemetry
{
    internal class FeedbackTelemetryEvent
    {
        private static readonly string PropertyPrefix = "sairf.visualStudio.extension.feedback.";
        private static readonly string Reason = PropertyPrefix + "reason";
        private static readonly string ToolName = PropertyPrefix + "toolName";
        private static readonly string ToolVersion = PropertyPrefix + "toolVersion";
        private static readonly string RuleId = PropertyPrefix + "ruleId";
        private static readonly string Comments = PropertyPrefix + "comments";
        private static readonly string Snippet = PropertyPrefix + "snippet";

        public static void SendFeedbackTelemetryEvent(FeedbackModel feedback)
        {
            Dictionary<string, string> properties = getProperties(feedback);

            TelemetryProvider.TrackEvent<FeedbackTelemetryEvent>(properties); 
        }

        private static Dictionary<string, string> getProperties(FeedbackModel feedback)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add(Reason, feedback.FeedbackType.ToString());
            properties.Add(ToolName, feedback.ToolName);
            properties.Add(ToolVersion, feedback.ToolVersion);
            properties.Add(RuleId, feedback.RuleId);
            properties.Add(Comments, feedback.Comment);
            if (feedback.SendSnippet)
            {
                properties.Add(Snippet, feedback.Snippet);
            }
            
            return properties;
        }
    }
}

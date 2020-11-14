// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis.Sarif;

using Newtonsoft.Json.Linq;

namespace Microsoft.Sarif.Viewer.Sarif
{
    internal static class RunExtensions
    {
        private static readonly object s_syncRoot = new object();
        private static JObject s_ruleMetadata;

        internal static JObject RuleMetadata
        {
            get
            {
                if (s_ruleMetadata == null)
                {
                    lock (s_syncRoot)
                    {
                        if (s_ruleMetadata == null)
                        {
                            byte[] ruleLookupBytes = Resources.RuleLookup;
                            string ruleLookupText = Encoding.UTF8.GetString(ruleLookupBytes);
                            s_ruleMetadata = JObject.Parse(ruleLookupText);
                        }
                    }
                }

                return s_ruleMetadata;
            }
        }

        public static bool TryGetRule(this Run run, string ruleId, out ReportingDescriptor reportingDescriptor)
        {
            reportingDescriptor = null;

            if (run?.Tool?.Driver?.Rules != null && ruleId != null)
            {
                List<ReportingDescriptor> rules = run.Tool.Driver.Rules as List<ReportingDescriptor>;
                reportingDescriptor = rules.Where(r => r.Id == ruleId).FirstOrDefault();
            }
            else if (ruleId != null)
            {
                // No rule in log file. 
                // If the rule is a PREfast rule, create a "fake" rule using the external rule metadata file.
                if (RuleMetadata[ruleId] != null)
                {
                    string ruleName = null;
                    if (RuleMetadata[ruleId]["heading"] != null)
                    {
                        ruleName = RuleMetadata[ruleId]["heading"].Value<string>();
                    }

                    Uri helpUri = null;
                    if (RuleMetadata[ruleId]["url"] != null)
                    {
                        helpUri = new Uri(RuleMetadata[ruleId]["url"].Value<string>());
                    }

                    if (ruleName != null || helpUri != null)
                    {
                        reportingDescriptor = new ReportingDescriptor(
                            ruleId,
                            null,
                            ruleName,
                            null,
                            ruleName,
                            null,
                            shortDescription: null,
                            fullDescription: null,
                            messageStrings: null,
                            defaultConfiguration: null,
                            helpUri: helpUri,
                            help: null, // PREfast rules don't need a "help" property; they all have online documentation.
                            relationships: null,
                            properties: null);
                    }
                }
            }

            return reportingDescriptor != null;
        }

        public static string GetToolName(this Run run)
        {
            if (run?.Tool?.Driver == null)
            {
                return null;
            }

            return run.Tool.Driver.FullName ?? run.Tool.Driver.Name;
        }

        public static bool HasNoResults(this Run run) => run.Results?.Count == 0;

        public static bool HasErrorLevelToolConfigurationNotifications(this Run run)
        {
            if (run.Invocations == null || run.Invocations.Count == 0)
            {
                return false;
            }

            foreach (Invocation invocation in run.Invocations)
            {
                if (invocation.ToolConfigurationNotifications?.Any(not => not.Level == FailureLevel.Error) == true)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasErrorLevelToolExecutionNotifications(this Run run)
        {
            if (run.Invocations == null || run.Invocations.Count == 0)
            {
                return false;
            }

            foreach (Invocation invocation in run.Invocations)
            {
                if (invocation.ToolExecutionNotifications?.Any(not => not.Level == FailureLevel.Error) == true)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

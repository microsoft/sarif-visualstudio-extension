// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;

namespace Microsoft.Sarif.Viewer.Sarif
{
    static class RuleExtensions
    {
        public static RuleModel ToRuleModel(this ReportingDescriptor rule, string defaultRuleId)
        {
            RuleModel model;
            string ruleId = defaultRuleId;

            if (Guid.TryParse(ruleId, out Guid result) && !(string.IsNullOrEmpty(rule.Name)))
            {
                ruleId = rule.Name;
            }


            if (rule == null)
            {
                model = new RuleModel()
                {
                    Id = ruleId,
                    DefaultFailureLevel = FailureLevel.Warning
                };
            }
            else
            {
                model = new RuleModel()
                {
                    Id = ruleId,
                    Name = rule.Name,
                    Description = rule.FullDescription?.Text,
                    DefaultFailureLevel = rule.DefaultConfiguration != null ?
                                    rule.DefaultConfiguration.Level :
                                    FailureLevel.Warning, // Default level
                    HelpUri = rule.HelpUri?.IsAbsoluteUri == true ? rule.HelpUri.AbsoluteUri : null
                };
            }

            return model;
        }
    }
}

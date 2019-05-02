// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;

namespace Microsoft.Sarif.Viewer.Sarif
{
    static class RuleExtensions
    {
        public static RuleModel ToRuleModel(this ReportingDescriptor rule, string defaultRuleId)
        {
            RuleModel model;

            if (rule == null)
            {
                model = new RuleModel()
                {
                    Id = defaultRuleId,
                    DefaultFailureLevel = FailureLevel.Warning
                };
            }
            else
            {
                model = new RuleModel()
                {
                    Id = rule.Id,
                    Name = rule.Name,
                    Description = rule.FullDescription?.Text,
                    DefaultFailureLevel = rule.DefaultConfiguration != null ?
                                    rule.DefaultConfiguration.Level :
                                    FailureLevel.Warning, // Default level
                    HelpUri = rule.HelpUri?.AbsoluteUri
                };
            }

            return model;
        }
    }
}

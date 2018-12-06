﻿// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;

namespace Microsoft.Sarif.Viewer.Sarif
{
    static class RuleExtensions
    {
        public static RuleModel ToRuleModel(this IRule rule, string defaultRuleId)
        {
            RuleModel model;

            if (rule == null)
            {
                model = new RuleModel()
                {
                    Id = defaultRuleId,
                };
            }
            else
            {
                model = new RuleModel()
                {
                    Id = rule.Id,
                    Name = rule.Name?.Text,
                    Description = rule.FullDescription?.Text,
                    DefaultLevel = rule.Configuration != null ?
                                    rule.Configuration.DefaultLevel :
                                    RuleConfigurationDefaultLevel.Warning, // Default level
                    HelpUri = rule.HelpUri?.AbsoluteUri
                };
            }

            return model;
        }
    }
}

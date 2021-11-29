// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.Sarif.Viewer.Sarif;

using Xunit;

namespace Microsoft.Sarif.Viewer.Models
{
    public class RuleModelTests
    {
        [Fact]
        public void RuleModel_WhenRuleIdIsGuid_IdShouldBeSameAsName()
        {
            string ruleId = Guid.NewGuid().ToString();
            ReportingDescriptor rule = new ReportingDescriptor
            {
                Id = ruleId,
                Name = "TestRule",
            };

            RuleModel ruleModel = rule.ToRuleModel(ruleId);
            ruleModel.Id.Should().BeEquivalentTo(rule.Name);
        }

        [Fact]
        public void RuleModel_WhenRuleIdIsSameAsName_DisplayNameShouldBeNull()
        {
            string ruleName = "TEST1001/Sub001";
            ReportingDescriptor rule = new ReportingDescriptor
            {
                Id = ruleName,
                Name = ruleName,
            };

            RuleModel ruleModel = rule.ToRuleModel(ruleName);
            ruleModel.Id.Should().BeEquivalentTo(ruleName);
            ruleModel.Name.Should().BeEquivalentTo(ruleName);
            ruleModel.DisplayName.Should().BeNull();
        }

        [Fact]
        public void RuleModel_RuleNameIsNull()
        {
            string ruleId = "TEST1001/Sub001";
            ReportingDescriptor rule = new ReportingDescriptor
            {
                Id = ruleId,
                Name = null,
            };

            RuleModel ruleModel = rule.ToRuleModel(ruleId);
            ruleModel.Id.Should().BeEquivalentTo(ruleId);
            ruleModel.Name.Should().BeNull();
            ruleModel.DisplayName.Should().BeNull();
        }

        [Fact]
        public void RuleModel_WithoutDefaultLevel_LevelShouldBeWarning()
        {
            string ruleId = "TEST1001/Sub001";
            ReportingDescriptor rule = new ReportingDescriptor
            {
                Id = ruleId,
                Name = null,
            };

            RuleModel ruleModel = rule.ToRuleModel(ruleId);
            ruleModel.DefaultFailureLevel.Should().Be(FailureLevel.Warning);
            ruleModel.FailureLevel.Should().Be(FailureLevel.Warning);
        }

        [Fact]
        public void RuleModel_Level_ShouldBeSameAsDefaultLevel()
        {
            string ruleId = "TEST1001/Sub001";
            ReportingDescriptor rule = new ReportingDescriptor
            {
                Id = ruleId,
                Name = null,
                DefaultConfiguration = new ReportingConfiguration
                {
                    Level = FailureLevel.Error,
                }
            };

            RuleModel ruleModel = rule.ToRuleModel(ruleId);
            ruleModel.DefaultFailureLevel.Should().Be(FailureLevel.Error);
            ruleModel.FailureLevel.Should().Be(FailureLevel.Error);
        }
    }
}

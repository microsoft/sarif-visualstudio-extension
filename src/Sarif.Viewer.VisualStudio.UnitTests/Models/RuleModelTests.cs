// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Sarif;

using Xunit;

namespace Microsoft.Sarif.Viewer.Models
{
    public class RuleModelTests
    {
        private const string testRuleId = "TEST1001/Sub001";
        private const string testRuleName = "TestRule";

        [Fact]
        public void RuleModel_WhenRuleIdIsGuid_IdShouldBeSameAsName()
        {
            string ruleId = Guid.NewGuid().ToString();
            ReportingDescriptor rule = new ReportingDescriptor
            {
                Id = ruleId,
                Name = testRuleName,
            };

            RuleModel ruleModel = rule.ToRuleModel(ruleId);
            ruleModel.Id.Should().BeEquivalentTo(rule.Name);
        }

        [Fact]
        public void RuleModel_WhenRuleIdIsSameAsName_DisplayNameShouldBeNull()
        {
            ReportingDescriptor rule = new ReportingDescriptor
            {
                Id = testRuleId,
                Name = testRuleId,
            };

            RuleModel ruleModel = rule.ToRuleModel(testRuleId);
            ruleModel.Id.Should().BeEquivalentTo(testRuleId);
            ruleModel.Name.Should().BeEquivalentTo(testRuleId);
            ruleModel.DisplayName.Should().BeNull();
        }

        [Fact]
        public void RuleModel_RuleNameIsNull()
        {
            ReportingDescriptor rule = new ReportingDescriptor
            {
                Id = testRuleId,
                Name = null,
            };

            RuleModel ruleModel = rule.ToRuleModel(testRuleId);
            ruleModel.Id.Should().BeEquivalentTo(testRuleId);
            ruleModel.Name.Should().BeNull();
            ruleModel.DisplayName.Should().BeNull();
        }

        [Fact]
        public void RuleModel_WithoutDefaultLevel_LevelShouldBeWarning()
        {
            ReportingDescriptor rule = new ReportingDescriptor
            {
                Id = testRuleId,
                Name = testRuleName,
            };

            RuleModel ruleModel = rule.ToRuleModel(testRuleId);
            ruleModel.DefaultFailureLevel.Should().Be(FailureLevel.Warning);
            ruleModel.FailureLevel.Should().Be(FailureLevel.Warning);
        }

        [Fact]
        public void RuleModel_Level_ShouldBeSameAsDefaultLevel()
        {
            ReportingDescriptor rule = new ReportingDescriptor
            {
                Id = testRuleId,
                Name = testRuleName,
                DefaultConfiguration = new ReportingConfiguration
                {
                    Level = FailureLevel.Error,
                }
            };

            RuleModel ruleModel = rule.ToRuleModel(testRuleId);
            ruleModel.DefaultFailureLevel.Should().Be(FailureLevel.Error);
            ruleModel.FailureLevel.Should().Be(FailureLevel.Error);
        }
    }
}

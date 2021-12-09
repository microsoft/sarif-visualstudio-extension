// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Windows.Documents;

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

        public RuleModelTests()
        {
            SarifViewerPackage.IsUnitTesting = true;
        }

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

        [Fact]
        public void RuleModel_PlainTextDescription_ShouldNotHaveInlines()
        {
            string plainText = "Rule description text";
            ReportingDescriptor rule = new ReportingDescriptor
            {
                Id = testRuleId,
                Name = testRuleName,
                DefaultConfiguration = new ReportingConfiguration
                {
                    Level = FailureLevel.Error,
                },
                FullDescription = new MultiformatMessageString
                {
                    Text = plainText
                }
            };

            RuleModel ruleModel = rule.ToRuleModel(testRuleId);
            ruleModel.Description.Should().BeEquivalentTo(plainText);
            ruleModel.DescriptionInlines.Should().BeNullOrEmpty();
            ruleModel.ShowPlainDescription.Should().BeTrue();
        }

        [Fact]
        public void RuleModel_DescriptionWithHyperlink_ShouldHaveInlines()
        {
            string descriptionText = "Rule description text with [hyperlink](https://example.com).";
            ReportingDescriptor rule = new ReportingDescriptor
            {
                Id = testRuleId,
                Name = testRuleName,
                DefaultConfiguration = new ReportingConfiguration
                {
                    Level = FailureLevel.Error,
                },
                FullDescription = new MultiformatMessageString
                {
                    Text = descriptionText
                }
            };

            RuleModel ruleModel = rule.ToRuleModel(testRuleId);
            ruleModel.Description.Should().BeEquivalentTo(descriptionText);
            ruleModel.DescriptionInlines.Should().NotBeNull();
            ruleModel.DescriptionInlines.Count.Should().Be(3);
            ruleModel.DescriptionInlines[1].GetType().Should().Be(typeof(Hyperlink));
            ruleModel.ShowPlainDescription.Should().BeFalse();
        }

        [Fact]
        public void RuleModel_NullDescription_ShouldNotHaveInlines()
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
            ruleModel.Description.Should().BeNull();
            ruleModel.DescriptionInlines.Should().BeNullOrEmpty();
            ruleModel.ShowPlainDescription.Should().BeFalse();
        }

        [Fact]
        public void RuleModel_RuleIdEqualsRuleName_DisplayName_ShouldBeNull()
        {
            ReportingDescriptor rule = new ReportingDescriptor
            {
                Id = testRuleId,
                Name = testRuleId,
                DefaultConfiguration = new ReportingConfiguration
                {
                    Level = FailureLevel.Error,
                }
            };

            RuleModel ruleModel = rule.ToRuleModel(testRuleId);
            ruleModel.DisplayName.Should().BeNull();
        }

        [Fact]
        public void RuleModel_RuleIdDoesNotEqualRuleName_DisplayName_ShouldBeRuleName()
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
            ruleModel.DisplayName.Should().NotBeNull();
            ruleModel.DisplayName.Should().BeEquivalentTo(testRuleName);
        }

        [Fact]
        public void RuleModel_NullRuleId_DisplayNameShouldBeRuleName()
        {
            ReportingDescriptor rule = new ReportingDescriptor
            {
                Id = null,
                Name = testRuleName,
                DefaultConfiguration = new ReportingConfiguration
                {
                    Level = FailureLevel.Error,
                }
            };

            RuleModel ruleModel = rule.ToRuleModel(testRuleId);
            ruleModel.DisplayName.Should().NotBeNull();
            ruleModel.DisplayName.Should().BeEquivalentTo(testRuleName);
        }

        [Fact]
        public void RuleModel_NullRuleName_DisplayNameShouldBeNull()
        {
            ReportingDescriptor rule = new ReportingDescriptor
            {
                Id = testRuleId,
                Name = null,
                DefaultConfiguration = new ReportingConfiguration
                {
                    Level = FailureLevel.Error,
                }
            };

            RuleModel ruleModel = rule.ToRuleModel(testRuleId);
            ruleModel.DisplayName.Should().BeNull();
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Windows;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.VisualStudio;
using Microsoft.Sarif.Viewer.VisualStudio.UnitTests;

using Xunit;

namespace Microsoft.Sarif.Viewer.Models
{
    public class AnalysisStepCollectionTests : SarifViewerPackageUnitTests
    {
        [Fact]
        public void AnalysisStepCollection_ExpandAll()
        {
            var collection = new AnalysisStepCollection();
            collection.Add(this.CreateAnalysisStep());
            collection.ExpandAll();

            collection[0].TopLevelNodes[0].IsExpanded.Should().BeTrue();
            collection[0].TopLevelNodes[0].Children[0].IsExpanded.Should().BeTrue();
            collection[0].TopLevelNodes[0].Children[1].IsExpanded.Should().BeTrue();
            collection[0].TopLevelNodes[0].Children[2].IsExpanded.Should().BeTrue();
            collection[0].TopLevelNodes[1].IsExpanded.Should().BeTrue();
            collection[0].TopLevelNodes[2].IsExpanded.Should().BeTrue();
            collection[0].TopLevelNodes[3].IsExpanded.Should().BeTrue();
            collection[0].TopLevelNodes[3].Children[0].IsExpanded.Should().BeTrue();
            collection[0].TopLevelNodes[3].Children[1].IsExpanded.Should().BeTrue();
            collection[0].TopLevelNodes[4].IsExpanded.Should().BeTrue();
            collection[0].TopLevelNodes[4].Children[0].IsExpanded.Should().BeTrue();
            collection[0].TopLevelNodes[4].Children[1].IsExpanded.Should().BeTrue();
        }

        [Fact]
        public void AnalysisStepCollection_CollapseAll()
        {
            var collection = new AnalysisStepCollection();
            collection.Add(this.CreateAnalysisStep());
            collection.CollapseAll();

            collection[0].TopLevelNodes[0].IsExpanded.Should().BeFalse();
            collection[0].TopLevelNodes[0].Children[0].IsExpanded.Should().BeFalse();
            collection[0].TopLevelNodes[0].Children[1].IsExpanded.Should().BeFalse();
            collection[0].TopLevelNodes[0].Children[2].IsExpanded.Should().BeFalse();
            collection[0].TopLevelNodes[1].IsExpanded.Should().BeFalse();
            collection[0].TopLevelNodes[2].IsExpanded.Should().BeFalse();
            collection[0].TopLevelNodes[3].IsExpanded.Should().BeFalse();
            collection[0].TopLevelNodes[3].Children[0].IsExpanded.Should().BeFalse();
            collection[0].TopLevelNodes[3].Children[1].IsExpanded.Should().BeFalse();
            collection[0].TopLevelNodes[4].IsExpanded.Should().BeFalse();
            collection[0].TopLevelNodes[4].Children[0].IsExpanded.Should().BeFalse();
            collection[0].TopLevelNodes[4].Children[1].IsExpanded.Should().BeFalse();
        }

        [Fact]
        public void AnalysisStepCollection_IntelligentExpand()
        {
            var collection = new AnalysisStepCollection();
            collection.Add(this.CreateAnalysisStep());
            collection.IntelligentExpand();

            collection[0].TopLevelNodes[0].IsExpanded.Should().BeTrue();
            collection[0].TopLevelNodes[0].Children[0].IsExpanded.Should().BeFalse();
            collection[0].TopLevelNodes[0].Children[1].IsExpanded.Should().BeTrue();
            collection[0].TopLevelNodes[0].Children[2].IsExpanded.Should().BeFalse();
            collection[0].TopLevelNodes[1].IsExpanded.Should().BeFalse();
            collection[0].TopLevelNodes[2].IsExpanded.Should().BeTrue();
            collection[0].TopLevelNodes[3].IsExpanded.Should().BeFalse();
            collection[0].TopLevelNodes[3].Children[0].IsExpanded.Should().BeFalse();
            collection[0].TopLevelNodes[3].Children[1].IsExpanded.Should().BeFalse();
            collection[0].TopLevelNodes[4].IsExpanded.Should().BeFalse();
            collection[0].TopLevelNodes[4].Children[0].IsExpanded.Should().BeFalse();
            collection[0].TopLevelNodes[4].Children[1].IsExpanded.Should().BeFalse();
        }

        [Fact]
        public void AnalysisStepCollection_SetVerbosity_Essential()
        {
            var collection = new AnalysisStepCollection();
            collection.Add(this.CreateAnalysisStep());
            collection.Verbosity = 1;

            collection[0].TopLevelNodes[0].Visibility.Should().Be(Visibility.Visible);
            collection[0].TopLevelNodes[0].Children[0].Visibility.Should().Be(Visibility.Collapsed);
            collection[0].TopLevelNodes[0].Children[1].Visibility.Should().Be(Visibility.Visible);
            collection[0].TopLevelNodes[0].Children[2].Visibility.Should().Be(Visibility.Collapsed);
            collection[0].TopLevelNodes[1].Visibility.Should().Be(Visibility.Collapsed);
            collection[0].TopLevelNodes[2].Visibility.Should().Be(Visibility.Visible);
            collection[0].TopLevelNodes[3].Visibility.Should().Be(Visibility.Collapsed);
            collection[0].TopLevelNodes[3].Children[0].Visibility.Should().Be(Visibility.Collapsed);
            collection[0].TopLevelNodes[3].Children[1].Visibility.Should().Be(Visibility.Collapsed);
            collection[0].TopLevelNodes[4].Visibility.Should().Be(Visibility.Collapsed);
            collection[0].TopLevelNodes[4].Visibility.Should().Be(Visibility.Collapsed);
            collection[0].TopLevelNodes[4].Children[1].Visibility.Should().Be(Visibility.Collapsed);
        }

        [Fact]
        public void AnalysisStepCollection_SetVerbosity_Important()
        {
            var collection = new AnalysisStepCollection();
            collection.Add(this.CreateAnalysisStep());
            collection.Verbosity = 100;

            collection[0].TopLevelNodes[0].Visibility.Should().Be(Visibility.Visible);
            collection[0].TopLevelNodes[0].Children[0].Visibility.Should().Be(Visibility.Visible);
            collection[0].TopLevelNodes[0].Children[1].Visibility.Should().Be(Visibility.Visible);
            collection[0].TopLevelNodes[0].Children[2].Visibility.Should().Be(Visibility.Collapsed);
            collection[0].TopLevelNodes[1].Visibility.Should().Be(Visibility.Collapsed);
            collection[0].TopLevelNodes[2].Visibility.Should().Be(Visibility.Visible);
            collection[0].TopLevelNodes[3].Visibility.Should().Be(Visibility.Visible);
            collection[0].TopLevelNodes[3].Children[0].Visibility.Should().Be(Visibility.Visible);
            collection[0].TopLevelNodes[3].Children[1].Visibility.Should().Be(Visibility.Collapsed);
            collection[0].TopLevelNodes[4].Visibility.Should().Be(Visibility.Collapsed);
            collection[0].TopLevelNodes[4].Visibility.Should().Be(Visibility.Collapsed);
            collection[0].TopLevelNodes[4].Children[1].Visibility.Should().Be(Visibility.Collapsed);
        }

        [Fact]
        public void AnalysisStepCollection_SetVerbosity_Unimportant()
        {
            var collection = new AnalysisStepCollection();
            collection.Add(this.CreateAnalysisStep());
            collection.Verbosity = 200;

            collection[0].TopLevelNodes[0].Visibility.Should().Be(Visibility.Visible);
            collection[0].TopLevelNodes[0].Children[0].Visibility.Should().Be(Visibility.Visible);
            collection[0].TopLevelNodes[0].Children[1].Visibility.Should().Be(Visibility.Visible);
            collection[0].TopLevelNodes[0].Children[2].Visibility.Should().Be(Visibility.Visible);
            collection[0].TopLevelNodes[1].Visibility.Should().Be(Visibility.Visible);
            collection[0].TopLevelNodes[2].Visibility.Should().Be(Visibility.Visible);
            collection[0].TopLevelNodes[3].Visibility.Should().Be(Visibility.Visible);
            collection[0].TopLevelNodes[3].Children[0].Visibility.Should().Be(Visibility.Visible);
            collection[0].TopLevelNodes[3].Children[1].Visibility.Should().Be(Visibility.Visible);
            collection[0].TopLevelNodes[4].Visibility.Should().Be(Visibility.Visible);
            collection[0].TopLevelNodes[4].Visibility.Should().Be(Visibility.Visible);
            collection[0].TopLevelNodes[4].Children[1].Visibility.Should().Be(Visibility.Visible);
        }

        private AnalysisStep CreateAnalysisStep()
        {
            CodeFlow codeFlow = SarifUtilities.CreateSingleThreadedCodeFlow(new[]
            {
                new ThreadFlowLocation
                {
                    NestingLevel = 0, // Call
                    Importance = ThreadFlowLocationImportance.Unimportant,
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 1,
                    Importance = ThreadFlowLocationImportance.Important,
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 1,
                    Importance = ThreadFlowLocationImportance.Essential,
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 1, // Return
                    Importance = ThreadFlowLocationImportance.Unimportant,
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 0,
                    Importance = ThreadFlowLocationImportance.Unimportant,
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 0,
                    Importance = ThreadFlowLocationImportance.Essential,
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 0, // Call
                    Importance = ThreadFlowLocationImportance.Unimportant,
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 1,
                    Importance = ThreadFlowLocationImportance.Important,
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 1, // Return
                    Importance = ThreadFlowLocationImportance.Unimportant,
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 0, // Call
                    Importance = ThreadFlowLocationImportance.Unimportant,
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 1,
                    Importance = ThreadFlowLocationImportance.Unimportant,
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 1,
                    Importance = ThreadFlowLocationImportance.Unimportant,
                },
            });

            var analysisStep = new AnalysisStep(CodeFlowToTreeConverter.Convert(codeFlow, run: null, resultId: 0, runIndex: 0));

            return analysisStep;
        }
    }
}

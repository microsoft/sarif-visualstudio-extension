// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class AnalysisStepTraversalTests : SarifViewerPackageUnitTests
    {
        [Fact]
        public void SelectPreviousNextCommandsTest()
        {
            CodeFlow codeFlow = SarifUtilities.CreateSingleThreadedCodeFlow(new[]
            {
                new ThreadFlowLocation
                {
                    NestingLevel = 0,
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 1,
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 1,
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 1,
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 0,
                },
                new ThreadFlowLocation
                {
                    NestingLevel = 0,
                },
            });

            var analysisStep = new AnalysisStep(CodeFlowToTreeConverter.Convert(codeFlow, run: null, resultId: 0, runIndex: 0));

            analysisStep.FindPrevious().Should().Be(null);
            analysisStep.FindNext().Should().Be(null);

            analysisStep.SelectedItem = analysisStep.TopLevelNodes[0];
            analysisStep.FindPrevious().Should().Be(analysisStep.TopLevelNodes[0]);
            analysisStep.FindNext().Should().Be(analysisStep.TopLevelNodes[0].Children[0]);

            analysisStep.SelectedItem = analysisStep.TopLevelNodes[0].Children[0];
            analysisStep.FindPrevious().Should().Be(analysisStep.TopLevelNodes[0]);
            analysisStep.FindNext().Should().Be(analysisStep.TopLevelNodes[0].Children[1]);

            analysisStep.SelectedItem = analysisStep.TopLevelNodes[0].Children[2];
            analysisStep.FindPrevious().Should().Be(analysisStep.TopLevelNodes[0].Children[1]);
            analysisStep.FindNext().Should().Be(analysisStep.TopLevelNodes[1]);

            analysisStep.SelectedItem = analysisStep.TopLevelNodes[1];
            analysisStep.FindPrevious().Should().Be(analysisStep.TopLevelNodes[0].Children[2]);
            analysisStep.FindNext().Should().Be(analysisStep.TopLevelNodes[2]);

            analysisStep.SelectedItem = analysisStep.TopLevelNodes[2];
            analysisStep.FindPrevious().Should().Be(analysisStep.TopLevelNodes[1]);
            analysisStep.FindNext().Should().Be(analysisStep.TopLevelNodes[2]);
        }

        [Fact]
        public void SelectPreviousNextCommandsCallNoChildrenTest()
        {
            CodeFlow codeFlow = SarifUtilities.CreateSingleThreadedCodeFlow(new[]
            {
                new ThreadFlowLocation
                {
                    NestingLevel = 0,
                },
            });

            var analysisStep = new AnalysisStep(CodeFlowToTreeConverter.Convert(codeFlow, run: null, resultId: 0, runIndex: 0));

            analysisStep.SelectedItem = analysisStep.TopLevelNodes[0];
            analysisStep.FindPrevious().Should().Be(analysisStep.TopLevelNodes[0]);
            analysisStep.FindNext().Should().Be(analysisStep.TopLevelNodes[0]);
        }
    }
}

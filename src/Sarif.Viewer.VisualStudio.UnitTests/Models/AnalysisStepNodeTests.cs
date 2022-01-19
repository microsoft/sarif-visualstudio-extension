// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Windows;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;

using Xunit;

namespace Microsoft.Sarif.Viewer.Models
{
    public class AnalysisStepNodeTests
    {
        private static readonly Random s_random = new Random();

        [Fact]
        public void AnalysisStepNode_DefaultHighlightColor()
        {
            var analysisStepNode = new AnalysisStepNode(resultId: 0, runIndex: 0)
            {
                Location = new ThreadFlowLocation(),
            };

            analysisStepNode.Location.Importance = ThreadFlowLocationImportance.Essential;
            analysisStepNode.DefaultSourceHighlightColor.Should().Be("CodeAnalysisKeyEventSelection");

            analysisStepNode.Location.Importance = ThreadFlowLocationImportance.Important;
            analysisStepNode.DefaultSourceHighlightColor.Should().Be("CodeAnalysisLineTraceSelection");

            analysisStepNode.Location.Importance = ThreadFlowLocationImportance.Unimportant;
            analysisStepNode.DefaultSourceHighlightColor.Should().Be("CodeAnalysisLineTraceSelection");
        }

        [Fact]
        public void AnalysisStepNode_SelectedHighlightColor()
        {
            var analysisStepNode = new AnalysisStepNode(resultId: 0, runIndex: 0)
            {
                Location = new ThreadFlowLocation(),
            };

            analysisStepNode.Location.Importance = ThreadFlowLocationImportance.Essential;
            analysisStepNode.SelectedSourceHighlightColor.Should().Be("CodeAnalysisCurrentStatementSelection");
        }

        [Fact]
        public void AnalysisStepNode_TextMargin()
        {
            var analysisStepNode = new AnalysisStepNode(resultId: 0, runIndex: 0);

            analysisStepNode.TextMargin.Should().NotBeNull();
            analysisStepNode.TextMargin.Left.Should().Be(0);
            analysisStepNode.TextMargin.Right.Should().Be(0);
            analysisStepNode.TextMargin.Top.Should().Be(0);
            analysisStepNode.TextMargin.Bottom.Should().Be(0);

            analysisStepNode.NestingLevel = s_random.Next(0, 30);

            analysisStepNode.TextMargin.Left.Should().Be(AnalysisStepNode.IndentWidth * analysisStepNode.NestingLevel);
            analysisStepNode.TextMargin.Right.Should().Be(0);
            analysisStepNode.TextMargin.Top.Should().Be(0);
            analysisStepNode.TextMargin.Bottom.Should().Be(0);
        }
    }
}

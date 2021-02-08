// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;

using Xunit;

namespace Microsoft.Sarif.Viewer.Models
{
    public class AnalysisStepNodeTests
    {
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
    }
}

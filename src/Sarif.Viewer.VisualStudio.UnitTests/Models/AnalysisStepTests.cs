// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Windows;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.VisualStudio;

using Xunit;

namespace Microsoft.Sarif.Viewer.Models
{
    public class AnalysisStepTests
    {
        private const string Expected = "expected";

        [Fact]
        public void AnalysisStep_TryGetIndexInAnalysisStepNodeList_NullList()
        {
            List<AnalysisStepNode> list = null;

            var node = new AnalysisStepNode(resultId: 0, runIndex: 0);

            int index;
            bool result = AnalysisStep.TryGetIndexInAnalysisStepNodeList(list, node, out index);

            result.Should().BeFalse();
        }

        [Fact]
        public void AnalysisStep_TryGetIndexInAnalysisStepNodeList_NullNode()
        {
            var list = new List<AnalysisStepNode>();
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));

            int index;
            bool result = AnalysisStep.TryGetIndexInAnalysisStepNodeList(list, null, out index);

            result.Should().BeFalse();
        }

        [Fact]
        public void AnalysisStep_TryGetIndexInAnalysisStepNodeList_FirstNode()
        {
            var list = new List<AnalysisStepNode>();
            var target = new AnalysisStepNode(resultId: 0, runIndex: 0);
            list.Add(target);
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));

            int index;
            bool result = AnalysisStep.TryGetIndexInAnalysisStepNodeList(list, target, out index);

            result.Should().BeTrue();
            index.Should().Be(0);
        }

        [Fact]
        public void AnalysisStep_TryGetIndexInAnalysisStepNodeList_LastNode()
        {
            var list = new List<AnalysisStepNode>();
            var target = new AnalysisStepNode(resultId: 0, runIndex: 0);
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));
            list.Add(target);

            int index;
            bool result = AnalysisStep.TryGetIndexInAnalysisStepNodeList(list, target, out index);

            result.Should().BeTrue();
            index.Should().Be(2);
        }

        [Fact]
        public void AnalysisStep_TryGetIndexInAnalysisStepNodeList_MiddleNode()
        {
            var list = new List<AnalysisStepNode>();
            var target = new AnalysisStepNode(resultId: 0, runIndex: 0);
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));
            list.Add(target);
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));

            int index;
            bool result = AnalysisStep.TryGetIndexInAnalysisStepNodeList(list, target, out index);

            result.Should().BeTrue();
            index.Should().Be(1);
        }

        [Fact]
        public void AnalysisStep_TryGetIndexInAnalysisStepNodeList_DoesNotExistNode()
        {
            var list = new List<AnalysisStepNode>();
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));

            int index;
            bool result = AnalysisStep.TryGetIndexInAnalysisStepNodeList(list, new AnalysisStepNode(resultId: 0, runIndex: 0), out index);

            result.Should().BeFalse();
        }

        [Fact]
        public void AnalysisStep_TryGetNextSibling_NullList()
        {
            List<AnalysisStepNode> list = null;

            var node = new AnalysisStepNode(resultId: 0, runIndex: 0);

            AnalysisStepNode resultNode;
            bool result = AnalysisStep.TryGetNextSibling(list, node, out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void AnalysisStep_TryGetNextSibling_NullNode()
        {
            var list = new List<AnalysisStepNode>();
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));

            AnalysisStepNode resultNode;
            bool result = AnalysisStep.TryGetNextSibling(list, null, out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void AnalysisStep_TryGetNextSibling_FirstNode()
        {
            var list = new List<AnalysisStepNode>();
            var target = new AnalysisStepNode(resultId: 0, runIndex: 0);
            list.Add(target);
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { FilePath = Expected });
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));

            AnalysisStepNode resultNode;
            bool result = AnalysisStep.TryGetNextSibling(list, target, out resultNode);

            result.Should().BeTrue();
            resultNode.FilePath.Should().Be(Expected);
        }

        [Fact]
        public void AnalysisStep_TryGetNextSibling_LastNode()
        {
            var list = new List<AnalysisStepNode>();
            var target = new AnalysisStepNode(resultId: 0, runIndex: 0);
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));
            list.Add(target);

            AnalysisStepNode resultNode;
            bool result = AnalysisStep.TryGetNextSibling(list, target, out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void AnalysisStep_TryGetNextSibling_MiddleNode()
        {
            var list = new List<AnalysisStepNode>();
            var target = new AnalysisStepNode(resultId: 0, runIndex: 0);
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));
            list.Add(target);
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { FilePath = Expected });

            AnalysisStepNode resultNode;
            bool result = AnalysisStep.TryGetNextSibling(list, target, out resultNode);

            result.Should().BeTrue();
            resultNode.FilePath.Should().Be(Expected);
        }

        [Fact]
        public void AnalysisStep_TryGetNextSibling_SkipNonVisibleNodes()
        {
            var list = new List<AnalysisStepNode>();
            var target = new AnalysisStepNode(resultId: 0, runIndex: 0);
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));
            list.Add(target);
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Collapsed });
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Hidden });
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { FilePath = Expected });

            AnalysisStepNode resultNode;
            bool result = AnalysisStep.TryGetNextSibling(list, target, out resultNode);

            result.Should().BeTrue();
            resultNode.FilePath.Should().Be(Expected);
        }

        [Fact]
        public void AnalysisStep_TryGetNextSibling_NoVisibleNodes()
        {
            var list = new List<AnalysisStepNode>();
            var target = new AnalysisStepNode(resultId: 0, runIndex: 0);
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));
            list.Add(target);
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Collapsed });
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Hidden });

            AnalysisStepNode resultNode;
            bool result = AnalysisStep.TryGetNextSibling(list, target, out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void AnalysisStep_TryGetNextSibling_DoesNotExistNode()
        {
            var list = new List<AnalysisStepNode>();
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Collapsed });
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Hidden });

            AnalysisStepNode resultNode;
            bool result = AnalysisStep.TryGetNextSibling(list, new AnalysisStepNode(resultId: 0, runIndex: 0), out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void AnalysisStep_TryGetPreviousSibling_NullList()
        {
            List<AnalysisStepNode> list = null;

            var node = new AnalysisStepNode(resultId: 0, runIndex: 0);

            AnalysisStepNode resultNode;
            bool result = AnalysisStep.TryGetPreviousSibling(list, node, out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void AnalysisStep_TryGetPreviousSibling_NullNode()
        {
            var list = new List<AnalysisStepNode>();
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));

            AnalysisStepNode resultNode;
            bool result = AnalysisStep.TryGetPreviousSibling(list, null, out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void AnalysisStep_TryGetPreviousSibling_FirstNode()
        {
            var list = new List<AnalysisStepNode>();
            var target = new AnalysisStepNode(resultId: 0, runIndex: 0);
            list.Add(target);
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { FilePath = Expected });
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));

            AnalysisStepNode resultNode;
            bool result = AnalysisStep.TryGetPreviousSibling(list, target, out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void AnalysisStep_TryGetPreviousSibling_LastNode()
        {
            var list = new List<AnalysisStepNode>();
            var target = new AnalysisStepNode(resultId: 0, runIndex: 0);
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { FilePath = Expected });
            list.Add(target);

            AnalysisStepNode resultNode;
            bool result = AnalysisStep.TryGetPreviousSibling(list, target, out resultNode);

            result.Should().BeTrue();
            resultNode.FilePath.Should().Be(Expected);
        }

        [Fact]
        public void AnalysisStep_TryGetPreviousSibling_MiddleNode()
        {
            var list = new List<AnalysisStepNode>();
            var target = new AnalysisStepNode(resultId: 0, runIndex: 0);
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { FilePath = Expected });
            list.Add(target);
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));

            AnalysisStepNode resultNode;
            bool result = AnalysisStep.TryGetPreviousSibling(list, target, out resultNode);

            result.Should().BeTrue();
            resultNode.FilePath.Should().Be(Expected);
        }

        [Fact]
        public void AnalysisStep_TryGetPreviousSibling_SkipNonVisibleNodes()
        {
            var list = new List<AnalysisStepNode>();
            var target = new AnalysisStepNode(resultId: 0, runIndex: 0);
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { FilePath = Expected });
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Collapsed });
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Hidden });
            list.Add(target);
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));

            AnalysisStepNode resultNode;
            bool result = AnalysisStep.TryGetPreviousSibling(list, target, out resultNode);

            result.Should().BeTrue();
            resultNode.FilePath.Should().Be(Expected);
        }

        [Fact]
        public void AnalysisStep_TryGetPreviousSibling_NoVisibleNodes()
        {
            var list = new List<AnalysisStepNode>();
            var target = new AnalysisStepNode(resultId: 0, runIndex: 0);
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Collapsed });
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Hidden });
            list.Add(target);
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));

            AnalysisStepNode resultNode;
            bool result = AnalysisStep.TryGetPreviousSibling(list, target, out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void AnalysisStep_TryGetPreviousSibling_DoesNotExistNode()
        {
            var list = new List<AnalysisStepNode>();
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Collapsed });
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Hidden });

            AnalysisStepNode resultNode;
            bool result = AnalysisStep.TryGetPreviousSibling(list, new AnalysisStepNode(resultId: 0, runIndex: 0), out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void AnalysisStep_TryGetFirstItem_NullList()
        {
            List<AnalysisStepNode> list = null;

            AnalysisStepNode resultNode;
            bool result = AnalysisStep.TryGetFirstItem(list, out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void AnalysisStep_TryGetFirstItem_FirstNode()
        {
            var list = new List<AnalysisStepNode>();
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { FilePath = Expected });
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));

            AnalysisStepNode resultNode;
            bool result = AnalysisStep.TryGetFirstItem(list, out resultNode);

            result.Should().BeTrue();
            resultNode.FilePath.Should().Be(Expected);
        }

        [Fact]
        public void AnalysisStep_TryGetFirstItem_SkipNonVisibleNodes()
        {
            var list = new List<AnalysisStepNode>();
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Collapsed });
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Hidden });
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { FilePath = Expected });
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));

            AnalysisStepNode resultNode;
            bool result = AnalysisStep.TryGetFirstItem(list, out resultNode);

            result.Should().BeTrue();
            resultNode.FilePath.Should().Be(Expected);
        }

        [Fact]
        public void AnalysisStep_TryGetFirstItem_NoVisibleNodes()
        {
            var list = new List<AnalysisStepNode>();
            var target = new AnalysisStepNode(resultId: 0, runIndex: 0);
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Collapsed });
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Hidden });

            AnalysisStepNode resultNode;
            bool result = AnalysisStep.TryGetFirstItem(list, out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void AnalysisStep_TryGetLastItem_NullList()
        {
            List<AnalysisStepNode> list = null;

            AnalysisStepNode resultNode;
            bool result = AnalysisStep.TryGetLastItem(list, out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void AnalysisStep_TryGetLastItem_LastNode()
        {
            var list = new List<AnalysisStepNode>();
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { FilePath = Expected });

            AnalysisStepNode resultNode;
            bool result = AnalysisStep.TryGetLastItem(list, out resultNode);

            result.Should().BeTrue();
            resultNode.FilePath.Should().Be(Expected);
        }

        [Fact]
        public void AnalysisStep_TryGetLastItem_SkipNonVisibleNodes()
        {
            var list = new List<AnalysisStepNode>();
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0));
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { FilePath = Expected });
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Collapsed });
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Hidden });

            AnalysisStepNode resultNode;
            bool result = AnalysisStep.TryGetLastItem(list, out resultNode);

            result.Should().BeTrue();
            resultNode.FilePath.Should().Be(Expected);
        }

        [Fact]
        public void AnalysisStep_TryGetLastItem_NoVisibleNodes()
        {
            var list = new List<AnalysisStepNode>();
            var target = new AnalysisStepNode(resultId: 0, runIndex: 0);
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Collapsed });
            list.Add(new AnalysisStepNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Hidden });

            AnalysisStepNode resultNode;
            bool result = AnalysisStep.TryGetLastItem(list, out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void AnalysisStep_ExpandAll_NoNodes()
        {
            var tree = new AnalysisStep(new List<AnalysisStepNode>());
            tree.ExpandAll();
        }

        [Fact]
        public void AnalysisStep_ExpandAll()
        {
            AnalysisStep tree = this.CreateAnalysisStep();
            tree.ExpandAll();

            tree.TopLevelNodes[0].IsExpanded.Should().BeTrue();
            tree.TopLevelNodes[0].Children[0].IsExpanded.Should().BeTrue();
            tree.TopLevelNodes[0].Children[1].IsExpanded.Should().BeTrue();
            tree.TopLevelNodes[0].Children[2].IsExpanded.Should().BeTrue();
            tree.TopLevelNodes[1].IsExpanded.Should().BeTrue();
            tree.TopLevelNodes[2].IsExpanded.Should().BeTrue();
        }

        [Fact]
        public void AnalysisStep_CollapseAll()
        {
            AnalysisStep tree = this.CreateAnalysisStep();
            tree.CollapseAll();

            tree.TopLevelNodes[0].IsExpanded.Should().BeFalse();
            tree.TopLevelNodes[0].Children[0].IsExpanded.Should().BeFalse();
            tree.TopLevelNodes[0].Children[1].IsExpanded.Should().BeFalse();
            tree.TopLevelNodes[0].Children[2].IsExpanded.Should().BeFalse();
            tree.TopLevelNodes[1].IsExpanded.Should().BeFalse();
            tree.TopLevelNodes[2].IsExpanded.Should().BeFalse();
        }

        [Fact]
        public void AnalysisStep_IntelligentExpand()
        {
            AnalysisStep tree = this.CreateAnalysisStep();
            tree.IntelligentExpand();

            tree.TopLevelNodes[0].IsExpanded.Should().BeTrue();
            tree.TopLevelNodes[0].Children[0].IsExpanded.Should().BeFalse();
            tree.TopLevelNodes[0].Children[1].IsExpanded.Should().BeTrue();
            tree.TopLevelNodes[0].Children[2].IsExpanded.Should().BeFalse();
            tree.TopLevelNodes[1].IsExpanded.Should().BeFalse();
            tree.TopLevelNodes[2].IsExpanded.Should().BeTrue();
        }

        [Fact]
        public void AnalysisStep_SetVerbosity_Essential()
        {
            AnalysisStep tree = this.CreateAnalysisStep();
            tree.SetVerbosity(ThreadFlowLocationImportance.Essential);

            tree.TopLevelNodes[0].Visibility.Should().Be(Visibility.Visible);
            tree.TopLevelNodes[0].Children[0].Visibility.Should().Be(Visibility.Collapsed);
            tree.TopLevelNodes[0].Children[1].Visibility.Should().Be(Visibility.Visible);
            tree.TopLevelNodes[0].Children[2].Visibility.Should().Be(Visibility.Collapsed);
            tree.TopLevelNodes[1].Visibility.Should().Be(Visibility.Collapsed);
            tree.TopLevelNodes[2].Visibility.Should().Be(Visibility.Visible);
        }

        [Fact]
        public void AnalysisStep_SetVerbosity_Important()
        {
            AnalysisStep tree = this.CreateAnalysisStep();
            tree.SetVerbosity(ThreadFlowLocationImportance.Important);

            tree.TopLevelNodes[0].Visibility.Should().Be(Visibility.Visible);
            tree.TopLevelNodes[0].Children[0].Visibility.Should().Be(Visibility.Visible);
            tree.TopLevelNodes[0].Children[1].Visibility.Should().Be(Visibility.Visible);
            tree.TopLevelNodes[0].Children[2].Visibility.Should().Be(Visibility.Collapsed);
            tree.TopLevelNodes[1].Visibility.Should().Be(Visibility.Collapsed);
            tree.TopLevelNodes[2].Visibility.Should().Be(Visibility.Visible);
        }

        [Fact]
        public void AnalysisStep_SetVerbosity_Unimportant()
        {
            AnalysisStep tree = this.CreateAnalysisStep();
            tree.SetVerbosity(ThreadFlowLocationImportance.Unimportant);

            tree.TopLevelNodes[0].Visibility.Should().Be(Visibility.Visible);
            tree.TopLevelNodes[0].Children[0].Visibility.Should().Be(Visibility.Visible);
            tree.TopLevelNodes[0].Children[1].Visibility.Should().Be(Visibility.Visible);
            tree.TopLevelNodes[0].Children[2].Visibility.Should().Be(Visibility.Visible);
            tree.TopLevelNodes[1].Visibility.Should().Be(Visibility.Visible);
            tree.TopLevelNodes[2].Visibility.Should().Be(Visibility.Visible);
        }

        private AnalysisStep CreateAnalysisStep()
        {
            CodeFlow codeFlow = SarifUtilities.CreateSingleThreadedCodeFlow(new[]
            {
                new ThreadFlowLocation
                {
                    NestingLevel = 0,
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
                    NestingLevel = 1,
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
            });

            var analysisStep = new AnalysisStep(CodeFlowToTreeConverter.Convert(codeFlow, run: null, resultId: 0, runIndex: 0));

            return analysisStep;
        }
    }
}

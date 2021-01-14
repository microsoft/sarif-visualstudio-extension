﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Windows;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.VisualStudio;

using Xunit;

namespace Microsoft.Sarif.Viewer.Models
{
    public class CallTreeTests
    {
        private const string Expected = "expected";

        [Fact]
        public void CallTree_TryGetIndexInCallTreeNodeList_NullList()
        {
            List<CallTreeNode> list = null;

            var node = new CallTreeNode(resultId: 0, runIndex: 0);

            int index;
            bool result = CallTree.TryGetIndexInCallTreeNodeList(list, node, out index);

            result.Should().BeFalse();
        }

        [Fact]
        public void CallTree_TryGetIndexInCallTreeNodeList_NullNode()
        {
            var list = new List<CallTreeNode>();
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));

            int index;
            bool result = CallTree.TryGetIndexInCallTreeNodeList(list, null, out index);

            result.Should().BeFalse();
        }

        [Fact]
        public void CallTree_TryGetIndexInCallTreeNodeList_FirstNode()
        {
            var list = new List<CallTreeNode>();
            var target = new CallTreeNode(resultId: 0, runIndex: 0);
            list.Add(target);
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));

            int index;
            bool result = CallTree.TryGetIndexInCallTreeNodeList(list, target, out index);

            result.Should().BeTrue();
            index.Should().Be(0);
        }

        [Fact]
        public void CallTree_TryGetIndexInCallTreeNodeList_LastNode()
        {
            var list = new List<CallTreeNode>();
            var target = new CallTreeNode(resultId: 0, runIndex: 0);
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));
            list.Add(target);

            int index;
            bool result = CallTree.TryGetIndexInCallTreeNodeList(list, target, out index);

            result.Should().BeTrue();
            index.Should().Be(2);
        }

        [Fact]
        public void CallTree_TryGetIndexInCallTreeNodeList_MiddleNode()
        {
            var list = new List<CallTreeNode>();
            var target = new CallTreeNode(resultId: 0, runIndex: 0);
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));
            list.Add(target);
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));

            int index;
            bool result = CallTree.TryGetIndexInCallTreeNodeList(list, target, out index);

            result.Should().BeTrue();
            index.Should().Be(1);
        }

        [Fact]
        public void CallTree_TryGetIndexInCallTreeNodeList_DoesNotExistNode()
        {
            var list = new List<CallTreeNode>();
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));

            int index;
            bool result = CallTree.TryGetIndexInCallTreeNodeList(list, new CallTreeNode(resultId: 0, runIndex: 0), out index);

            result.Should().BeFalse();
        }

        [Fact]
        public void CallTree_TryGetNextSibling_NullList()
        {
            List<CallTreeNode> list = null;

            var node = new CallTreeNode(resultId: 0, runIndex: 0);

            CallTreeNode resultNode;
            bool result = CallTree.TryGetNextSibling(list, node, out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void CallTree_TryGetNextSibling_NullNode()
        {
            var list = new List<CallTreeNode>();
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));

            CallTreeNode resultNode;
            bool result = CallTree.TryGetNextSibling(list, null, out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void CallTree_TryGetNextSibling_FirstNode()
        {
            var list = new List<CallTreeNode>();
            var target = new CallTreeNode(resultId: 0, runIndex: 0);
            list.Add(target);
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { FilePath = Expected });
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));

            CallTreeNode resultNode;
            bool result = CallTree.TryGetNextSibling(list, target, out resultNode);

            result.Should().BeTrue();
            resultNode.FilePath.Should().Be(Expected);
        }

        [Fact]
        public void CallTree_TryGetNextSibling_LastNode()
        {
            var list = new List<CallTreeNode>();
            var target = new CallTreeNode(resultId: 0, runIndex: 0);
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));
            list.Add(target);

            CallTreeNode resultNode;
            bool result = CallTree.TryGetNextSibling(list, target, out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void CallTree_TryGetNextSibling_MiddleNode()
        {
            var list = new List<CallTreeNode>();
            var target = new CallTreeNode(resultId: 0, runIndex: 0);
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));
            list.Add(target);
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { FilePath = Expected });

            CallTreeNode resultNode;
            bool result = CallTree.TryGetNextSibling(list, target, out resultNode);

            result.Should().BeTrue();
            resultNode.FilePath.Should().Be(Expected);
        }

        [Fact]
        public void CallTree_TryGetNextSibling_SkipNonVisibleNodes()
        {
            var list = new List<CallTreeNode>();
            var target = new CallTreeNode(resultId: 0, runIndex: 0);
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));
            list.Add(target);
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Collapsed });
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Hidden });
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { FilePath = Expected });

            CallTreeNode resultNode;
            bool result = CallTree.TryGetNextSibling(list, target, out resultNode);

            result.Should().BeTrue();
            resultNode.FilePath.Should().Be(Expected);
        }

        [Fact]
        public void CallTree_TryGetNextSibling_NoVisibleNodes()
        {
            var list = new List<CallTreeNode>();
            var target = new CallTreeNode(resultId: 0, runIndex: 0);
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));
            list.Add(target);
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Collapsed });
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Hidden });

            CallTreeNode resultNode;
            bool result = CallTree.TryGetNextSibling(list, target, out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void CallTree_TryGetNextSibling_DoesNotExistNode()
        {
            var list = new List<CallTreeNode>();
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Collapsed });
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Hidden });

            CallTreeNode resultNode;
            bool result = CallTree.TryGetNextSibling(list, new CallTreeNode(resultId: 0, runIndex: 0), out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void CallTree_TryGetPreviousSibling_NullList()
        {
            List<CallTreeNode> list = null;

            var node = new CallTreeNode(resultId: 0, runIndex: 0);

            CallTreeNode resultNode;
            bool result = CallTree.TryGetPreviousSibling(list, node, out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void CallTree_TryGetPreviousSibling_NullNode()
        {
            var list = new List<CallTreeNode>();
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));

            CallTreeNode resultNode;
            bool result = CallTree.TryGetPreviousSibling(list, null, out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void CallTree_TryGetPreviousSibling_FirstNode()
        {
            var list = new List<CallTreeNode>();
            var target = new CallTreeNode(resultId: 0, runIndex: 0);
            list.Add(target);
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { FilePath = Expected });
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));

            CallTreeNode resultNode;
            bool result = CallTree.TryGetPreviousSibling(list, target, out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void CallTree_TryGetPreviousSibling_LastNode()
        {
            var list = new List<CallTreeNode>();
            var target = new CallTreeNode(resultId: 0, runIndex: 0);
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { FilePath = Expected });
            list.Add(target);

            CallTreeNode resultNode;
            bool result = CallTree.TryGetPreviousSibling(list, target, out resultNode);

            result.Should().BeTrue();
            resultNode.FilePath.Should().Be(Expected);
        }

        [Fact]
        public void CallTree_TryGetPreviousSibling_MiddleNode()
        {
            var list = new List<CallTreeNode>();
            var target = new CallTreeNode(resultId: 0, runIndex: 0);
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { FilePath = Expected });
            list.Add(target);
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));

            CallTreeNode resultNode;
            bool result = CallTree.TryGetPreviousSibling(list, target, out resultNode);

            result.Should().BeTrue();
            resultNode.FilePath.Should().Be(Expected);
        }

        [Fact]
        public void CallTree_TryGetPreviousSibling_SkipNonVisibleNodes()
        {
            var list = new List<CallTreeNode>();
            var target = new CallTreeNode(resultId: 0, runIndex: 0);
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { FilePath = Expected });
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Collapsed });
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Hidden });
            list.Add(target);
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));

            CallTreeNode resultNode;
            bool result = CallTree.TryGetPreviousSibling(list, target, out resultNode);

            result.Should().BeTrue();
            resultNode.FilePath.Should().Be(Expected);
        }

        [Fact]
        public void CallTree_TryGetPreviousSibling_NoVisibleNodes()
        {
            var list = new List<CallTreeNode>();
            var target = new CallTreeNode(resultId: 0, runIndex: 0);
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Collapsed });
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Hidden });
            list.Add(target);
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));

            CallTreeNode resultNode;
            bool result = CallTree.TryGetPreviousSibling(list, target, out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void CallTree_TryGetPreviousSibling_DoesNotExistNode()
        {
            var list = new List<CallTreeNode>();
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Collapsed });
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Hidden });

            CallTreeNode resultNode;
            bool result = CallTree.TryGetPreviousSibling(list, new CallTreeNode(resultId: 0, runIndex: 0), out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void CallTree_TryGetFirstItem_NullList()
        {
            List<CallTreeNode> list = null;

            CallTreeNode resultNode;
            bool result = CallTree.TryGetFirstItem(list, out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void CallTree_TryGetFirstItem_FirstNode()
        {
            var list = new List<CallTreeNode>();
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { FilePath = Expected });
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));

            CallTreeNode resultNode;
            bool result = CallTree.TryGetFirstItem(list, out resultNode);

            result.Should().BeTrue();
            resultNode.FilePath.Should().Be(Expected);
        }

        [Fact]
        public void CallTree_TryGetFirstItem_SkipNonVisibleNodes()
        {
            var list = new List<CallTreeNode>();
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Collapsed });
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Hidden });
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { FilePath = Expected });
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));

            CallTreeNode resultNode;
            bool result = CallTree.TryGetFirstItem(list, out resultNode);

            result.Should().BeTrue();
            resultNode.FilePath.Should().Be(Expected);
        }

        [Fact]
        public void CallTree_TryGetFirstItem_NoVisibleNodes()
        {
            var list = new List<CallTreeNode>();
            var target = new CallTreeNode(resultId: 0, runIndex: 0);
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Collapsed });
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Hidden });

            CallTreeNode resultNode;
            bool result = CallTree.TryGetFirstItem(list, out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void CallTree_TryGetLastItem_NullList()
        {
            List<CallTreeNode> list = null;

            CallTreeNode resultNode;
            bool result = CallTree.TryGetLastItem(list, out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void CallTree_TryGetLastItem_LastNode()
        {
            var list = new List<CallTreeNode>();
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { FilePath = Expected });

            CallTreeNode resultNode;
            bool result = CallTree.TryGetLastItem(list, out resultNode);

            result.Should().BeTrue();
            resultNode.FilePath.Should().Be(Expected);
        }

        [Fact]
        public void CallTree_TryGetLastItem_SkipNonVisibleNodes()
        {
            var list = new List<CallTreeNode>();
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0));
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { FilePath = Expected });
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Collapsed });
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Hidden });

            CallTreeNode resultNode;
            bool result = CallTree.TryGetLastItem(list, out resultNode);

            result.Should().BeTrue();
            resultNode.FilePath.Should().Be(Expected);
        }

        [Fact]
        public void CallTree_TryGetLastItem_NoVisibleNodes()
        {
            var list = new List<CallTreeNode>();
            var target = new CallTreeNode(resultId: 0, runIndex: 0);
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Collapsed });
            list.Add(new CallTreeNode(resultId: 0, runIndex: 0) { Visibility = Visibility.Hidden });

            CallTreeNode resultNode;
            bool result = CallTree.TryGetLastItem(list, out resultNode);

            result.Should().BeFalse();
        }

        [Fact]
        public void CallTree_ExpandAll_NoNodes()
        {
            var tree = new CallTree(new List<CallTreeNode>());
            tree.ExpandAll();
        }

        [Fact]
        public void CallTree_ExpandAll()
        {
            CallTree tree = this.CreateCallTree();
            tree.ExpandAll();

            tree.TopLevelNodes[0].IsExpanded.Should().BeTrue();
            tree.TopLevelNodes[0].Children[0].IsExpanded.Should().BeTrue();
            tree.TopLevelNodes[0].Children[1].IsExpanded.Should().BeTrue();
            tree.TopLevelNodes[0].Children[2].IsExpanded.Should().BeTrue();
            tree.TopLevelNodes[1].IsExpanded.Should().BeTrue();
            tree.TopLevelNodes[2].IsExpanded.Should().BeTrue();
        }

        [Fact]
        public void CallTree_CollapseAll()
        {
            CallTree tree = this.CreateCallTree();
            tree.CollapseAll();

            tree.TopLevelNodes[0].IsExpanded.Should().BeFalse();
            tree.TopLevelNodes[0].Children[0].IsExpanded.Should().BeFalse();
            tree.TopLevelNodes[0].Children[1].IsExpanded.Should().BeFalse();
            tree.TopLevelNodes[0].Children[2].IsExpanded.Should().BeFalse();
            tree.TopLevelNodes[1].IsExpanded.Should().BeFalse();
            tree.TopLevelNodes[2].IsExpanded.Should().BeFalse();
        }

        [Fact]
        public void CallTree_IntelligentExpand()
        {
            CallTree tree = this.CreateCallTree();
            tree.IntelligentExpand();

            tree.TopLevelNodes[0].IsExpanded.Should().BeTrue();
            tree.TopLevelNodes[0].Children[0].IsExpanded.Should().BeFalse();
            tree.TopLevelNodes[0].Children[1].IsExpanded.Should().BeTrue();
            tree.TopLevelNodes[0].Children[2].IsExpanded.Should().BeFalse();
            tree.TopLevelNodes[1].IsExpanded.Should().BeFalse();
            tree.TopLevelNodes[2].IsExpanded.Should().BeTrue();
        }

        [Fact]
        public void CallTree_SetVerbosity_Essential()
        {
            CallTree tree = this.CreateCallTree();
            tree.SetVerbosity(ThreadFlowLocationImportance.Essential);

            tree.TopLevelNodes[0].Visibility.Should().Be(Visibility.Visible);
            tree.TopLevelNodes[0].Children[0].Visibility.Should().Be(Visibility.Collapsed);
            tree.TopLevelNodes[0].Children[1].Visibility.Should().Be(Visibility.Visible);
            tree.TopLevelNodes[0].Children[2].Visibility.Should().Be(Visibility.Collapsed);
            tree.TopLevelNodes[1].Visibility.Should().Be(Visibility.Collapsed);
            tree.TopLevelNodes[2].Visibility.Should().Be(Visibility.Visible);
        }

        [Fact]
        public void CallTree_SetVerbosity_Important()
        {
            CallTree tree = this.CreateCallTree();
            tree.SetVerbosity(ThreadFlowLocationImportance.Important);

            tree.TopLevelNodes[0].Visibility.Should().Be(Visibility.Visible);
            tree.TopLevelNodes[0].Children[0].Visibility.Should().Be(Visibility.Visible);
            tree.TopLevelNodes[0].Children[1].Visibility.Should().Be(Visibility.Visible);
            tree.TopLevelNodes[0].Children[2].Visibility.Should().Be(Visibility.Collapsed);
            tree.TopLevelNodes[1].Visibility.Should().Be(Visibility.Collapsed);
            tree.TopLevelNodes[2].Visibility.Should().Be(Visibility.Visible);
        }

        [Fact]
        public void CallTree_SetVerbosity_Unimportant()
        {
            CallTree tree = this.CreateCallTree();
            tree.SetVerbosity(ThreadFlowLocationImportance.Unimportant);

            tree.TopLevelNodes[0].Visibility.Should().Be(Visibility.Visible);
            tree.TopLevelNodes[0].Children[0].Visibility.Should().Be(Visibility.Visible);
            tree.TopLevelNodes[0].Children[1].Visibility.Should().Be(Visibility.Visible);
            tree.TopLevelNodes[0].Children[2].Visibility.Should().Be(Visibility.Visible);
            tree.TopLevelNodes[1].Visibility.Should().Be(Visibility.Visible);
            tree.TopLevelNodes[2].Visibility.Should().Be(Visibility.Visible);
        }

        private CallTree CreateCallTree()
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

            var callTree = new CallTree(CodeFlowToTreeConverter.Convert(codeFlow, run: null, resultId: 0, runIndex: 0));

            return callTree;
        }
    }
}

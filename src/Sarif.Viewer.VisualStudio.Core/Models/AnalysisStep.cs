// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Telemetry;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.Sarif.Viewer.Models
{
    internal class AnalysisStep : NotifyPropertyChangedObject
    {
        private AnalysisStepNode _selectedItem;
        private DelegateCommand<TreeView> _selectPreviousCommand;
        private DelegateCommand<TreeView> _selectNextCommand;

        private ObservableCollection<AnalysisStepNode> _topLevelNodes;

        public AnalysisStep(IList<AnalysisStepNode> topLevelNodes)
        {
            this.TopLevelNodes = new ObservableCollection<AnalysisStepNode>(topLevelNodes);
        }

        public ObservableCollection<AnalysisStepNode> TopLevelNodes
        {
            get
            {
                return this._topLevelNodes;
            }

            set
            {
                this._topLevelNodes = value;

                // Set this object as the AnalysisStep for the child nodes.
                if (this._topLevelNodes != null)
                {
                    for (int i = 0; i < this._topLevelNodes.Count; i++)
                    {
                        this._topLevelNodes[i].AnalysisStep = this;
                    }
                }
            }
        }

        public AnalysisStepNode SelectedItem
        {
            get
            {
                return this._selectedItem;
            }

            set
            {
                if (this._selectedItem != value)
                {
                    this._selectedItem = value;

                    this.NotifyPropertyChanged();
                }
            }
        }

        internal static bool TryGetIndexInAnalysisStepNodeList(IList<AnalysisStepNode> list, AnalysisStepNode givenNode, out int index)
        {
            index = -1;

            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    AnalysisStepNode listNode = list[i];
                    if (listNode == givenNode)
                    {
                        index = i;
                        break;
                    }
                }
            }

            return index != -1;
        }

        internal AnalysisStepNode FindNext(AnalysisStepNode currentNode, bool includeChildren)
        {
            if (currentNode == null)
            {
                return null;
            }

            // For Call nodes, find the first visible child.
            AnalysisStepNode nextNode;
            if (includeChildren && TryGetFirstItem(currentNode.Children, out nextNode))
            {
                return nextNode;
            }

            // For all other nodes or Call nodes without a visible child, find the next visible sibling.
            AnalysisStepNode currentParent = currentNode.Parent;
            IList<AnalysisStepNode> nodeList;

            if (currentParent == null)
            {
                nodeList = this.TopLevelNodes;
            }
            else
            {
                nodeList = currentParent.Children;
            }

            if (TryGetNextSibling(nodeList, currentNode, out nextNode))
            {
                return nextNode;
            }

            // Walk up the tree trying to find the next node.
            return this.FindNext(currentParent, false);
        }

        internal AnalysisStepNode FindPrevious(AnalysisStepNode currentNode, bool includeChildren)
        {
            if (currentNode == null)
            {
                return null;
            }

            AnalysisStepNode previousNode;

            // Find the next visible sibling.
            AnalysisStepNode currentParent = currentNode.Parent;
            IList<AnalysisStepNode> nodeList;

            if (currentParent == null)
            {
                nodeList = this.TopLevelNodes;
            }
            else
            {
                nodeList = currentParent.Children;
            }

            if (TryGetPreviousSibling(nodeList, currentNode, out previousNode))
            {
                AnalysisStepNode previousNodeChild;
                if (includeChildren && TryGetLastItem(previousNode.Children, out previousNodeChild))
                {
                    return previousNodeChild;
                }
                else
                {
                    return previousNode;
                }
            }
            else if (currentParent?.Visibility == System.Windows.Visibility.Visible)
            {
                return currentParent;
            }

            // Walk up the tree trying to find the previous node.
            return this.FindPrevious(currentParent, false);
        }

        internal static bool TryGetNextSibling(IList<AnalysisStepNode> items, AnalysisStepNode currentItem, out AnalysisStepNode nextSibling)
        {
            nextSibling = null;

            int currentIndex;
            if (TryGetIndexInAnalysisStepNodeList(items, currentItem, out currentIndex))
            {
                for (int i = currentIndex + 1; i < items.Count; i++)
                {
                    AnalysisStepNode nextNode = items[i];
                    if (nextNode.Visibility == System.Windows.Visibility.Visible)
                    {
                        nextSibling = nextNode;
                        break;
                    }
                }
            }

            return nextSibling != null;
        }

        internal static bool TryGetPreviousSibling(IList<AnalysisStepNode> items, AnalysisStepNode currentItem, out AnalysisStepNode previousSibling)
        {
            previousSibling = null;

            int currentIndex;
            if (TryGetIndexInAnalysisStepNodeList(items, currentItem, out currentIndex))
            {
                for (int i = currentIndex - 1; i >= 0; i--)
                {
                    AnalysisStepNode previousNode = items[i];
                    if (previousNode.Visibility == System.Windows.Visibility.Visible)
                    {
                        previousSibling = previousNode;
                        break;
                    }
                }
            }

            return previousSibling != null;
        }

        internal static bool TryGetFirstItem(IList<AnalysisStepNode> items, out AnalysisStepNode firstItem)
        {
            firstItem = null;

            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    AnalysisStepNode nextNode = items[i];
                    if (nextNode.Visibility == System.Windows.Visibility.Visible)
                    {
                        firstItem = nextNode;
                        break;
                    }
                }
            }

            return firstItem != null;
        }

        internal static bool TryGetLastItem(IList<AnalysisStepNode> items, out AnalysisStepNode lastItem)
        {
            lastItem = null;

            if (items != null)
            {
                for (int i = items.Count - 1; i >= 0; i--)
                {
                    AnalysisStepNode nextNode = items[i];
                    if (nextNode.Visibility == System.Windows.Visibility.Visible)
                    {
                        lastItem = nextNode;
                        break;
                    }
                }
            }

            return lastItem != null;
        }

        internal AnalysisStepNode FindNext()
        {
            AnalysisStepNode next = this.FindNext(this.SelectedItem, true);
            if (next == null)
            {
                // no next exists, current remains selected
                return this.SelectedItem;
            }
            else
            {
                return next;
            }
        }

        // go to parent, find self, find previous/next, make sure not to roll off
        internal AnalysisStepNode FindPrevious()
        {
            AnalysisStepNode previous = this.FindPrevious(this.SelectedItem, true);
            if (previous == null)
            {
                // no previous exists, current remains selected
                return this.SelectedItem;
            }
            else
            {
                return previous;
            }
        }

        public DelegateCommand<TreeView> SelectPreviousCommand
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (this._selectPreviousCommand == null)
                {
                    this._selectPreviousCommand = new DelegateCommand<TreeView>(treeView =>
                    {
                        this.NavigateTo(treeView, this.FindPrevious());
                    });
                }

                return this._selectPreviousCommand;
            }
        }

        public DelegateCommand<TreeView> SelectNextCommand
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (this._selectNextCommand == null)
                {
                    this._selectNextCommand = new DelegateCommand<TreeView>(treeView =>
                    {
                        this.NavigateTo(treeView, this.FindNext());
                    });
                }

                return this._selectNextCommand;
            }
        }

        internal void NavigateTo(TreeView treeview, AnalysisStepNode node)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var model = treeview.DataContext as AnalysisStep;
            model.SelectedItem = node;
            model.SelectedItem.NavigateTo(usePreviewPane: false, moveFocusToCaretLocation: false);

            KeyEventTelemetry.Instance.TrackEvent(
                KeyEventTelemetry.EventNames.NavigateToKeyEventWarning,
                model.SelectedItem.RuleId,
                model.SelectedItem.ResultGuid,
                model.SelectedItem.Index);

            treeview.Focus();
        }

        internal void ExpandAll()
        {
            if (this.TopLevelNodes != null)
            {
                foreach (AnalysisStepNode child in this.TopLevelNodes)
                {
                    child.ExpandAll();
                }
            }
        }

        internal void CollapseAll()
        {
            if (this.TopLevelNodes != null)
            {
                foreach (AnalysisStepNode child in this.TopLevelNodes)
                {
                    child.CollapseAll();
                }
            }
        }

        internal void IntelligentExpand()
        {
            if (this.TopLevelNodes != null)
            {
                foreach (AnalysisStepNode child in this.TopLevelNodes)
                {
                    child.IntelligentExpand();
                }
            }
        }

        internal void SetVerbosity(ThreadFlowLocationImportance importance)
        {
            if (this.TopLevelNodes != null)
            {
                foreach (AnalysisStepNode child in this.TopLevelNodes)
                {
                    child.SetVerbosity(importance);
                }
            }
        }
    }
}

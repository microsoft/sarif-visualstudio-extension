// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Views
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;

    using Microsoft.Sarif.Viewer.Models;

    /// <summary>
    /// Implementation of a tree view for the call tree.
    /// </summary>
    /// <remarks>
    /// The selection in the tree view is maintained by the data model through the <see cref="CallTree.SelectedItem"/> property.
    /// This class ensures that when the selected item property is modified in the data model,
    /// that the "parent" UI elements (tree view items) in the tree view are properly expanded so the element is visible.
    /// The class must also perform the reverse logic of when the selection changes in the tree view ensure that it is reflected
    /// back in the data model.
    /// A future enhancement would be to separate out the "view logic data" from the "SARIF data model". Specifically the concept
    /// of the selected item in the view should be distinct and separate from <see cref="CallTree.SelectedItem"/> as that is not
    /// a "SARIF" concept.
    /// </remarks>
    internal class CallTreeView : TreeView
    {
        public CallTreeView()
        {
            SelectedItemChanged += CallTree_TreeView_SelectionChanged;
            Loaded += CallTree_TreeView_Loaded;
            Unloaded += CallTree_TreeView_Unloaded;
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            // Subscribe to the call tree's property changed notification
            // so that this control can properly handle any changes to the selected item
            // (selected call tree node).
            if (DataContext is INotifyPropertyChanged notifyPropertyChanged)
            {
                notifyPropertyChanged.PropertyChanged += CallTree_PropertyChanged;
            }
        }

        /// <summary>
        /// Called when properties of the <see cref="CallTree"/> is modified.
        /// </summary>
        /// <param name="sender">The <see cref="CallTree"/> object whose properties are modified.</param>
        /// <param name="e">An instance of <see cref="PropertyChangedEventArgs"/> that contains the name of the modified property.</param>
        /// <remarks>
        /// For the purposes of behavior in this tree view, we only pay attention to the <see cref="CallTree.SelectedItem"/> property.
        /// </remarks>
        private void CallTree_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CallTree.SelectedItem) &&
                sender is CallTree callTree &&
                callTree.SelectedItem != SelectedItem)
            {
                EnsureNodeIsVisibleAndSelected(this, callTree.SelectedItem);
            }
        }

        private void CallTree_TreeView_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // Reflect the change in selection in the tree view back to the call tree.
            if (DataContext is CallTree callTree)
            {
                callTree.SelectedItem = e.NewValue as CallTreeNode;
            }
        }

        private void CallTree_TreeView_Unloaded(object sender, RoutedEventArgs e)
        {
            SelectedItemChanged -= CallTree_TreeView_SelectionChanged;
            Unloaded -= CallTree_TreeView_Unloaded;
            Loaded -= CallTree_TreeView_Loaded;

            if (DataContext is INotifyPropertyChanged notifyPropertyChanged)
            {
                notifyPropertyChanged.PropertyChanged -= CallTree_PropertyChanged;
            }
        }

        private void CallTree_TreeView_Loaded(object sender, RoutedEventArgs e)
        {
            // On initial load, attempt to select the desired item from the data model.
            if (SelectedItem == null &&
                DataContext is CallTree callTree &&
                callTree.SelectedItem != null)
            {
                EnsureNodeIsVisibleAndSelected(this, callTree.SelectedItem);
            }
        }

        private static void EnsureNodeIsVisibleAndSelected(TreeView callTreeView, object newSelectedNode)
        {
            // In order to set the current node, we need to find it in the tree by navigating
            // the hierarchy from the root down

            Stack<CallTreeNode> pathToNode = new Stack<CallTreeNode>();
            CallTreeNode currentNode = newSelectedNode as CallTreeNode;

            // Construct the path to the new node
            while (currentNode != null)
            {
                pathToNode.Push(currentNode);
                currentNode = currentNode.Parent;
            }

            int depth = pathToNode.Count;
            TreeViewItem node = null;

            // Walk the tree from the root to the new node
            while (pathToNode.Count > 0)
            {
                currentNode = pathToNode.Pop();
                ItemsControl parent;

                if (pathToNode.Count == depth - 1)
                {
                    parent = callTreeView;
                }
                else
                {
                    parent = node;
                }

                node = (TreeViewItem)parent.ItemContainerGenerator.ContainerFromItem(currentNode);

                // Make sure to expand all the nodes in the hierarchy as we walk down.
                // But we don't want to expand the selected node because we only
                // want to select that node inside it's parent tree view item, not show
                // it's children.
                if (node != null
                    && node != newSelectedNode
                    && !node.IsExpanded)
                {
                    node.ExpandSubtree();
                }
            }

            // If we found the node in the XAML UI tree, make sure it is selected
            // and in view.
            if (node != null)
            {
                node.BringIntoView();
                node.UpdateLayout();

                // You might be tempted to set focus to this node
                // as well as select it. If the node is focused
                // then when a user is navigating through a source file
                // in the editor and a "Tagged" region is selected (See ResultTextMark)
                // due to the caret entering a region, then focus is moved off the editor
                // which means it's nearly impossible for the user to edit highlighted text.
                if (!node.IsSelected)
                {
                    node.IsSelected = true;
                }
            }
        }
    }
}

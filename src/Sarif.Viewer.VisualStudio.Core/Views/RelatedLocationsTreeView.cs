// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Microsoft.Sarif.Viewer.Models;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.Sarif.Viewer.Views
{
    internal class RelatedLocationsTreeView : TreeView
    {
        public RelatedLocationsTreeView()
        {
            SelectedItemChanged += this.RelatedLocationsTreeView_SelectedItemChanged;
            KeyDown += this.RelatedLocationsTreeView_KeyDown;
        }

        private void RelatedLocationsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (this.DataContext is SarifErrorListItem errorListItem)
            {
                if (e.OldValue is LocationModel oldSelectedLocation)
                {
                    oldSelectedLocation.IsSelectedNode = false;
                    errorListItem.RelatedLocations.SelectedItem = null;
                }

                if (e.NewValue is LocationModel newSelectedLocation)
                {
                    newSelectedLocation.IsSelectedNode = true;
                    errorListItem.RelatedLocations.SelectedItem = newSelectedLocation;
                }

                e.Handled = true;
            }
        }

        private void RelatedLocationsTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                if (e.Source is TreeView treeView)
                {
                    if (treeView.SelectedItem is LocationModel locationModel)
                    {
                        locationModel.NavigateTo();
                        treeView.Focus();
                        e.Handled = true;
                    }
                }
            }
        }
    }
}

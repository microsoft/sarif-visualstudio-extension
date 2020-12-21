// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.Sarif.Viewer.Models
{
    internal class LocationCollection : ObservableCollection<LocationModel>
    {
        private string _message;
        private LocationModel _selectedItem;
        private DelegateCommand<LocationModel> _selectedCommand;

        public LocationCollection(string message)
        {
            this._message = message;

            // Subscribe to collection changed events so we can listen
            // to property change notifications from our child items
            // and set our selected item property.
            this.CollectionChanged += this.LocationCollection_CollectionChanged;
        }

        private void LocationCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.NewItems != null)
                {
                    foreach (object newItem in e.NewItems)
                    {
                        if (newItem is INotifyPropertyChanged notifyPropertyChanged)
                        {
                            notifyPropertyChanged.PropertyChanged += this.LocationModelPropertyChanged;
                        }
                    }
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                if (e.OldItems != null)
                {
                    foreach (object oldItem in e.OldItems)
                    {
                        if (oldItem is INotifyPropertyChanged notifyPropertyChanged)
                        {
                            notifyPropertyChanged.PropertyChanged -= this.LocationModelPropertyChanged;
                        }
                    }
                }
            }
        }

        private void LocationModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (e.PropertyName == nameof(LocationModel.IsSelected) && sender is LocationModel locationModel)
            {
                this.SelectedItem = locationModel;
            }
        }

        public string Message
        {
            get
            {
                return this._message;
            }
            set
            {
                if (value != this._message)
                {
                    this._message = value;

                    this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.Message)));
                }
            }
        }

        /// <summary>
        /// Gets or sets whether this model shows as selected (without affecting keyboard focus)
        /// in the SARIF explorer UI.
        /// </summary>
        /// <remarks>
        /// Future enhancement, factor this out of the data model into a view model as this is not
        /// part of the SARIF model.
        /// </remarks>
        public LocationModel SelectedItem
        {
            get
            {
                return this._selectedItem;
            }
            set
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (this._selectedItem != value)
                {
                    // If we have a selected item, make sure to mark it unselected.
                    if (this._selectedItem != null)
                    {
                        this._selectedItem.IsSelected = false;
                    }

                    this._selectedItem = value;

                    // If we have a selected item, make sure to mark it selected.
                    if (this._selectedItem != null)
                    {
                        this._selectedItem.IsSelected = true;
                    }

                    this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.SelectedItem)));
                }
            }
        }

        public DelegateCommand<LocationModel> SelectedCommand
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (this._selectedCommand == null)
                {
                    this._selectedCommand = new DelegateCommand<LocationModel>(l => this.SelectionChanged(l));
                }

                return this._selectedCommand;
            }
            set
            {
                this._selectedCommand = value;
            }
        }

        private void SelectionChanged(LocationModel selectedItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            selectedItem.NavigateTo(usePreviewPane: true, moveFocusToCaretLocation: false);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using Microsoft.VisualStudio.Shell;
using System.Collections.ObjectModel;
using System.ComponentModel;

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
        }

        public string  Message
        {
            get
            {
                return _message;
            }
            set
            {
                if (value != this._message)
                {
                    _message = value;

                    this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.Message)));
                }
            }
        }

        public LocationModel SelectedItem
        {
            get
            {
                return _selectedItem;
            }
            set
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (this._selectedItem != value)
                {
                    _selectedItem = value;
                    this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.SelectedItem)));
                }
            }
        }

        public DelegateCommand<LocationModel> SelectedCommand
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (_selectedCommand == null)
                {
                    _selectedCommand = new DelegateCommand<LocationModel>(l => this.SelectionChanged(l));
                }

                return _selectedCommand;
            }
            set
            {
                _selectedCommand = value;
            }
        }

        private void SelectionChanged(LocationModel selectedItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            selectedItem.NavigateTo(usePreviewPane: true, moveFocusToCaretLocation: false);
        }
    }
}

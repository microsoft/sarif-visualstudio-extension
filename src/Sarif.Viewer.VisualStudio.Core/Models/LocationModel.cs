// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Numerics;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;

namespace Microsoft.Sarif.Viewer.Models
{
    internal class LocationModel : CodeLocationObject
    {
        private string _message;
        private string _logicalLocation;
        private string _module;
        private bool _isEssential;
        private bool _isSelected;
        private DelegateCommand _navigateCommand;

        public LocationModel()
            : base(resultId: -1, runIndex: -1)
        {
        }

        public LocationModel(int resultId, int runIndex)
            : base(resultId: resultId, runIndex: runIndex)
        {
        }

        public BigInteger Id { get; set; }

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
                    this.NotifyPropertyChanged();
                }
            }
        }

        public override string FilePath
        {
            get
            {
                return base.FilePath;
            }

            set
            {
                if (value != this._filePath)
                {
                    base.FilePath = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public string FileName
        {
            get
            {
                return Path.GetFileName(this.FilePath);
            }
        }

        public string LocationDisplayString => $"{this.FileName} {this.RegionDisplayString}";

        public string Module
        {
            get
            {
                return this._module;
            }

            set
            {
                if (value != this._module)
                {
                    this._module = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public string LogicalLocation
        {
            get
            {
                return this._logicalLocation;
            }

            set
            {
                if (value != this._logicalLocation)
                {
                    this._logicalLocation = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool IsEssential
        {
            get
            {
                return this._isEssential;
            }

            set
            {
                if (value != this._isEssential)
                {
                    this._isEssential = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public int Index { get; set; }

        public int NestingLevel { get; set; } = 0;

        public IList<LocationModel> Children { get; } = new List<LocationModel>();

        public LocationModel Parent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this model shows as selected (without affecting keyboard focus)
        /// in the SARIF explorer UI.
        /// </summary>
        /// <remarks>
        /// Future enhancement, factor this out of the data model into a view model as this is not
        /// part of the SARIF data model.
        /// </remarks>
        public bool IsSelected
        {
            get => this._isSelected;

            set
            {
                if (value != this._isSelected)
                {
                    this._isSelected = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a persistent span that represents the location's region.
        /// </summary>
        public IPersistentSpan PersistentSpan { get; set; }

        public DelegateCommand NavigateCommand
        {
            get
            {
                this._navigateCommand ??= new DelegateCommand(this.Navigate);
                return this._navigateCommand;
            }

            set
            {
                this._navigateCommand = value;
            }
        }

        private void Navigate()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            this.IsSelected = true;
            this.NavigateTo(usePreviewPane: false, moveFocusToCaretLocation: true);
        }
    }
}

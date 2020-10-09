// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System.IO;

namespace Microsoft.Sarif.Viewer.Models
{
    internal class LocationModel : CodeLocationObject
    {
        private string _message;
        private string _logicalLocation;
        private string _module;
        private bool _isEssential;

        public LocationModel(int resultId, int runIndex)
            : base(resultId: resultId, runIndex: runIndex)
        {
        }

        public int Id { get; set; }

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
                    NotifyPropertyChanged();
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
                    NotifyPropertyChanged();
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
                    NotifyPropertyChanged();
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
                    NotifyPropertyChanged();
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
                    NotifyPropertyChanged();
                }
            }
        }

        public int Index { get; set; }
        public bool IsSelected { get; set; }
    }
}

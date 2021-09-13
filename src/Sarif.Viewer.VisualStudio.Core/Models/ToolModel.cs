// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.Models
{
    public class ToolModel : NotifyPropertyChangedObject
    {
        private string _name;
        private string _fullName;
        private string _version;
        private string _semanticVersion;

        public string Name
        {
            get
            {
                return this._name;
            }

            set
            {
                if (value != this._name)
                {
                    this._name = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public string FullName
        {
            get
            {
                return this._fullName;
            }

            set
            {
                if (value != this._fullName)
                {
                    this._fullName = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public string Version
        {
            get
            {
                return this._version;
            }

            set
            {
                if (value != this._version)
                {
                    this._version = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public string SemanticVersion
        {
            get
            {
                return this._semanticVersion;
            }

            set
            {
                if (value != this._semanticVersion)
                {
                    this._semanticVersion = value;
                    this.NotifyPropertyChanged();
                }
            }
        }
    }
}

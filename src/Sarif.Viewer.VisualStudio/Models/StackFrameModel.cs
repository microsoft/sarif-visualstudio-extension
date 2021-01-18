// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.Sarif.Viewer.Models
{
    internal class StackFrameModel : CodeLocationObject
    {
        private string _message;
        private int _line;
        private int _column;
        private int _address;
        private int _offset;
        private string _fullyQualifiedLogicalName;
        private string _module;
        private DelegateCommand _navigateCommand;

        public StackFrameModel(int resultId, int runIndex)
            : base(resultId: resultId, runIndex: runIndex)
        {
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
                    this.NotifyPropertyChanged();
                }
            }
        }

        public int Line
        {
            get
            {
                return this._line;
            }

            set
            {
                if (value != this._line)
                {
                    this._line = value;
                    this.NotifyPropertyChanged();
                    this.NotifyPropertyChanged(nameof(this.FullyQualifiedLocation));
                }
            }
        }

        public int Column
        {
            get
            {
                return this._column;
            }

            set
            {
                if (value != this._column)
                {
                    this._column = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public int Address
        {
            get
            {
                return this._address;
            }

            set
            {
                if (value != this._address)
                {
                    this._address = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public int Offset
        {
            get
            {
                return this._offset;
            }

            set
            {
                if (value != this._offset)
                {
                    this._offset = value;
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

        public string FullyQualifiedLogicalName
        {
            get
            {
                return this._fullyQualifiedLogicalName;
            }

            set
            {
                if (value != this._fullyQualifiedLogicalName)
                {
                    this._fullyQualifiedLogicalName = value;
                    this.NotifyPropertyChanged();
                    this.NotifyPropertyChanged(nameof(this.FullyQualifiedLocation));
                }
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
                    this.NotifyPropertyChanged();
                    this.NotifyPropertyChanged(nameof(this.FullyQualifiedLocation));
                }
            }
        }

        public string FullyQualifiedLocation
        {
            get
            {
                string val = string.Empty;

                if (!string.IsNullOrWhiteSpace(this.Module))
                {
                    val += this.Module + "!";
                }

                val += this.FullyQualifiedLogicalName;

                if (this.Line > 0)
                {
                    val += " Line " + this.Line;
                }

                return val;
            }
        }

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
            this.NavigateTo(usePreviewPane: true, moveFocusToCaretLocation: false);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.IO;

namespace Microsoft.Sarif.Viewer.Models
{
    public class ArtifactChangeModel : NotifyPropertyChangedObject
    {
        private string _filePath;

        public ArtifactChangeModel()
        {
            this.Replacements = new ObservableCollection<ReplacementModel>();
        }

        public string FilePath
        {
            get
            {
                return this._filePath;
            }

            set
            {
                if (value != this._filePath)
                {
                    this._filePath = value;

                    this.NotifyPropertyChanged();
                    this.NotifyPropertyChanged(nameof(this.FileName));
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

        public ObservableCollection<ReplacementModel> Replacements { get; }
    }
}

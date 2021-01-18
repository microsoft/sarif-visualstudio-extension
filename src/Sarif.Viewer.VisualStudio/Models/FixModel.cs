// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace Microsoft.Sarif.Viewer.Models
{
    public class FixModel : NotifyPropertyChangedObject
    {
        protected string _description;
        protected ObservableCollection<ArtifactChangeModel> _artifactChanges;

        public delegate void FixAppliedHandler();

        public FixModel(string description)
        {
            this._description = description;
            this._artifactChanges = new ObservableCollection<ArtifactChangeModel>();
        }

        public string Description
        {
            get
            {
                return this._description;
            }

            set
            {
                if (value != this._description)
                {
                    this._description = value;

                    this.NotifyPropertyChanged();
                }
            }
        }

        public ObservableCollection<ArtifactChangeModel> ArtifactChanges
        {
            get
            {
                return this._artifactChanges;
            }
        }
    }
}

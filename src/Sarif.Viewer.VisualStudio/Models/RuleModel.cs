// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using Microsoft.CodeAnalysis.Sarif;

namespace Microsoft.Sarif.Viewer.Models
{
    public class RuleModel : NotifyPropertyChangedObject
    {
        private string _id;
        private string _name;
        private string _category;
        private string _description;
        private string _helpUri;
        private FailureLevel _defaultFailureLevel;

        public string Id
        {
            get
            {
                return _id;
            }
            set
            {
                if (value != _id)
                {
                    _id = value;
                    NotifyPropertyChanged(nameof(Id));
                }
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    NotifyPropertyChanged(nameof(Name));
                }
            }
        }

        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                if (value != _description)
                {
                    _description = value;
                    NotifyPropertyChanged(nameof(Description));
                }
            }
        }

        public string Category
        {
            get
            {
                return _category;
            }
            set
            {
                if (value != _category)
                {
                    _category = value;
                    NotifyPropertyChanged(nameof(Category));
                }
            }
        }

        public FailureLevel DefaultFailureLevel
        {
            get
            {
                return _defaultFailureLevel;
            }
            set
            {
                if (value != _defaultFailureLevel)
                {
                    _defaultFailureLevel = value;
                    NotifyPropertyChanged(nameof(DefaultFailureLevel));
                }
            }
        }

        public FailureLevel FailureLevel
        {
            get
            {
                FailureLevel level = FailureLevel.Warning;

                if (DefaultFailureLevel != FailureLevel.None)
                {
                    string defaultLevel = DefaultFailureLevel.ToString();

                    level = (FailureLevel)Enum.Parse(typeof(FailureLevel), defaultLevel);
                }

                return level;
            }
        }

        public string HelpUri
        {
            get
            {
                return _helpUri;
            }
            set
            {
                if (value != _helpUri)
                {
                    _helpUri = value;
                    NotifyPropertyChanged(nameof(HelpUri));
                }
            }
        }
    }
}

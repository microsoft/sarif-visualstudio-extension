﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.VisualStudio.Shell;

using XamlDoc = System.Windows.Documents;

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
        private ObservableCollection<XamlDoc.Inline> _descriptionInlines;

        public string Id
        {
            get
            {
                return this._id;
            }

            set
            {
                if (value != this._id)
                {
                    this._id = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

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

        public string Category
        {
            get
            {
                return this._category;
            }

            set
            {
                if (value != this._category)
                {
                    this._category = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public FailureLevel DefaultFailureLevel
        {
            get
            {
                return this._defaultFailureLevel;
            }

            set
            {
                if (value != this._defaultFailureLevel)
                {
                    this._defaultFailureLevel = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public FailureLevel FailureLevel
        {
            get
            {
                FailureLevel level = FailureLevel.Warning;

                if (this.DefaultFailureLevel != FailureLevel.None)
                {
                    string defaultLevel = this.DefaultFailureLevel.ToString();

                    level = (FailureLevel)Enum.Parse(typeof(FailureLevel), defaultLevel);
                }

                return level;
            }
        }

        public string HelpUri
        {
            get
            {
                return this._helpUri;
            }

            set
            {
                if (value != this._helpUri)
                {
                    this._helpUri = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public string DisplayName
        {
            get
            {
                // if rule name equals rule id, only display rule id in SARIF explorer by setting name to null
                return (this._name != null && this._name == this._id) ? null : this._name;
            }
        }

        public bool ShowPlainDescription
        {
            get
            {
                return !string.IsNullOrWhiteSpace(this.Description) && !(this.DescriptionInlines?.Any() == true);
            }
        }

        public ObservableCollection<XamlDoc.Inline> DescriptionInlines
        {
            get
            {
                return this._descriptionInlines ??=
                    string.IsNullOrWhiteSpace(this.Description)
                    ? null
                    : new ObservableCollection<XamlDoc.Inline>(SdkUIUtilities.GetMessageInlines(this.Description, this.InlineLink_Click));
            }
        }

        internal void InlineLink_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!(sender is XamlDoc.Hyperlink hyperLink))
            {
                return;
            }

            string uriString = null;
            if (hyperLink.Tag is string uriAsString)
            {
                uriString = uriAsString;
            }
            else if (hyperLink.Tag is Uri uri)
            {
                uriString = uri.ToString();
            }

            if (!string.IsNullOrEmpty(uriString) && Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out Uri _))
            {
                SdkUIUtilities.OpenExternalUrl(uriString);
            }
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Controls;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Controls;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.Sarif.Viewer.Models
{
    internal class SuppressionModel : NotifyPropertyChangedObject
    {
        private SuppressionKind _kind = SuppressionKind.External;
        private SuppressionStatus _status = SuppressionStatus.Accepted;
        private IEnumerable<SarifErrorListItem> _sarifErrorListItems;

        public SuppressionModel(IEnumerable<SarifErrorListItem> sarifErrorListItems)
        {
            this._sarifErrorListItems = sarifErrorListItems;
        }

        public SuppressionModel(SarifErrorListItem sarifErrorListItem)
        {
            this._sarifErrorListItems = new[] { sarifErrorListItem };
        }

        public IEnumerable<SarifErrorListItem> SelectedErrorListItems
        {
            get => this._sarifErrorListItems;
            set
            {
                if (value != this._sarifErrorListItems)
                {
                    this._sarifErrorListItems = value;
                }
            }
        }

        [Browsable(false)]
        public SuppressionKind Kind
        {
            get => this._kind;
            set
            {
                if (value != this._kind)
                {
                    this._kind = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        [Browsable(false)]
        public SuppressionStatus Status
        {
            get => this._status;
            set
            {
                if (value != this._status)
                {
                    this._status = value;
                    this.NotifyPropertyChanged();
                }
            }
        }
    }
}

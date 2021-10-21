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
        private Guid? _guid = System.Guid.NewGuid();
        private SuppressionKind _kind = SuppressionKind.External;
        private SuppressionStatus _status = SuppressionStatus.UnderReview;
        private readonly IEnumerable<SuppressionStatus> _suppressionStatusValues = new[] { SuppressionStatus.UnderReview, SuppressionStatus.Accepted, SuppressionStatus.Rejected };
        private string _justification;
        private string _userAlias;
        private DateTime? _timestamp;
        private DateTime? _expiryDate;
        private int _expiryInDays = 0;
        private DelegateCommand _openSuppressionDialogCommand;
        private Microsoft.VisualStudio.PlatformUI.DelegateCommand _addSuppressionCommand;
        private IEnumerable<SarifErrorListItem> _sarifErrorListItems;

        public SuppressionModel(IEnumerable<SarifErrorListItem> sarifErrorListItems)
        {
            this._sarifErrorListItems = sarifErrorListItems;
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
        public Guid? Guid
        {
            get => this._guid;
            set
            {
                if (value != this._guid)
                {
                    this._guid = value;
                    this.NotifyPropertyChanged();
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

        [Browsable(false)]
        public string Justification
        {
            get => this._justification;
            set
            {
                if (value != this._justification)
                {
                    this._justification = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        [Browsable(false)]
        public string UserAlias
        {
            get => this._userAlias;
            set
            {
                if (value != this._userAlias)
                {
                    this._userAlias = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        [Browsable(false)]
        public DateTime? Timestamp
        {
            get => this._timestamp;
            set
            {
                if (value != this._timestamp)
                {
                    this._timestamp = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        [Browsable(false)]
        public DateTime? ExpiryDate
        {
            get => this._expiryDate;
            set
            {
                if (value != this._expiryDate)
                {
                    this._expiryDate = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        [Browsable(false)]
        public int ExpiryInDays
        {
            get => this._expiryInDays;
            set
            {
                if (value != this._expiryInDays)
                {
                    this._expiryInDays = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        [Browsable(false)]
        public string IconPath
        {
            get
            {
                switch (this._status)
                {
                    case SuppressionStatus.UnderReview:
                        return "../SuppressionUnderReview.png";
                    case SuppressionStatus.Accepted:
                        return "../SuppressionAccepted.png";
                    case SuppressionStatus.Rejected:
                        return "../SuppressionRejected.png";
                    default:
                        return string.Empty;
                }
            }
        }

        public IEnumerable<SuppressionStatus> SuppressionStatusValues => _suppressionStatusValues;

        [Browsable(false)]
        public DelegateCommand OpenSuppressionDialogCommand => this._openSuppressionDialogCommand ??= new DelegateCommand(() =>
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var suppressionDialog = new SuppressionDialog(
                new SuppressionModel(this._sarifErrorListItems));
            suppressionDialog.ShowModal();
        });

        [Browsable(false)]
        public Microsoft.VisualStudio.PlatformUI.DelegateCommand AddSuppressionCommand
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

#if DEV17
                this._addSuppressionCommand ??= new Microsoft.VisualStudio.PlatformUI.DelegateCommand(
                execute: (param) =>
                {
                    var dialogWindow = param as Microsoft.VisualStudio.PlatformUI.DialogWindow;
                    dialogWindow.Close();

                    CodeAnalysisResultManager.Instance.AddSuppressionToSarifLog(this);
                },
                canExecute: (obj) => true,
                jtf: null);
#else
                this._addSuppressionCommand ??= new Microsoft.VisualStudio.PlatformUI.DelegateCommand(
                execute: (param) =>
                {
                    var dialogWindow = param as Microsoft.VisualStudio.PlatformUI.DialogWindow;
                    dialogWindow.Close();

                    CodeAnalysisResultManager.Instance.AddSuppressionToSarifLog(this);
                });
#endif
                return this._addSuppressionCommand;
            }
        }
    }

    public class ExpiryDaysValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string strValue = Convert.ToString(value);
            if (string.IsNullOrWhiteSpace(strValue))
            {
                return new ValidationResult(false, $"Expiry in days value should not be empty.");
            }

            if (!int.TryParse(strValue, out int intVal))
            {
                return new ValidationResult(false, $"Expiry in days value should be an integer number.");
            }

            if (intVal < 0)
            {
                return new ValidationResult(false, $"Expiry in days value should be greater than or equal to 0.");
            }

            return new ValidationResult(true, null);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.Sarif.Viewer.Models
{
    public class AnalysisStepState : NotifyPropertyChangedObject
    {
        private DelegateCommand _variableCheckedCommand;

        private readonly string _value;

        public bool Selected { get; set; }

        public bool ValueChanged { get; set; }

        public string Expression { get; set; }

        public string Value { get => this._value; }

        public string NormalizedValue { get => this.NormalizeStateValue(); }

        public DelegateCommand VariableCheckedCommand
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (this._variableCheckedCommand == null)
                {
                    this._variableCheckedCommand = new DelegateCommand(() =>
                    {
                        this.Test();
                    });
                }

                return this._variableCheckedCommand;
            }
        }

        public AnalysisStepState(string expression, string value)
        {
            this.Expression = expression;
            this._value = value;
        }

        private void Test()
        {
        }

        private string NormalizeStateValue()
        {
            if (this._value == null)
            {
                return null;
            }

            return this._value.Replace("{expr}", this.Expression);
        }
    }
}

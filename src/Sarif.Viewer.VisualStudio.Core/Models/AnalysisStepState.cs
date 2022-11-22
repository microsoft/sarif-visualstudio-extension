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

        public bool Selected { get; set; }

        public bool ValueChanged { get; set; }

        public string Expression { get; set; }

        public string Value { get; set; }

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
            this.Value = value;
        }

        private void Test()
        {
        }
    }
}

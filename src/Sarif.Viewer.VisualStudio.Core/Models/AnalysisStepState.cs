// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Microsoft.Sarif.Viewer.Models
{
    public class AnalysisStepState : NotifyPropertyChangedObject
    {
        public string Expression { get; set; }

        public string Value { get; set; }

        public AnalysisStepState(string expression, string value)
        {
            this.Expression = expression;
            this.Value = value;
        }
    }
}

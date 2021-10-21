// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.Sarif.Viewer.Models
{
    internal class SuppressionCollection : ObservableCollection<SuppressionModel>
    {
        public SuppressionCollection()
        {
        }

        public VSSuppressionState SuppressionState { get; set; }
    }
}

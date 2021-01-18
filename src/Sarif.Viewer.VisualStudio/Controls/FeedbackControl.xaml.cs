// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Windows.Controls;

using Microsoft.Sarif.Viewer.Models;

namespace Microsoft.Sarif.Viewer.Controls
{
    /// <summary>
    /// Interaction logic for FeedbackControl.xaml.
    /// </summary>
    public partial class FeedbackControl : UserControl
    {
        public FeedbackControl(FeedbackModel model)
        {
            this.InitializeComponent();

            this.DataContext = model;
        }
    }
}

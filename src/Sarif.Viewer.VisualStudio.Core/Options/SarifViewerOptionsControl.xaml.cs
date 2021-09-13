// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Windows.Controls;

namespace Microsoft.Sarif.Viewer.Options
{
    /// <summary>
    /// Interaction logic for SariferOptionsControl.xaml.
    /// </summary>
    public partial class SarifViewerOptionsControl : UserControl
    {
        /// <summary>
        /// A handle to the options page instance that this control is bound to.
        /// </summary>
        private readonly SarifViewerOptionPage sariferOptionsPage;

        public SarifViewerOptionsControl(SarifViewerOptionPage page)
        {
            InitializeComponent();
            sariferOptionsPage = page;
            this.DataContext = sariferOptionsPage;
        }
    }
}

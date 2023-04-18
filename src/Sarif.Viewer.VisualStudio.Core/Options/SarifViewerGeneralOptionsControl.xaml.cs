// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Windows.Controls;

namespace Microsoft.Sarif.Viewer.Options
{
    /// <summary>
    /// Interaction logic for SarifGeneralOptionsControl.xaml.
    /// </summary>
    public partial class SarifViewerGeneralOptionsControl : UserControl
    {
        /// <summary>
        /// A handle to the options page instance that this control is bound to.
        /// </summary>
        private readonly SarifViewerGeneralOptionsPage sariferOptionsPage;

        public SarifViewerGeneralOptionsControl(SarifViewerGeneralOptionsPage page)
        {
            InitializeComponent();
            sariferOptionsPage = page;
            this.DataContext = sariferOptionsPage;
        }
    }
}

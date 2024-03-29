﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Windows.Controls;

namespace Microsoft.Sarif.Viewer.Options
{
    /// <summary>
    /// Interaction logic for SariferOptionsControl.xaml.
    /// </summary>
    public partial class SarifViewerColorOptionsControl : UserControl
    {
        /// <summary>
        /// A handle to the options page instance that this control is bound to.
        /// </summary>
        private readonly SarifViewerColorOptionsPage colorOptionsPage;

        public SarifViewerColorOptionsControl(SarifViewerColorOptionsPage page)
        {
            InitializeComponent();
            colorOptionsPage = page;
            this.DataContext = colorOptionsPage;
        }
    }
}

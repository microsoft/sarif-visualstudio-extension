// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Windows.Controls;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// Interaction logic for SariferOptionsControl.xaml.
    /// </summary>
    public partial class SariferOptionsControl : UserControl
    {
        /// <summary>
        /// A handle to the options page instance that this control is bound to.
        /// </summary>
        private readonly SariferOptionsPage sariferOptionsPage;

        public SariferOptionsControl(SariferOptionsPage page)
        {
            InitializeComponent();
            sariferOptionsPage = page;
            this.DataContext = sariferOptionsPage;
        }
    }
}

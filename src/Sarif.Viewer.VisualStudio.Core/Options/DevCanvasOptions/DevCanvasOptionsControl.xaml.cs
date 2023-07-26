// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Windows.Controls;

namespace Microsoft.Sarif.Viewer.Options
{
    /// <summary>
    /// Interaction logic for SarifGeneralOptionsControl.xaml.
    /// </summary>
    public partial class DevCanvasOptionsControl : UserControl
    {
        /// <summary>
        /// A handle to the options page instance that this control is bound to.
        /// </summary>
        private readonly DevCanvasOptionsPage generalOptionsPage;

        public DevCanvasOptionsControl(DevCanvasOptionsPage page)
        {
            InitializeComponent();
            generalOptionsPage = page;
            this.DataContext = generalOptionsPage;
        }
    }
}

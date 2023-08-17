// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Windows.Controls;

using Microsoft.Sarif.Viewer.ResultSources.Domain.Models;

namespace Microsoft.Sarif.Viewer.Options
{
    /// <summary>
    /// Interaction logic for SarifGeneralOptionsControl.xaml.
    /// </summary>
    public partial class SarifViewerGeneralOptionsControl : UserControl
    {
        /// <summary>
        /// Fired when an event is fired by the settings ui.
        /// Some examples of this are button clicks or other listeners.
        /// </summary>
        public event EventHandler<SettingsEventArgs> SettingsEvent;

        /// <summary>
        /// A handle to the options page instance that this control is bound to.
        /// </summary>
        private readonly SarifViewerGeneralOptionsPage generalOptionsPage;

        public SarifViewerGeneralOptionsControl(SarifViewerGeneralOptionsPage page)
        {
            InitializeComponent();
            generalOptionsPage = page;
            this.DataContext = generalOptionsPage;
        }

        private void OnDevCanvasLoginButtonClick(object sender, System.Windows.RoutedEventArgs e)
        {
            SettingsEventArgs args = new SettingsEventArgs()
            {
                EventName = "DevCanvasLoginButtonClicked",
                Value = !this.generalOptionsPage.DevCanvasLoggedIn,
            };
            SettingsEvent?.Invoke(this, args);
        }
    }
}

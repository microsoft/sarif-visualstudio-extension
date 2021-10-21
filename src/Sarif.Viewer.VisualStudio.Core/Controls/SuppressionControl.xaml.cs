// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using System.Windows.Controls;

using Microsoft.Sarif.Viewer.Models;

namespace Microsoft.Sarif.Viewer.Controls
{
    /// <summary>
    /// Interaction logic for SuppressionControl.xaml.
    /// </summary>
    public partial class SuppressionControl : UserControl
    {
        private readonly Regex regex = new Regex("[^0-9]+");

        internal SuppressionControl(SuppressionModel model)
        {
            this.InitializeComponent();

            this.DataContext = model;
        }

        private void ExpiryInDaysTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}

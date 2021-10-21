// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Windows;

using Microsoft.Sarif.Viewer.Models;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.Sarif.Viewer.Controls
{
    internal class SuppressionDialog : DialogWindow
    {
        public SuppressionDialog(SuppressionModel suppressionModel)
        {
            this.Title = Viewer.Resources.SuppressionDialog_DialogTitle;
            this.SizeToContent = SizeToContent.WidthAndHeight;
            this.ResizeMode = ResizeMode.NoResize;

            this.Content = new SuppressionControl(suppressionModel);
        }
    }
}

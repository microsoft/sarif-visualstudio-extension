// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Windows;

using Microsoft.Sarif.Viewer.Models;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.Sarif.Viewer.Controls
{
    internal class FeedbackDialog : DialogWindow
    {
        public FeedbackDialog(string title, SarifErrorListItem sarifErrorListItem)
        {
            this.Title = title;
            this.SizeToContent = SizeToContent.WidthAndHeight;

            var model = new FeedbackModel(sarifErrorListItem.Rule.Id);

            this.Content = new FeedbackControl(model);
        }
    }
}

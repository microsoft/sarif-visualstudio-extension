// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Windows;

using Microsoft.Sarif.Viewer.Models;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.Sarif.Viewer.Controls
{
    internal class FeedbackDialog : DialogWindow
    {
        public FeedbackDialog(string title, SarifErrorListItem sarifErrorListItem, FeedbackType feedbackType, string snippet, string summary)
        {
            this.Title = title;
            this.SizeToContent = SizeToContent.WidthAndHeight;
            this.ResizeMode = ResizeMode.NoResize;

            var model = new FeedbackModel(sarifErrorListItem.Rule.Id, sarifErrorListItem.Tool.Name, sarifErrorListItem.Tool.Version, snippet, feedbackType, summary);

            this.Content = new FeedbackControl(model);
        }
    }
}

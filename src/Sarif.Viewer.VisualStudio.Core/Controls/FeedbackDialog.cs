// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Windows;

using Microsoft.Sarif.Viewer.Models;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.Sarif.Viewer.Controls
{
    internal class FeedbackDialog : DialogWindow
    {
        public FeedbackDialog(string title, FeedbackModel feedbackModel)
        {
            this.Title = title;
            this.SizeToContent = SizeToContent.WidthAndHeight;
            this.ResizeMode = ResizeMode.NoResize;

            this.Content = new FeedbackControl(feedbackModel);
        }
    }
}

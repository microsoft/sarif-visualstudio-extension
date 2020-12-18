// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Windows;

using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.Sarif.Viewer.Controls
{
    internal class FeedbackDialog : DialogWindow
    {
        public FeedbackDialog(string title)
        {
            this.Title = title;
            this.SizeToContent = SizeToContent.WidthAndHeight;
            this.Content = new FeedbackControl();
        }
    }
}

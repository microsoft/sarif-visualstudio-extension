// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.PlatformUI;

using System.Windows;

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

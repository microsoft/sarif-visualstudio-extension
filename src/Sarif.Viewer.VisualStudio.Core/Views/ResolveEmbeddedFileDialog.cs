// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Windows;

using Microsoft.Sarif.Viewer.ViewModels;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.Sarif.Viewer.Views
{
    internal class ResolveEmbeddedFileDialog : DialogWindow
    {
        public ResolveEmbeddedFileModel Result;

        public ResolveEmbeddedFileDialog(bool hasEmbeddedContent)
        {
            this.Title = Viewer.Resources.ConfirmSourceFileDialog_Title;
            this.SizeToContent = SizeToContent.WidthAndHeight;
            this.ResizeMode = ResizeMode.NoResize;

            this.Content = new ResolveEmbeddedFileDialogControl(
                new ResolveEmbeddedFileModel { HasEmbeddedContent = hasEmbeddedContent });

            this.Closed += this.ResolveEmbeddedFileDialog_Closed;
        }

        private void ResolveEmbeddedFileDialog_Closed(object sender, System.EventArgs e)
        {
            if (this.Content is ResolveEmbeddedFileDialogControl control &&
                control.DataContext is ResolveEmbeddedFileModel model)
            {
                this.Result = model;
            }
        }
    }
}

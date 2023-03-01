// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

using EnvDTE;

using EnvDTE80;

using Microsoft.Sarif.Viewer.Tags;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Tagging;

using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Sarif.Viewer.VisualStudio.Core.Models
{
    /// <summary>
    /// Wraps other render-able content in a Scroll viewer, preventing content from going off screen when possible.
    /// </summary>
    internal class ScrollViewerWrapper : IErrorTag
    {
        private readonly List<IErrorTag> objectsToWrap;

        public ScrollViewerWrapper(List<IErrorTag> objectsToWrap)
        {
            this.objectsToWrap = objectsToWrap;
        }

        public string ErrorType
        {
            get
            {
                if (this.objectsToWrap.Count == 0)
                {
                    return string.Empty;
                }

                return this.objectsToWrap[0].ErrorType;
            }
        }

        public object ToolTipContent
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                var stackPanel = new StackPanel();
                IEnumerable<object> y = this.objectsToWrap.Select(x => x.ToolTipContent);
                foreach (IErrorTag x in this.objectsToWrap)
                {
                    stackPanel.Children.Add((UIElement)x.ToolTipContent);
                }

                int maxHeight = 800;
                var dte = AsyncPackage.GetGlobalService(typeof(DTE)) as DTE2;
                if (dte != null && dte.MainWindow != null)
                {
                    maxHeight = dte.MainWindow.Height / 2;
                }

                ScrollViewer scrollViewer = new ScrollViewer()
                {
                    MaxHeight = maxHeight,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = stackPanel,
                };
                return scrollViewer;
            }
        }
    }
}

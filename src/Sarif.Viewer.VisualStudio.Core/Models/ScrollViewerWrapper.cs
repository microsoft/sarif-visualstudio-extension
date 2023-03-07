﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EnvDTE;
using EnvDTE80;
using Markdig.Wpf;
using Microsoft.Sarif.Viewer.Tags;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;

using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Sarif.Viewer.VisualStudio.Core.Models
{
    /// <summary>
    /// Wraps other render-able content in a Scroll viewer, preventing content from going off screen when possible.
    /// </summary>
    internal class ScrollViewerWrapper : IErrorTag
    {
        /// <summary>
        /// A collection of error tags that are to be wrapped and displayed in order.
        /// </summary>
        private readonly List<IErrorTag> objectsToWrap;

        private static readonly Dictionary<string, int> errorTypeToRank = new Dictionary<string, int>()
        {
            { PredefinedErrorTypeNames.SyntaxError, 1 },
            { PredefinedErrorTypeNames.CompilerError, 2 },
            { PredefinedErrorTypeNames.OtherError, 3 },
            { PredefinedErrorTypeNames.Warning, 4 },
            { PredefinedErrorTypeNames.Suggestion, 5 },
            { PredefinedErrorTypeNames.HintedSuggestion, 6 },
        };

        public ScrollViewerWrapper(List<IErrorTag> objectsToWrap)
        {
            this.objectsToWrap = objectsToWrap;
        }

        /// <summary>
        /// Gets the highest error type of the error tags that are wrapped.
        /// The order of severity is defined in <see cref="ScrollViewerWrapper.errorTypeToRank"/>.
        /// </summary>
        public string ErrorType
        {
            get
            {
                if (this.objectsToWrap.Count == 0)
                {
                    return string.Empty;
                }

                string highestErrorStr = PredefinedErrorTypeNames.HintedSuggestion;
                int highestRank = errorTypeToRank[highestErrorStr];
                foreach (IErrorTag errorTag in this.objectsToWrap)
                {
                    string errorType = errorTag.ErrorType;
                    int tagRank = 1;
                    if (errorTypeToRank.ContainsKey(errorType))
                    {
                        tagRank = errorTypeToRank[errorType];
                    }
                    else
                    {
                        tagRank = errorTypeToRank[PredefinedErrorTypeNames.OtherError];
                    }

                    if (tagRank < highestRank)
                    {
                        highestRank = tagRank;
                        highestErrorStr = errorType;
                    }
                }

                return highestErrorStr;
            }
        }

        /// <summary>
        /// Gets the content that is to be displayed to the end user. Will wrap all of the child content in a scroll viewer, and will scale the height to prevent content from goin off screen.
        /// </summary>
        public object ToolTipContent
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                var stackPanel = new StackPanel();
                foreach (IErrorTag objectToWrap in this.objectsToWrap)
                {
                    UIElement tooltip = (UIElement)objectToWrap.ToolTipContent;
                    stackPanel.Children.Add(tooltip);
                }

                int maxHeight = 800;
                var dte = AsyncPackage.GetGlobalService(typeof(DTE)) as DTE2;
                if (dte != null && dte.MainWindow != null)
                {
                    maxHeight = dte.MainWindow.Height / 3;
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
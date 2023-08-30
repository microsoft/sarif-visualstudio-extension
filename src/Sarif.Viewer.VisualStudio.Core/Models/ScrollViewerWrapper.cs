// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EnvDTE;
using EnvDTE80;
using Microsoft.Sarif.Viewer.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Tagging;

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

        private readonly ISarifViewerColorOptions sarifViewerOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScrollViewerWrapper"/> class.
        /// Creates a wrapper around the provided objects that need to be wrapped.
        /// </summary>
        /// <param name="objectsToWrap">The objects to be wrapped in a single scroll viewer.</param>
        /// <param name="sarifViewerOptions">The SarifViewerOptions that are currently being used.</param>
        public ScrollViewerWrapper(List<IErrorTag> objectsToWrap, ISarifViewerColorOptions sarifViewerOptions)
        {
            this.objectsToWrap = objectsToWrap;
            this.sarifViewerOptions = sarifViewerOptions;
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

                Dictionary<string, int> errorTypeToSeverity = GetRankDictionary();
                int highestRank = 3;
                string highestErrorStr = this.sarifViewerOptions.GetSelectedColorName("NoteUnderline");
                foreach (IErrorTag errorTag in this.objectsToWrap)
                {
                    string errorType = errorTag.ErrorType;
                    int tagRank = 3;
                    if (errorTypeToSeverity.ContainsKey(errorType))
                    {
                        tagRank = errorTypeToSeverity[errorType];
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
                Dictionary<string, int> errorTypeToSeverity = GetRankDictionary();

                List<IErrorTag> sortedObjects = this.objectsToWrap.OrderBy(x =>
                {
                    int rank;
                    if (!errorTypeToSeverity.TryGetValue(x.ErrorType, out rank))
                    {
                        rank = 3;
                    }

                    return rank;
                }).ToList();

                for (int i = 0; i < sortedObjects.Count; i++)
                {
                    IErrorTag objectToWrap = sortedObjects[i];
                    UIElement tooltip = (UIElement)objectToWrap.ToolTipContent;
                    if (i != 0 && tooltip is TextBlock textBlock)
                    {
                        textBlock.Margin = new Thickness(0, 20, 0, 0);
                    }

                    stackPanel.Children.Add(tooltip);
                }

                int maxHeight = 800;
                var dte = AsyncPackage.GetGlobalService(typeof(DTE)) as DTE2;
                if (dte != null && dte.MainWindow != null)
                {
                    maxHeight = dte.MainWindow.Height / 3;
                }

                var scrollViewer = new ScrollViewer()
                {
                    MaxHeight = maxHeight,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = stackPanel,
                };

/*                scrollViewer.Background = Brushes.Red;
                scrollViewer.Foreground = Brushes.Blue;*/

                // scrollViewer.Background = GetBrush(dte, vsThemeColors.vsThemeColorDesignerBackground);
                // scrollViewer.Background = GetBrush(dte, vsThemeColors.vsThemeColorEnvironmentBackground);
                // scrollViewer.Foreground = GetBrush(dte, vsThemeColors.vsThemeColorTitlebarActiveText);

                return scrollViewer;
            }
        }

        /// <summary>
        /// Returns a dictionary of error type name to the severity, with 1 being the most severe (error) and 3 being the least (note).
        /// </summary>
        /// <returns>A dictionary of error type name to the severity, with 1 being the most severe (error) and 3 being the least (note).</returns>
        private Dictionary<string, int> GetRankDictionary()
        {
            var errorTypeToSeverity = new Dictionary<string, int>(); // error type name -> severity rank (1 is error, 2 is warning, 3 is note)
            errorTypeToSeverity.Add(this.sarifViewerOptions.GetSelectedColorName("ErrorUnderline"), 1);

            // two underline colors can have the same color, we only care about the "highest" rank
            if (!errorTypeToSeverity.ContainsKey(this.sarifViewerOptions.GetSelectedColorName("WarningUnderline")))
            {
                errorTypeToSeverity.Add(this.sarifViewerOptions.GetSelectedColorName("WarningUnderline"), 2);
            }

            if (!errorTypeToSeverity.ContainsKey(this.sarifViewerOptions.GetSelectedColorName("NoteUnderline")))
            {
                errorTypeToSeverity.Add(this.sarifViewerOptions.GetSelectedColorName("NoteUnderline"), 3);
            }

            return errorTypeToSeverity;
        }
    }
}

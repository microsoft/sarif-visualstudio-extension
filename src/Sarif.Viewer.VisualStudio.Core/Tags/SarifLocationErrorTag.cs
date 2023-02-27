// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Windows;
using System.Windows.Controls;

using EnvDTE;
using EnvDTE80;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.Sarif.Viewer.Tags
{
    /// <summary>
    /// Contains the data necessary to display a error tag (a underlined squiggle with a tool tip)
    /// inside Visual Studio's text views.
    /// </summary>
    internal class SarifLocationErrorTag : SarifLocationTagBase, IErrorTag
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SarifLocationErrorTag"/> class.
        /// </summary>
        /// <param name="persistentSpan">The persistent span for the tag within a document.</param>
        /// <param name="runIndex">The SARIF run index associated with this tag.</param>
        /// <param name="resultId">the result ID associated with this tag.</param>
        /// <param name="errorType">The Visual Studio error type to display.</param>
        /// <param name="content">The content to use when displaying a tool tip for this error. This parameter may be null.</param>
        /// <param name="context">Gets the data context for this tag.</param>
        public SarifLocationErrorTag(IPersistentSpan persistentSpan, int runIndex, int resultId, string errorType, object content, object context)
            : base(persistentSpan, runIndex: runIndex, resultId: resultId, context: context)
        {
            this.ErrorType = errorType;
            this.Content = content;
        }

        /// <summary>
        /// Gets the Visual Studio error type to display.
        /// </summary>
        /// <remarks>
        /// The "error type" is basically the squiggle color. This may be null.
        /// </remarks>
        public string ErrorType { get; }

        /// <summary>
        /// Gets the content to use when displaying a tool tip for this error.
        /// </summary>
        /// <remarks>
        /// This may be null.
        /// </remarks>
        public object Content { get; }

        public object ToolTipContent
        {
            get
            {
                if (_toolTipContent != null)
                {
                    return _toolTipContent;
                }
                else
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
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
                        Content = Content,
                    };
                    _toolTipContent = scrollViewer;
                    return scrollViewer;
                }
            }
        }

        private object _toolTipContent;
    }
}

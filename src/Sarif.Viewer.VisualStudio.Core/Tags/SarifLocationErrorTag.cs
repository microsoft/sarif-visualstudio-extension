// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

using Sarif.Viewer.VisualStudio.Core;

namespace Microsoft.Sarif.Viewer.Tags
{
    /// <summary>
    /// Contains the data necessary to display a error tag (a underlined squiggle with a tool tip)
    /// inside Visual Studio's text views.
    /// </summary>
    internal class SarifLocationErrorTag : SarifLocationTagBase, IErrorTag
    {
        private readonly object toolTipString;
        private readonly string toolTipXamlString;
        private FrameworkElement xamlElement;

        /// <summary>
        /// Initializes a new instance of the <see cref="SarifLocationErrorTag"/> class.
        /// </summary>
        /// <param name="persistentSpan">The persistent span for the tag within a document.</param>
        /// <param name="runIndex">The SARIF run index associated with this tag.</param>
        /// <param name="resultId">the result ID associated with this tag.</param>
        /// <param name="errorType">The Visual Studio error type to display.</param>
        /// <param name="toolTipContent">The content to use when displaying a tool tip for this error. This parameter may be null.</param>
        /// <param name="toolTipXamlString">The string to be rendered as Xaml element when displaying a tool tip for this error. This parameter may be null.</param>
        /// <param name="context">Gets the data context for this tag.</param>
        public SarifLocationErrorTag(IPersistentSpan persistentSpan, int runIndex, int resultId, string errorType, object toolTipContent, string toolTipXamlString, object context)
            : base(persistentSpan, runIndex: runIndex, resultId: resultId, context: context)
        {
            this.ErrorType = errorType;
            this.toolTipString = toolTipContent;
            this.toolTipXamlString = toolTipXamlString;
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
        public object ToolTipContent
        {
            get
            {
                if (!SarifViewerPackage.IsUnitTesting)
                {
#pragma warning disable VSTHRD108 // Assert thread affinity unconditionally
                    ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning restore VSTHRD108
                }

                if (!string.IsNullOrWhiteSpace(toolTipXamlString) && this.xamlElement == null)
                {
                    FrameworkElement element = XamlUtilities.GetElementFromString(toolTipXamlString);
                    this.xamlElement = XamlUtilities.InstallHyperLinkerNavgiateEvent(element, Hyperlink_RequestNavigate);
                }

                if (this.xamlElement != null)
                {
                    // In practice this getter may be called multiple times whenever the user hovers over the region.
                    // If return same FrameworkElement after first time, will get an System.InvalidOperationException:
                    // Element already has a logical parent. It must be detached from the old parent before it is attached to a new one.
                    // So return a new ScrollView every time, which its child element is cached element.
                    return XamlUtilities.CreateScrollViewElement(this.xamlElement);
                }

                return this.toolTipString;
            }
        }

        // Event handler for HyperLink click event in a XAML element
        private static void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (sender is Hyperlink hyperlink && !string.IsNullOrWhiteSpace(hyperlink.NavigateUri?.AbsoluteUri))
            {
                SdkUIUtilities.OpenExternalUrl(hyperlink.NavigateUri.AbsoluteUri);
                e.Handled = true;
            }
        }
    }
}

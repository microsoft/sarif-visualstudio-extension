// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Tags
{
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Tagging;

    /// <summary>
    /// Contains the data necessary to display a error tag (a underlined squiggle with a tool tip)
    /// inside Visual Studio's text views.
    /// </summary>
    internal class SarifLocationErrorTag : SarifLocationTagBase, IErrorTag
    {
        /// <summary>
        /// Initialize a new instance of <see cref="SarifLocationErrorTag"/>.
        /// </summary>
        /// <param name="persistentSpan">The persistent span for the tag within a document.</param>
        /// <param name="runIndex">The SARIF run index associated with this tag.</param>
        /// <param name="resultId">the result ID associated with this tag.</param>
        /// <param name="errorType">The Visual Studio error type to display.</param>
        /// <param name="toolTipContent">The content to use when displaying a tool tip for this error. This parameter may be null.</param>
        /// <param name="context">Gets the data context for this tag.</param>
        public SarifLocationErrorTag(IPersistentSpan persistentSpan, int runIndex, int resultId, string errorType, object toolTipContent, object context)
            : base(persistentSpan, runIndex: runIndex, resultId: resultId, context: context)
        {
            this.ErrorType = errorType;
            this.ToolTipContent = toolTipContent;
        }

        /// <inheritdoc/>
        public string ErrorType { get; }

        /// <inheritdoc/>
        public object ToolTipContent { get; }
    }
}

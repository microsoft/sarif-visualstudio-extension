// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Sarif.Viewer.VisualStudio.Utilities
{
    /// <summary>
    /// Provides data for the event handler invoked when the last <see cref="ITextView"/> on an
    /// <see cref="ITextBuffer"/> is closed.
    /// </summary>
    public class LastViewRemovedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LastViewRemovedEventArgs"/> class.
        /// </summary>
        /// <param name="textBuffer">
        /// The <see cref="ITextBuffer"/> whose last <see cref="ITextView"> has been closed.
        /// </param>
        public LastViewRemovedEventArgs(ITextBuffer textBuffer)
        {
            this.TextBuffer = textBuffer;
        }

        /// <summary>
        /// Gets the <see cref="ITextBuffer"/> whose last <see cref="ITextView"/> was closed.
        /// </summary>
        public ITextBuffer TextBuffer { get; }
    }
}

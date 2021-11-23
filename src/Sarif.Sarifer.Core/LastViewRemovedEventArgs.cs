// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
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
        /// <param name="path">
        /// The path to the file whose contents are being viewed.
        /// </param>
        public LastViewRemovedEventArgs(string path)
        {
            this.Path = path;
        }

        /// <summary>
        /// Gets the path to the file whose contents are being viewed, or <c>null</c> if
        /// <see cref="TextBuffer"/> is not associated with a file.
        /// </summary>
        public string Path { get; }
    }
}

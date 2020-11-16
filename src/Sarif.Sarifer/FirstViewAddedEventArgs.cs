// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// Provides data for the event handler invoked when the first <see cref="ITextView"/> on an
    /// <see cref="ITextBuffer"/> is opened.
    /// </summary>
    public class FirstViewAddedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FirstViewAddedEventArgs"/> class.
        /// </summary>
        /// <param name="path">
        /// The path to the file whose contents are being viewed, or <code>null</code> if
        /// <paramref name="textBuffer"/> is not associated with a file.
        /// </param>
        /// <param name="text">
        /// The contents of the text buffer being viewed.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> that can be used to cancel analysis of the buffer
        /// being viewed.
        /// </param>
        public FirstViewAddedEventArgs(string path, string text, CancellationToken cancellationToken)
        {
            Path = path;
            Text = text;
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Gets the path to the file whose contents are being viewed, or <code>null</code> if
        /// <see cref="TextBuffer"/> is not associated with a file.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the contents of the text buffer being viewed.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Gets a <see cref="CancellationToken"/> that can be used to cancel analysis of the
        /// buffer being viewed.
        /// </summary>
        internal CancellationToken CancellationToken { get; }
    }
}

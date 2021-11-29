﻿// Copyright (c) Microsoft. All rights reserved.
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
        /// The path to the file whose contents are being viewed, or <c>null</c> if
        /// <paramref name="textBuffer"/> is not associated with a file.
        /// </param>
        /// <param name="text">
        /// The contents of the text buffer being viewed.
        /// </param>
        /// <param name="cancellationToken">
        /// The <see cref="System.Threading.CancellationToken"/>.
        /// </param>
        public FirstViewAddedEventArgs(string path, string text, CancellationToken cancellationToken)
        {
            this.Path = path;
            this.Text = text;
            this.CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Gets the path to the file whose contents are being viewed, or <c>null</c> if
        /// <see cref="TextBuffer"/> is not associated with a file.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the contents of the text buffer being viewed.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Gets the <see cref="System.Threading.CancellationToken"/>.
        /// </summary>
        public CancellationToken CancellationToken { get; }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// Interfaces exposed by objects that keep track of the set of <see cref="ITextView"/>s that
    /// are open on each tracked <see cref="ITextBuffer"/>, and notifies subscribers when the last
    /// view on a buffer is closed.
    /// </summary>
    public interface ITextBufferManager
    {
        /// <summary>
        /// Occurs when the last <see cref="ITextView"/> on an <see cref="ITextBuffer"/> is closed.
        /// </summary>
        event EventHandler<LastViewRemovedEventArgs> LastViewRemoved;

        /// <summary>
        /// Add a <see cref="ITextView"/> to the list of views for that view's
        /// <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="textView">
        /// The <see cref="ITextView"/> to be added.
        /// </param>
        void AddTextView(ITextView textView);

        /// <summary>
        /// Remove a <see cref="ITextView"/> frpm the list of views for that view's
        /// <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="textView">
        /// The <see cref="ITextView"/> to be removed.
        /// </param>
        void RemoveTextView(ITextView textView);
    }
}

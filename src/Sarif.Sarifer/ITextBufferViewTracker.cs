// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// Interfaces exposed by objects that keep track of the set of <see cref="ITextView"/>s that
    /// are open on each tracked <see cref="ITextBuffer"/>, and notifies subscribers when the first
    /// view on a buffer is open or the last view on a buffer is closed.
    /// </summary>
    public interface ITextBufferViewTracker
    {
        /// <summary>
        /// Occurs when the first <see cref="ITextView"/> on an <see cref="ITextBuffer"/> is opened.
        /// </summary>
        event EventHandler<FirstViewAddedEventArgs> FirstViewAdded;

        /// <summary>
        /// Occurs when the last <see cref="ITextView"/> on an <see cref="ITextBuffer"/> is closed.
        /// </summary>
        event EventHandler<LastViewRemovedEventArgs> LastViewRemoved;

        /// <summary>
        /// Occurs when the <see cref="ITextView"/>.<see cref="ITextBuffer"/> is updated.
        /// </summary>
        event EventHandler<ViewUpdatedEventArgs> ViewUpdated;

        /// <summary>
        /// Add a <see cref="ITextView"/> to the list of views for that view's
        /// <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="textView">
        /// The <see cref="ITextView"/> to be added.
        /// </param>
        /// <param name="path">
        /// The path to the file opened in text editor.
        /// </param>
        /// <param name="text">
        /// The text in text editor.
        /// </param>
        void AddTextView(ITextView textView, string path, string text);

        /// <summary>
        /// Remove a <see cref="ITextView"/> from the list of views for that view's
        /// <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="textView">
        /// The <see cref="ITextView"/> to be removed.
        /// </param>
        void RemoveTextView(ITextView textView);

        /// <summary>
        /// Update a <see cref="ITextView"/> to the list of views for that view's
        /// <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="textView">
        /// The <see cref="ITextView"/> to be updated.
        /// </param>
        /// <param name="path">
        /// The path to the file opened in text editor.
        /// </param>
        /// <param name="text">
        /// The text in text editor.
        /// </param>
        void UpdateTextView(ITextView textView, string path, string text);

        /// <summary>
        /// Clear all <see cref="ITextView"/>s tracked.
        /// </summary>
        void Clear();
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// Keeps track of the set of <see cref="ITextView"/>s that are open on each tracked
    /// <see cref="ITextBuffer"/>, and notifies subscribers when the last view on a buffer
    /// is closed.
    /// </summary>
    [Export(typeof(ITextBufferManager))]
    public class TextBufferManager : ITextBufferManager
    {
        private readonly IDictionary<ITextBuffer, List<ITextView>> bufferToViewsDictionary = new Dictionary<ITextBuffer, List<ITextView>>();

        public event EventHandler<LastViewRemovedEventArgs> LastViewRemoved;

        /// <inheritdoc/>
        public void AddTextView(ITextView textView)
        {
            textView = textView ?? throw new ArgumentNullException(nameof(textView));

            if (!this.bufferToViewsDictionary.TryGetValue(textView.TextBuffer, out List<ITextView> textViews))
            {
                textViews = new List<ITextView>();
                this.bufferToViewsDictionary.Add(textView.TextBuffer, textViews);
            }

            textViews.Add(textView);
        }

        /// <inheritdoc/>
        public void RemoveTextView(ITextView textView)
        {
            textView = textView ?? throw new ArgumentNullException(nameof(textView));

            if (!this.bufferToViewsDictionary.TryGetValue(textView.TextBuffer, out List<ITextView> textViews))
            {
                return;
            }

            textViews.Remove(textView);
            if (textViews.Count == 0)
            {
                this.bufferToViewsDictionary.Remove(textView.TextBuffer);
                LastViewRemoved?.Invoke(this, new LastViewRemovedEventArgs(textView.TextBuffer));
            }
        }
    }
}

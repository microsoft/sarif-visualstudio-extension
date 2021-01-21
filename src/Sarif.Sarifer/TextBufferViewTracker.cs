// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Sarif.Sarifer
{
    /// <summary>
    /// Keeps track of the set of <see cref="ITextView"/>s that are open on each tracked
    /// <see cref="ITextBuffer"/>, and notifies subscribers when the first view on a buffer
    /// is open or the last view on a buffer is closed.
    /// </summary>
    [Export(typeof(ITextBufferViewTracker))]
    public class TextBufferViewTracker : ITextBufferViewTracker
    {
        protected const int DefaultUpdateDelayInMS = 1500;

        private readonly ConcurrentDictionary<ITextBuffer, TextBufferViewTrackingInformation> bufferToViewsDictionary = new ConcurrentDictionary<ITextBuffer, TextBufferViewTrackingInformation>();

        /// <inheritdoc/>
        public event EventHandler<FirstViewAddedEventArgs> FirstViewAdded;

        /// <inheritdoc/>
        public event EventHandler<LastViewRemovedEventArgs> LastViewRemoved;

        /// <inheritdoc/>
        public event EventHandler<ViewUpdatedEventArgs> ViewUpdated;

        /// <inheritdoc/>
        public void AddTextView(ITextView textView, string path, string text)
        {
            bool first = false;
            textView = textView ?? throw new ArgumentNullException(nameof(textView));
            TextBufferViewTrackingInformation trackingInformation;

            if (!this.bufferToViewsDictionary.TryGetValue(textView.TextBuffer, out trackingInformation))
            {
                first = true;
                trackingInformation = new TextBufferViewTrackingInformation(path);
                this.bufferToViewsDictionary.AddOrUpdate(textView.TextBuffer, trackingInformation, (textBuffer, trackingInfor) => trackingInformation);
            }

            trackingInformation.Add(textView);
            if (first)
            {
                FirstViewAdded?.Invoke(this, new FirstViewAddedEventArgs(path, text, trackingInformation.CancellationTokenSource.Token));
            }
        }

        /// <inheritdoc/>
        public void RemoveTextView(ITextView textView)
        {
            textView = textView ?? throw new ArgumentNullException(nameof(textView));

            if (!this.bufferToViewsDictionary.TryGetValue(textView.TextBuffer, out TextBufferViewTrackingInformation trackingInformation))
            {
                return;
            }

            trackingInformation.Remove(textView);
            if (trackingInformation.Views.Count == 0)
            {
                this.bufferToViewsDictionary.TryRemove(textView.TextBuffer, out TextBufferViewTrackingInformation value);
                trackingInformation.CancellationTokenSource.Dispose();
                LastViewRemoved?.Invoke(
                    this,
                    new LastViewRemovedEventArgs(trackingInformation.Path));
            }
        }

        /// <inheritdoc/>
        public void UpdateTextView(ITextView textView, string path, string text)
        {
            textView = textView ?? throw new ArgumentNullException(nameof(textView));
            TextBufferViewTrackingInformation trackingInformation;

            if (!this.bufferToViewsDictionary.TryGetValue(textView.TextBuffer, out trackingInformation))
            {
                return;
            }

            ViewUpdated?.Invoke(this, new ViewUpdatedEventArgs(trackingInformation.Path, text, trackingInformation.CancellationTokenSource.Token));
        }

        /// <inheritdoc/>
        public void Clear()
        {
            foreach (KeyValuePair<ITextBuffer, TextBufferViewTrackingInformation> item in this.bufferToViewsDictionary)
            {
                item.Value.CancellationTokenSource.Cancel();
                item.Value.CancellationTokenSource.Dispose();
            }

            this.bufferToViewsDictionary.Clear();
        }
    }
}

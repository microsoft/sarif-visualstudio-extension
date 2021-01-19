// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;

using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.Sarif.Viewer.Tags
{
    /// <summary>
    /// This service provides consumers the ability to listen to the Visual Studio caret entering and leaving
    /// tags such as SARIF result error tags and call tree node tags.
    /// </summary>
    /// <typeparam name="T">Generic type of tag.</typeparam>
    [Export(typeof(ITextViewCaretListenerService<>))]
    internal class TextViewCaretListenerService<T> : ITextViewCaretListenerService<T>, IDisposable
        where T : ITag
    {
        /// <summary>
        /// Protects access to the <see cref="existingListeners"/> list.
        /// </summary>
        private readonly ReaderWriterLockSlimWrapper existingListenersLock = new ReaderWriterLockSlimWrapper(new ReaderWriterLockSlim());
        private readonly Dictionary<ITextView, TextViewCaretListener<T>> existingListeners = new Dictionary<ITextView, TextViewCaretListener<T>>();

        private bool isDisposed;

        /// <inheritdoc/>
        public event EventHandler<TagInCaretChangedEventArgs> CaretEnteredTag;

        /// <inheritdoc/>
        public event EventHandler<TagInCaretChangedEventArgs> CaretLeftTag;

        /// <inheritdoc/>
        public void CreateListener(ITextView textView, ITagger<T> tagger)
        {
            using (this.existingListenersLock.EnterUpgradeableReadLock())
            {
                if (this.existingListeners.ContainsKey(textView))
                {
                    return;
                }

                using (this.existingListenersLock.EnterWriteLock())
                {
                    var newTagger = new TextViewCaretListener<T>(textView, tagger);
                    this.existingListeners.Add(textView, newTagger);

                    newTagger.CaretEnteredTag += this.Tagger_CaretEnteredTag;
                    newTagger.CaretLeftTag += this.Tagger_CaretLeftTag;
                    textView.Closed += this.TextView_Closed;
                }
            }
        }

        private void Tagger_CaretEnteredTag(object sender, TagInCaretChangedEventArgs e)
        {
            this.CaretEnteredTag?.Invoke(this, e);
        }

        private void Tagger_CaretLeftTag(object sender, TagInCaretChangedEventArgs e)
        {
            this.CaretLeftTag?.Invoke(this, e);
        }

        private void TextView_Closed(object sender, EventArgs e)
        {
            if (sender is ITextView textView)
            {
                textView.Closed -= this.TextView_Closed;
                using (this.existingListenersLock.EnterWriteLock())
                {
                    if (this.existingListeners.TryGetValue(textView, out TextViewCaretListener<T> listener))
                    {
                        this.existingListeners.Remove(textView);

                        listener.CaretEnteredTag -= this.Tagger_CaretEnteredTag;
                        listener.CaretLeftTag -= this.Tagger_CaretLeftTag;
                    }
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;

            using (this.existingListenersLock.EnterWriteLock())
            {
                foreach (KeyValuePair<ITextView, TextViewCaretListener<T>> textViewAndTagger in this.existingListeners)
                {
                    textViewAndTagger.Value.CaretEnteredTag -= this.Tagger_CaretEnteredTag;
                    textViewAndTagger.Value.CaretLeftTag -= this.Tagger_CaretLeftTag;
                    textViewAndTagger.Key.Closed -= this.TextView_Closed;
                }

                this.existingListeners.Clear();
            }

            this.existingListenersLock.InnerLock.Dispose();
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

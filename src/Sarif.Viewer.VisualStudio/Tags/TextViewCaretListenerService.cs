// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Tags
{
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.Utilities;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Threading;

    /// <summary>
    /// This service provides consumers the ability to listen to the Visual Studio caret entering and leaving
    /// tags such as SARIF result error tags and call tree node tags.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Export(typeof(ITextViewCaretListenerService<>))]
    internal class TextViewCaretListenerService<T>: ITextViewCaretListenerService<T>, IDisposable
        where T: ITag
    {
        /// <summary>
        /// Protects access to the <see cref="ExistingListeners"/> list.
        /// </summary>
        private readonly ReaderWriterLockSlimWrapper ExistingListenersLock = new ReaderWriterLockSlimWrapper(new ReaderWriterLockSlim());
        private readonly Dictionary<ITextView, TextViewCaretListener<T>> ExistingListeners = new Dictionary<ITextView, TextViewCaretListener<T>>();

        private bool isDisposed;

        /// <inheritdoc/>
        public event EventHandler<CaretEventArgs> CaretEnteredTag;

        /// <inheritdoc/>
        public event EventHandler<CaretEventArgs> CaretLeftTag;

        /// <inheritdoc/>
        public void CreateListener(ITextView textView, ITagger<T> tagger)
        {
            using (this.ExistingListenersLock.EnterUpgradeableReadLock())
            {
                if (this.ExistingListeners.ContainsKey(textView))
                {
                    return;
                }

                using (ExistingListenersLock.EnterWriteLock())
                {
                    TextViewCaretListener<T> newTagger = new TextViewCaretListener<T>(textView, tagger);
                    this.ExistingListeners.Add(textView, newTagger);

                    newTagger.CaretEnteredTag += this.Tagger_CaretEnteredTag;
                    newTagger.CaretLeftTag += this.Tagger_CaretLeftTag;
                    textView.Closed += this.TextView_Closed;
                }
            }
        }

        private void Tagger_CaretEnteredTag(object sender, CaretEventArgs e)
        {
            this.CaretEnteredTag?.Invoke(this, e);
        }

        private void Tagger_CaretLeftTag(object sender, CaretEventArgs e)
        {
            this.CaretLeftTag?.Invoke(this, e);
        }

        private void TextView_Closed(object sender, EventArgs e)
        {
            if (sender is ITextView textView)
            {
                textView.Closed -= this.TextView_Closed;
                using (this.ExistingListenersLock.EnterWriteLock())
                {
                    if (this.ExistingListeners.TryGetValue(textView, out TextViewCaretListener<T> listener))
                    {
                        this.ExistingListeners.Remove(textView);

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

            using (ExistingListenersLock.EnterWriteLock())
            {
                foreach (KeyValuePair<ITextView, TextViewCaretListener<T>> textViewAndTagger in this.ExistingListeners)
                {
                    textViewAndTagger.Value.CaretEnteredTag += this.Tagger_CaretEnteredTag;
                    textViewAndTagger.Value.CaretLeftTag += this.Tagger_CaretLeftTag;
                    textViewAndTagger.Key.Closed -= this.TextView_Closed;
                }

                this.ExistingListeners.Clear();
            }

            this.ExistingListenersLock.InnerLock.Dispose();
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

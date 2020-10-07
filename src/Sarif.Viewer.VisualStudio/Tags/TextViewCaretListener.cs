// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Tags
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Handles listening to caret and layout updates to the text view in order
    /// to send notifications about the caret entering a tag.
    /// </summary>
    internal class TextViewCaretListener<T> : IDisposable
        where T: ITag
    {
        /// <summary>
        /// Protects access to the <see cref="SarifTaggers"/> list.
        /// </summary>
        private static readonly ReaderWriterLockSlimWrapper ExistingListenersLock = new ReaderWriterLockSlimWrapper(new ReaderWriterLockSlim());
        private static readonly Dictionary<ITextView, TextViewCaretListener<T>> ExistingListeners = new Dictionary<ITextView, TextViewCaretListener<T>>();

        private readonly TextMarkerTagCompaerer textMarkerTagCompaerer = new TextMarkerTagCompaerer();
        private readonly ITagger<T> tagger;
        private readonly ITextView textView;
        private List<ISarifLocationTag> previousTagsCaretWasIn;
        private bool isDisposed;

        public static void CreateListener(ITextView textView, ITagger<T> tagger)
        {
            using (ExistingListenersLock.EnterUpgradeableReadLock())
            {
                if (ExistingListeners.ContainsKey(textView))
                {
                    return;
                }

                using (ExistingListenersLock.EnterWriteLock())
                {
                    ExistingListeners.Add(textView, new TextViewCaretListener<T>(textView, tagger));
                }
            }
        }

        private TextViewCaretListener(ITextView textView, ITagger<T> tagger)
        {
            this.tagger = tagger;
            this.textView = textView;
            this.textView.Closed += TextView_Closed;
            this.textView.LayoutChanged += TextView_LayoutChanged;
            this.textView.Caret.PositionChanged += Caret_PositionChanged;
        }

        private void TextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            // If a new snapshot wasn't generated, then skip this layout
            if (e.NewViewState.EditSnapshot != e.OldViewState.EditSnapshot)
            {
                UpdateAtCaretPosition(textView.Caret.Position);
            }
        }

        private void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            if (e.OldPosition == e.NewPosition)
            {
                return;
            }

            UpdateAtCaretPosition(e.NewPosition);
        }

        private void UpdateAtCaretPosition(CaretPosition caretPosition)
        {
            SnapshotPoint caretSnapshotPoint = caretPosition.BufferPosition;

            NormalizedSnapshotSpanCollection normalizedSnapshotSpanCollection = new NormalizedSnapshotSpanCollection(new SnapshotSpan(caretSnapshotPoint, 1));

            List<ISarifLocationTag> tagsCaretIsCurrentlyIn = this.tagger.GetTags(normalizedSnapshotSpanCollection).
                Where(tag => tag.Tag is ISarifLocationTag).
                Select(tag => tag.Tag as ISarifLocationTag).
                ToList();

            if (this.previousTagsCaretWasIn != null && tagsCaretIsCurrentlyIn.SequenceEqual(this.previousTagsCaretWasIn, this.textMarkerTagCompaerer))
            {
                return;
            }

            IEnumerable<ISarifLocationTag> tagsToNotifyOfCaretEnter = tagsCaretIsCurrentlyIn.Where(tagCaretIsCurrentlyIn => this.previousTagsCaretWasIn == null || !this.previousTagsCaretWasIn.Contains(tagCaretIsCurrentlyIn, this.textMarkerTagCompaerer));
            IEnumerable<ISarifLocationTag> tagsToNotifyOfCaretLeave = this.previousTagsCaretWasIn?.Except(tagsToNotifyOfCaretEnter, this.textMarkerTagCompaerer);

            // Start an update batch in case the notifications cause a series of changes to tags. (Such as highlight colors).
            foreach (ISarifLocationTag tagToNotify in tagsToNotifyOfCaretEnter)
            {
                tagToNotify.NotifyCaretEntered();
            }

            if (tagsToNotifyOfCaretLeave != null)
            {
                foreach (ISarifLocationTag tagToNotify in tagsToNotifyOfCaretLeave)
                {
                    tagToNotify.NotifyCaretLeft();
                }
            }

            this.previousTagsCaretWasIn = tagsCaretIsCurrentlyIn;
        }

        private void TextView_Closed(object sender, EventArgs e)
        {
            this.UnsubscribeFromEvents();
        }

        private void UnsubscribeFromEvents()
        {
            this.textView.Closed -= this.TextView_Closed;
            this.textView.LayoutChanged -= this.TextView_LayoutChanged;
            this.textView.Caret.PositionChanged -= this.Caret_PositionChanged;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;
            if (disposing)
            {
                this.UnsubscribeFromEvents();

                using (ExistingListenersLock.EnterWriteLock())
                {
                    ExistingListeners.Remove(this.textView);
                }
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private class TextMarkerTagCompaerer : IEqualityComparer<ISarifLocationTag>
        {
            public bool Equals(ISarifLocationTag x, ISarifLocationTag y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                if (x.DocumentPersistentSpan.IsDocumentOpen != y.DocumentPersistentSpan.IsDocumentOpen)
                {
                    return false;
                }

                if (!x.DocumentPersistentSpan.FilePath.Equals(y.DocumentPersistentSpan.FilePath, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (x.DocumentPersistentSpan.TryGetSpan(out Span xSpan) && y.DocumentPersistentSpan.TryGetSpan(out Span ySpan))
                {
                    return xSpan == ySpan;
                }

                return false;
            }

            public int GetHashCode(ISarifLocationTag obj)
            {
                return obj.DocumentPersistentSpan.GetHashCode();
            }
        }
    }
}

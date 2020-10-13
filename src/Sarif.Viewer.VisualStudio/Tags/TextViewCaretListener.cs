// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Tags
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Tagging;

    /// <summary>
    /// Handles listening to caret and layout updates to the text view in order
    /// to send notifications about the caret entering a tag.
    /// </summary>
    internal class TextViewCaretListener<T>
        where T: ITag
    {
        private readonly TextMarkerTagCompaerer textMarkerTagCompaerer = new TextMarkerTagCompaerer();
        private readonly ITagger<T> tagger;
        private readonly ITextView textView;
        private List<ISarifLocationTag> previousTagsCaretWasIn;

        public TextViewCaretListener(ITextView textView, ITagger<T> tagger)
        {
            this.tagger = tagger;
            this.textView = textView;
            this.textView.Closed += TextView_Closed;
            this.textView.LayoutChanged += TextView_LayoutChanged;
            this.textView.Caret.PositionChanged += Caret_PositionChanged;
        }

        /// <summary>
        /// Fired when the Visual Studio caret enters a tag.
        /// </summary>
        public event EventHandler<TagInCaretChangedEventArgs> CaretEnteredTag;

        /// <summary>
        /// Fired when the Visual Studio caret leaves a tag.
        /// </summary>
        public event EventHandler<TagInCaretChangedEventArgs> CaretLeftTag;

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

            NormalizedSnapshotSpanCollection normalizedSnapshotSpanCollection = new NormalizedSnapshotSpanCollection(new SnapshotSpan(caretSnapshotPoint, caretSnapshotPoint));

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
                this.CaretEnteredTag?.Invoke(this, new TagInCaretChangedEventArgs(tagToNotify));
                (tagToNotify as ISarifLocationTagCaretNotify)?.OnCaretEntered();
            }

            if (tagsToNotifyOfCaretLeave != null)
            {
                foreach (ISarifLocationTag tagToNotify in tagsToNotifyOfCaretLeave)
                {
                    this.CaretLeftTag?.Invoke(this, new TagInCaretChangedEventArgs(tagToNotify));
                    (tagToNotify as ISarifLocationTagCaretNotify)?.OnCaretLeft();
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

        /// <summary>
        /// This class is used to check for equality based on the persistent span present
        /// in a tag. The tag reference itself can be different, but the tags can represent
        /// the same location. This is important when preventing multiple caret notifications.
        /// </summary>
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

                if (x.PersistentSpan.IsDocumentOpen != y.PersistentSpan.IsDocumentOpen)
                {
                    return false;
                }

                if (!x.PersistentSpan.FilePath.Equals(y.PersistentSpan.FilePath, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (x.PersistentSpan.TryGetSpan(out Span xSpan) && y.PersistentSpan.TryGetSpan(out Span ySpan))
                {
                    return xSpan == ySpan;
                }

                return false;
            }

            public int GetHashCode(ISarifLocationTag obj)
            {
                return obj.PersistentSpan.GetHashCode();
            }
        }
    }
}

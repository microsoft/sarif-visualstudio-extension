// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.Sarif.Viewer.Tags
{
    /// <summary>
    /// Handles listening to caret and layout updates to the text view in order
    /// to send notifications about the caret entering a tag.
    /// </summary>
    /// <typeparam name="T">Generic type of tag.</typeparam>
    internal class TextViewCaretListener<T>
        where T : ITag
    {
        private readonly TextMarkerTagCompaerer textMarkerTagCompaerer = new TextMarkerTagCompaerer();
        private readonly ITagger<T> tagger;
        private readonly ITextView textView;
        private List<ISarifLocationTag> previousTagsCaretWasIn;

        public TextViewCaretListener(ITextView textView, ITagger<T> tagger)
        {
            this.tagger = tagger;
            this.textView = textView;
            this.textView.Closed += this.TextView_Closed;
            this.textView.LayoutChanged += this.TextView_LayoutChanged;
            this.textView.Caret.PositionChanged += this.Caret_PositionChanged;
            this.textView.GotAggregateFocus += this.TextView_GotAggregateFocus;
            this.textView.LostAggregateFocus += this.TextView_LostAggregateFocus;

            // Send an update of the initial caret position for this text view.
            // This allows the SARIF explorer to properly receive the caret
            // entered notifications for a newly created text view.
            // This allows the SARIF explorer to proper set selection to items
            // on initial load.
            ThreadHelper.JoinableTaskFactory.RunAsync(() => this.UpdateInitialCaretPositionAsync());
        }

        /// <summary>
        /// Fired when the Visual Studio caret enters a tag.
        /// </summary>
        public event EventHandler<TagInCaretChangedEventArgs> CaretEnteredTag;

        /// <summary>
        /// Fired when the Visual Studio caret leaves a tag.
        /// </summary>
        public event EventHandler<TagInCaretChangedEventArgs> CaretLeftTag;

        private async System.Threading.Tasks.Task UpdateInitialCaretPositionAsync()
        {
            // When ThreadHelper.JoinableTaskFactory.RunAsync is invoked, if the current thread
            // is the main UI thread, then it runs this task synchronously.
            // What we want is for the constructor above to return and the consumer
            // of the new object to be able to subscribe to events before the initial
            // caret position update (and its corresponding events) are sent.
            // So we yield to allow the constructor call stack to unwind before sending
            // the initial caret position events.
            await System.Threading.Tasks.Task.Yield();

            this.UpdateAtCaretPosition(this.textView.Caret.Position);
        }

        private void TextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            // If a new snapshot wasn't generated, then skip this layout
            if (e.NewViewState.EditSnapshot != e.OldViewState.EditSnapshot)
            {
                this.UpdateAtCaretPosition(this.textView.Caret.Position);
            }
        }

        private void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            if (e.OldPosition == e.NewPosition)
            {
                return;
            }

            this.UpdateAtCaretPosition(e.NewPosition);
        }

        private void TextView_GotAggregateFocus(object sender, EventArgs e)
        {
            this.UpdateAtCaretPosition(this.textView.Caret.Position);
        }

        private void TextView_LostAggregateFocus(object sender, EventArgs e)
        {
            this.UpdateAtCaretPosition(this.textView.Caret.Position);
        }

        private void UpdateAtCaretPosition(CaretPosition caretPosition)
        {
            SnapshotPoint caretSnapshotPoint = caretPosition.BufferPosition;

            var normalizedSnapshotSpanCollection = new NormalizedSnapshotSpanCollection(new SnapshotSpan(start: caretSnapshotPoint, end: caretSnapshotPoint));

            // Handling the aggregate focus allows tags to have their highlights removed when the focus moves from
            // one document to another rather than having a bunch of views with highlights that don't correspond
            // with what's being selected in the solution explorer.
            var tagsCaretIsCurrentlyIn = (this.textView.HasAggregateFocus
                ? this.tagger.GetTags(normalizedSnapshotSpanCollection).
                    Where(tag => tag.Tag is ISarifLocationTag).
                    Select(tag => tag.Tag as ISarifLocationTag)
                : Enumerable.Empty<ISarifLocationTag>()).ToList();

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
            this.textView.GotAggregateFocus -= this.TextView_GotAggregateFocus;
            this.textView.LostAggregateFocus -= this.TextView_LostAggregateFocus;
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

                if (x == null || y == null || x.PersistentSpan == null || y.PersistentSpan == null)
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

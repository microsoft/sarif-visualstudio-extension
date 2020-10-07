// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Tags
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Handles listening to caret and layout updates to the text view in order
    /// to send notifications about the caret entering a tag.
    /// </summary>
    internal class TextViewCaretListener : IDisposable
    {
        private readonly ITextView textView;
        private List<ISarifLocationTag> previousTagsCaretWasIn;
        private bool disposedValue;
        private readonly List<ISarifLocationTag> sarifTags;
        private readonly ISarifLocationTagger2 tagger;

        public TextViewCaretListener(ISarifLocationTagger2 tagger, ITextView textView, List<ISarifLocationTag> sarifTags)
        {
            this.textView = textView;
            this.sarifTags = sarifTags;
            this.textView.Closed += TextView_Closed;
            this.textView.LayoutChanged += TextView_LayoutChanged;
            this.textView.Caret.PositionChanged += Caret_PositionChanged;
            this.tagger = tagger;
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
            UpdateAtCaretPosition(e.NewPosition);
        }

        private void UpdateAtCaretPosition(CaretPosition caretPosition)
        {
            List<ISarifLocationTag> tagsCaretIsCurrentlyIn;
            if (!this.sarifTags.Any())
            {
                return;
            }

            // Keep track of the tags the caret is in now, versus the tags
            // that the caret was previously in. (Yes, there can be multiple tags per text range).
            // This is done so we don't keep re-issuing caret entered notifications while
            // the user is moving the caret around the editor.

            SnapshotPoint caretSnapshotPoint = caretPosition.BufferPosition;
            tagsCaretIsCurrentlyIn = this.sarifTags.
                Where(currentTag => currentTag.DocumentPersistentSpan.Span != null).
                Where(currentTag => currentTag.DocumentPersistentSpan.Span.GetSpan(caretSnapshotPoint.Snapshot).Contains(caretSnapshotPoint)).
                OrderBy(currentTag => currentTag.DocumentPersistentSpan.Span.GetSpan(caretSnapshotPoint.Snapshot).Length).
                ToList();

            IEnumerable<ISarifLocationTag> tagsToNotifyOfCaretEnter = tagsCaretIsCurrentlyIn.Where(tagCaretIsCurrentlyIn => this.previousTagsCaretWasIn == null || !this.previousTagsCaretWasIn.Contains(tagCaretIsCurrentlyIn));
            IEnumerable<ISarifLocationTag> tagsToNotifyOfCaretLeave = previousTagsCaretWasIn?.Except(tagsToNotifyOfCaretEnter);

            // Start an update batch in case the notifications cause a series of changes to tags. (Such as highlight colors).
            foreach (ISarifLocationTag tagToNotify in tagsToNotifyOfCaretEnter)
            {
                tagToNotify.NotifyCaretEntered();
            }

            foreach (ISarifLocationTag tagToNotify in tagsToNotifyOfCaretLeave)
            {
                tagToNotify.NotifyCaretLeft();
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
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.UnsubscribeFromEvents();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

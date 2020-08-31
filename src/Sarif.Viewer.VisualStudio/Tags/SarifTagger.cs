// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Tags
{
    using Microsoft.CodeAnalysis.Sarif;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.TextManager.Interop;
    using Microsoft.VisualStudio.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    internal class SarifTagger : ITagger<TextMarkerTag>, ISarifTagger, ITextViewCreationListener, IDisposable
    {
        private static ReaderWriterLockSlimWrapper tagListLock = new ReaderWriterLockSlimWrapper(new ReaderWriterLockSlim());
        private static readonly Dictionary<string, List<SarifTag>> SourceCodeFileToSarifTags = new Dictionary<string, List<SarifTag>>();

        private readonly ReaderWriterLockSlimWrapper batchUpdateLock = new ReaderWriterLockSlimWrapper(new ReaderWriterLockSlim());
        private ITrackingSpan batchUpdateSpan;

        private readonly ITextBuffer textBuffer;
        private readonly IPersistentSpanFactory persistentSpanFactory;
        private readonly string fileName;
        private int updateCount;
        private bool disposed;

        public SarifTagger(ITextBuffer textBuffer, IPersistentSpanFactory persistentSpanFactory)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!SdkUIUtilities.TryGetFileNameFromTextBuffer(textBuffer, out string textBufferFilename))
            {
                throw new ArgumentException("Always expect to be able to get file name from text buffer.", nameof(textBuffer));
            }

            this.fileName = textBufferFilename;
            this.textBuffer = textBuffer;
            this.persistentSpanFactory = persistentSpanFactory;

            // Subscribe to property changed event on any existing tags.
            // We subscribe so we can properly send change events to VS
            // when things like the tag color are changed.
            using (tagListLock.EnterReadLock())
            {
                if (SourceCodeFileToSarifTags.TryGetValue(fileName, out List<SarifTag> sarifTags))
                {
                    foreach(var sarifTag in sarifTags)
                    {
                        sarifTag.PropertyChanged += this.SarifTagPropertyChanged;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        /// <inheritdoc/>
        public ISarifTag AddTag(Region sourceRegion, TextSpan documentSpan, TextMarkerTag tag)
        {
            using (this.Update())
            {
                using (tagListLock.EnterUpgradeableReadLock())
                {
                    if (SourceCodeFileToSarifTags.TryGetValue(fileName, out List<SarifTag> sarifTags))
                    {
                        SarifTag existingSarifTag = SourceCodeFileToSarifTags[this.fileName].FirstOrDefault(
                            (sarifTag) =>
                                sarifTag.SourceRegion.ValueEquals(sourceRegion));

                        if (existingSarifTag != null)
                        {
                            return existingSarifTag;
                        }
                    }

                    using (tagListLock.EnterWriteLock())
                    {
                        IPersistentSpan persistentSpan = this.persistentSpanFactory.Create(
                            this.textBuffer.CurrentSnapshot,
                            startLine: documentSpan.iStartLine,
                            startIndex: documentSpan.iStartIndex,
                            endLine: documentSpan.iEndLine,
                            endIndex: documentSpan.iEndIndex,
                            SpanTrackingMode.EdgeInclusive);

                        SarifTag newSarifTag = new SarifTag(
                            persistentSpan,
                            sourceRegion,
                            textMarkerTag: tag);

                        if (sarifTags == null)
                        {
                            sarifTags = new List<SarifTag>();
                            SourceCodeFileToSarifTags[this.fileName] = sarifTags;
                        }

                        sarifTags.Add(newSarifTag);
                        newSarifTag.PropertyChanged += SarifTagPropertyChanged;

                        this.UpdateBatchSpan(newSarifTag.DocumentPersistentSpan.Span);

                        return newSarifTag;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public bool TryGetTag(Region sourceRegion, out ISarifTag existingTag)
        {
            using (tagListLock.EnterReadLock())
            {
                if (!SourceCodeFileToSarifTags.TryGetValue(fileName, out List<SarifTag> sarifTags))
                {
                    existingTag = null;
                    return false;
                }

                existingTag = SourceCodeFileToSarifTags[this.fileName].FirstOrDefault(sarifTag => sarifTag.SourceRegion.ValueEquals(sourceRegion));
            }

            return existingTag != null;
        }

        /// <inheritdoc/>
        public void RemoveTag(ISarifTag tag)
        {
            using (this.Update())
            {
                using (tagListLock.EnterWriteLock())
                {
                    if (tag is SarifTag sarifTag && 
                        SourceCodeFileToSarifTags.TryGetValue(sarifTag.DocumentPersistentSpan.FilePath, out List<SarifTag> sarifTags) &&
                        sarifTags.Remove(sarifTag))
                    {
                        sarifTag.PropertyChanged -= this.SarifTagPropertyChanged;
                        sarifTag.Dispose();

                        this.UpdateBatchSpan(sarifTag.DocumentPersistentSpan.Span);
                    }
                }
            }
        }

        public IEnumerable<ITagSpan<TextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
            {
                yield break;
            }

            SarifTag[] possibleTags = null;
            using (tagListLock.EnterReadLock())
            {
                if (SourceCodeFileToSarifTags.TryGetValue(this.fileName, out List<SarifTag> sarifTags))
                {
                    possibleTags = new SarifTag[sarifTags.Count];
                    sarifTags.CopyTo(possibleTags, 0);
                }
            }

            if (possibleTags == null)
            {
                yield break;
            }

            foreach (var span in spans)
            {
                foreach (var possibleTag in possibleTags.Where((possibleTag) => possibleTag.DocumentPersistentSpan.Span != null))
                {
                    SnapshotSpan possibleTagSnapshotSpan = possibleTag.DocumentPersistentSpan.Span.GetSpan(span.Snapshot);
                    if (span.IntersectsWith(possibleTagSnapshotSpan))
                    {
                        yield return new TagSpan<TextMarkerTag>(possibleTagSnapshotSpan, possibleTag.Tag);
                    }
                }
            }
        }

        public IDisposable Update()
        {
            return new BatchUpdate(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            if (disposing)
            {
                using (tagListLock.EnterReadLock())
                {
                    // Important note that we do not dispose or clear
                    // the SARIF tag list as that would destroy the whole purpose of using
                    // "persistent spans" which survive open and close of a document within
                    // a VS session.
                    if (SourceCodeFileToSarifTags.TryGetValue(this.fileName, out List<SarifTag> sarifTags))
                    {
                        foreach (var sarifTag in sarifTags)
                        {
                            sarifTag.PropertyChanged -= this.SarifTagPropertyChanged;
                        }
                    }
                }

                this.batchUpdateLock.InnerLock.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void SarifTagPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is SarifTag sarifTag)
            {
                using (this.Update())
                {
                    this.UpdateBatchSpan(sarifTag.DocumentPersistentSpan.Span);
                }
            }
        }

        private void UpdateBatchSpan(ITrackingSpan snapshotSpan)
        {
            // If there currently is a batch span, update it to include the biggest
            // range of buffer affected so far.
            if (this.batchUpdateSpan == null)
            {
                this.batchUpdateSpan = snapshotSpan;
                return;
            }

            ITextSnapshot snapshot = this.textBuffer.CurrentSnapshot;

            SnapshotSpan currentBatchSpan = this.batchUpdateSpan.GetSpan(snapshot);
            SnapshotSpan currentUpdate = snapshotSpan.GetSpan(snapshot);

            SnapshotPoint newStart = currentBatchSpan.Start.Position < currentUpdate.Start.Position ? currentBatchSpan.Start : currentUpdate.Start;
            SnapshotPoint newEnd = currentBatchSpan.End.Position > currentUpdate.End.Position ? currentBatchSpan.End : currentUpdate.End;

            this.batchUpdateSpan = snapshot.CreateTrackingSpan(new SnapshotSpan(newStart, newEnd), this.batchUpdateSpan.TrackingMode);
        }

        public void TextViewCreated(ITextView textView)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!SdkUIUtilities.TryGetFileNameFromTextBuffer(textView.TextBuffer, out string textViewFileName))
            {
                return;
            }

            if (!textViewFileName.Equals(this.fileName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            new TextViewCaretListener(textView, this);
        }

        private class TextViewCaretListener
        {
            private readonly ITextView textView;
            private readonly SarifTagger tagger;
            private List<SarifTag> previousTagsCaretWasIn;

            public TextViewCaretListener(ITextView textView, SarifTagger tagger)
            {
                this.textView = textView;
                this.tagger = tagger;
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
                UpdateAtCaretPosition(e.NewPosition);
            }

            private void UpdateAtCaretPosition(CaretPosition caretPoisition)
            {
                SarifTag[] possibleTags = null;
                using (tagListLock.EnterReadLock())
                {
                    if (SourceCodeFileToSarifTags.TryGetValue(this.tagger.fileName, out List<SarifTag> sarifTags))
                    {
                        possibleTags = new SarifTag[sarifTags.Count];
                        sarifTags.CopyTo(possibleTags, 0);
                    }
                }

                if (possibleTags != null)
                {
                    // Keep track of the tags the caret is in now, versus the tags
                    // that the caret was previously in. (Yes, there can be multiple tags per text range).
                    // This is done so we don't keep re-issuing caret entered notifications while
                    // the user is moving the caret around the editor.
                    var tagsCaretIsCurrentlyIn = new List<SarifTag>();

                    foreach (var possibleTag in possibleTags)
                    {
                        SnapshotPoint caretBufferPosition = caretPoisition.BufferPosition;
                        if (possibleTag.DocumentPersistentSpan.Span.GetSpan(caretBufferPosition.Snapshot).Contains(caretBufferPosition))
                        {
                            tagsCaretIsCurrentlyIn.Add(possibleTag);
                        }
                    }

                    foreach (SarifTag tagCaretIsCurrentlyIn in tagsCaretIsCurrentlyIn)
                    {
                        if (this.previousTagsCaretWasIn == null || !this.previousTagsCaretWasIn.Contains(tagCaretIsCurrentlyIn))
                        {
                            tagCaretIsCurrentlyIn.RaiseCaretEnteredTag();
                        }
                    }

                    this.previousTagsCaretWasIn = tagsCaretIsCurrentlyIn;
                }
            }

            private void TextView_Closed(object sender, EventArgs e)
            {
                this.textView.Closed -= this.TextView_Closed;
                this.textView.LayoutChanged -= this.TextView_LayoutChanged;
                this.textView.Caret.PositionChanged -= this.TextView_Closed;
            }
        }

        private class BatchUpdate : IDisposable
        {
            private readonly SarifTagger tagger;
            public BatchUpdate(SarifTagger tagger)
            {
                this.tagger = tagger;
                using (this.tagger.batchUpdateLock.EnterWriteLock())
                {
                    if (Interlocked.Increment(ref tagger.updateCount) == 0)
                    {
                        this.tagger.batchUpdateSpan = null;
                    }
                }
            }

            public void Dispose()
            {
                if (Interlocked.Decrement(ref tagger.updateCount) == 0 &&
                    this.tagger.batchUpdateSpan != null)
                {
                    this.tagger.TagsChanged?.Invoke(this.tagger, new SnapshotSpanEventArgs(this.tagger.batchUpdateSpan.GetSpan(this.tagger.textBuffer.CurrentSnapshot)));
                }
            }
        }
    }
}

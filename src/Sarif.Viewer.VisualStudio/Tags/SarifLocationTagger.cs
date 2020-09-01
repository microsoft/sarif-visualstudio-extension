// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Tags
{
    using Microsoft.CodeAnalysis.Sarif;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.TextManager.Interop;
    using Microsoft.VisualStudio.Utilities;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading;

    internal class SarifLocationTagger : ITagger<TextMarkerTag>, ISarifLocationTagger, ITextViewCreationListener, IDisposable
    {
        /// <summary>
        /// Protects access to the <see cref="SourceCodeFileToSarifTags"/> and <see cref="RunIdToSarifTags"/> dictionaries.
        /// </summary>
        private static ReaderWriterLockSlimWrapper TagListLock = new ReaderWriterLockSlimWrapper(new ReaderWriterLockSlim());

        /// <summary>
        /// Provides a dictionary from source code files (C/C++, etc.) to a set of tags to display in the VS editor.
        /// </summary>
        /// <remarks>
        /// This is a static instance as a "tagger" is created based on an opened text buffer but the tags" persist beyond that instance
        /// of a tagger and will be re-used if the text buffer is re-opened. (For example in a file close and re-open scenario).
        /// </remarks>
        private static readonly Dictionary<string, List<SarifLocationTag>> SourceCodeFileToSarifTags = new Dictionary<string, List<SarifLocationTag>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Provides a dictionary from SARIF log run Id to a set of tags to display in the VS editor.
        /// </summary>
        /// <remarks>
        /// This is a static instance as a "tagger" is created based on an opened text buffer but the tags" persist beyond that instance
        /// of a tagger and will be re-used if the text buffer is re-opened. (For example in a file close and re-open scenario).
        /// </remarks>
        private static readonly Dictionary<int, List<SarifLocationTag>> RunIdToSarifTags = new Dictionary<int, List<SarifLocationTag>>();

        /// <summary>
        /// Protects access to the <see cref="SarifTaggers"/> list.
        /// </summary>
        private static readonly ReaderWriterLockSlimWrapper SarifTaggersLock = new ReaderWriterLockSlimWrapper(new ReaderWriterLockSlim());

        /// <summary>
        /// This list of running taggers.
        /// </summary>
        private static readonly List<SarifLocationTagger> SarifTaggers = new List<SarifLocationTagger>();

        /// <summary>
        /// Protects access to the <see cref="batchUpdateSpan"/> when batch updates to tags are being performed.
        /// </summary>
        private readonly ReaderWriterLockSlimWrapper batchUpdateLock = new ReaderWriterLockSlimWrapper(new ReaderWriterLockSlim());

        /// <summary>
        /// When batch updates are being performed, contains the all inclusive span for all the tags that were modified.
        /// </summary>
        private ITrackingSpan batchUpdateSpan;

        /// <summary>
        /// Used to track nested calls to <see cref="Update"/>.
        /// </summary>
        private int updateCount;

        private readonly ITextBuffer textBuffer;
        private readonly IPersistentSpanFactory persistentSpanFactory;
        private readonly string fileName;
        private List<SarifLocationTag> sarifTags;

        private bool disposed;

        /// <summary>
        /// Create an instance of the SARIF issue tagger.
        /// </summary>
        /// <param name="textBuffer">The Visual Studio text buffer that the tagger will be associated with.</param>
        /// <param name="persistentSpanFactory">The persistent span factory that will be used to create individual tags.</param>
        public SarifLocationTagger(ITextBuffer textBuffer, IPersistentSpanFactory persistentSpanFactory)
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
            using (TagListLock.EnterWriteLock())
            {
                // Go ahead and grab a reference to the SARIF tags this tagger actually
                // cares about. If the list doesn't exist, then create an empty one
                // so the remainder of this code doesn't have to reason about
                // potential null checks.
                if (!SourceCodeFileToSarifTags.TryGetValue(fileName, out this.sarifTags))
                {
                    this.sarifTags = new List<SarifLocationTag>();
                    SourceCodeFileToSarifTags[fileName] = this.sarifTags;
                }

                foreach(var sarifTag in this.sarifTags)
                {
                    sarifTag.PropertyChanged += this.SarifTagPropertyChanged;
                }
            }

            using (SarifTaggersLock.EnterWriteLock())
            {
                SarifTaggers.Add(this);
            }
        }

        /// <inheritdoc/>
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        /// <summary>
        /// Removes tags for a given SARIF log file run ID.
        /// </summary>
        /// <param name="runId">The run ID to remove.</param>
        public static void RemoveAllTagsForRun(int runId)
        {
            // First, we need to let any running taggers know to remove their tags.
            // Note that taggers are only running if there is a "Text view"
            // opened for the source code file that contains a result (tag).
            List<SarifLocationTagger> runningTaggers;
            using (SarifTaggersLock.EnterReadLock())
            {
                runningTaggers = new List<SarifLocationTagger>(SarifTaggers);
            }

            foreach (SarifLocationTagger runningTagger in runningTaggers)
            {
                runningTagger.RemoveTagsForRun(runId);
            }

            // Next, remove any remaining tags for the run ID from the static lists.
            using (TagListLock.EnterWriteLock())
            {
                if (!RunIdToSarifTags.TryGetValue(runId, out List<SarifLocationTag> sarifTagsForRun) ||
                    sarifTagsForRun.Count == 0)
                {
                    return;
                }

                foreach (SarifLocationTag sarifTagForRun in sarifTagsForRun)
                {
                    if (SourceCodeFileToSarifTags.TryGetValue(sarifTagForRun.DocumentPersistentSpan.FilePath, out List<SarifLocationTag> sarifTagsForSourceFile))
                    {
                        sarifTagsForSourceFile.Remove(sarifTagForRun);
                    }

                    sarifTagForRun.Dispose();
                }

                sarifTagsForRun.Clear();
            }
        }

        /// <summary>
        /// Removes all tags.
        /// </summary>
        public static void RemoveAllTags()
        {
            IEnumerable<int> runsToRemove;
            using (TagListLock.EnterReadLock())
            {
                runsToRemove = RunIdToSarifTags.Keys.ToList();
            }

            foreach (int runId in runsToRemove)
            {
                RemoveAllTagsForRun(runId);
            }
        }

            /// <inheritdoc/>
        public ISarifLocationTag AddTag(Region sourceRegion, TextSpan documentSpan, int runId, TextMarkerTag tag)
        {
            // Start an update so that even on a call (where the caller is not batching using Update themselves)
            // that a "tags changed" event is fired to update VS's editor.
            using (this.Update())
            {
                using (TagListLock.EnterWriteLock())
                {
                    SarifLocationTag existingSarifTag = this.sarifTags.FirstOrDefault(
                        (sarifTag) =>
                            sarifTag.SourceRegion.ValueEquals(sourceRegion));

                    if (existingSarifTag != null)
                    {
                        return existingSarifTag;
                    }

                    IPersistentSpan persistentSpan = this.persistentSpanFactory.Create(
                        this.textBuffer.CurrentSnapshot,
                        startLine: documentSpan.iStartLine,
                        startIndex: documentSpan.iStartIndex,
                        endLine: documentSpan.iEndLine,
                        endIndex: documentSpan.iEndIndex,
                        trackingMode: SpanTrackingMode.EdgeInclusive);

                    SarifLocationTag newSarifTag = new SarifLocationTag(
                        persistentSpan,
                        sourceRegion,
                        runId,
                        tag);

                    this.sarifTags.Add(newSarifTag);
                    newSarifTag.PropertyChanged += SarifTagPropertyChanged;

                    if (RunIdToSarifTags.TryGetValue(runId, out List<SarifLocationTag> sarifTagsForRun))
                    {
                        sarifTagsForRun.Add(newSarifTag);
                    }
                    else
                    {
                        RunIdToSarifTags.Add(runId, new List<SarifLocationTag> { newSarifTag });
                    }

                    this.UpdateBatchSpan(newSarifTag.DocumentPersistentSpan.Span);

                    return newSarifTag;
                }
            }
        }

        /// <inheritdoc/>
        public bool TryGetTag(Region sourceRegion, out ISarifLocationTag existingTag)
        {
            using (TagListLock.EnterReadLock())
            {
                existingTag = this.sarifTags.FirstOrDefault(sarifTag => sarifTag.SourceRegion.ValueEquals(sourceRegion));
            }

            return existingTag != null;
        }

        /// <inheritdoc/>
        public void RemoveTag(ISarifLocationTag tag)
        {
            if (!(tag is SarifLocationTag sarifTag))
            {
                return;
            }

            using (this.Update())
            {
                using (TagListLock.EnterWriteLock())
                {
                    if (this.sarifTags.Remove(sarifTag))
                    {
                        this.UpdateBatchSpan(sarifTag.DocumentPersistentSpan.Span);

                        // We do not need TryGetValue here because if it exists in the SARIF tags list
                        // it must exist in the run Id to SARIF tag map. If it doesn't, it means the lists
                        // are out of sync which should never happen and is bad.
                        RunIdToSarifTags[sarifTag.RunId].Remove(sarifTag);

                        sarifTag.PropertyChanged -= this.SarifTagPropertyChanged;
                        sarifTag.Dispose();
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void RemoveTagsForRun(int runId)
        {
            List<SarifLocationTag> tagsToRemove;

            using (TagListLock.EnterReadLock())
            {
                if (!RunIdToSarifTags.TryGetValue(runId, out List<SarifLocationTag> sarifTagsForRun) ||
                    sarifTagsForRun.Count == 0)
                {
                    return;
                }

                // Copy so we can update (which can make outgoing callse) outside of lock
                tagsToRemove = new List<SarifLocationTag>(sarifTagsForRun);
            }

            using (this.Update())
            {
                foreach (SarifLocationTag tagToRemove in tagsToRemove)
                {
                    this.RemoveTag(tagToRemove);
                }
            }
        }

        /// <inheritdoc/>
        public IEnumerable<ITagSpan<TextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
            {
                yield break;
            }

            SarifLocationTag[] possibleTags = null;
            using (TagListLock.EnterReadLock())
            {
                if (this.sarifTags == null)
                {
                    yield break;
                }

                possibleTags = new SarifLocationTag[this.sarifTags.Count];
                this.sarifTags.CopyTo(possibleTags, 0);
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

        /// <inheritdoc/>
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
                using (TagListLock.EnterReadLock())
                {
                    // Important note that we do not dispose or clear
                    // the SARIF tag list as that would destroy the whole purpose of using
                    // "persistent spans" which survive open and close of a document within
                    // a VS session.
                    foreach (var sarifTag in this.sarifTags)
                    {
                        sarifTag.PropertyChanged -= this.SarifTagPropertyChanged;
                    }
                }

                using (SarifTaggersLock.EnterWriteLock())
                {
                    SarifTaggers.Remove(this);
                }

                this.batchUpdateLock.InnerLock.Dispose();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void SarifTagPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is SarifLocationTag sarifTag)
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

        /// <inheritdoc/>
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

            new TextViewCaretListener(textView, this.sarifTags);
        }

        /// <summary>
        /// Used to control batch changes to tags.
        /// </summary>
        /// <remarks>
        /// When this object is disposed by the caller and all batch updates have completed, a 
        /// tags changed event is sent to Visual Studio.
        /// </remarks>
        private class BatchUpdate : IDisposable
        {
            private readonly SarifLocationTagger tagger;
            public BatchUpdate(SarifLocationTagger tagger)
            {
                this.tagger = tagger;
                Interlocked.Increment(ref tagger.updateCount);
            }

            public void Dispose()
            {
                ITrackingSpan tagsChangedSpan = null;

                using (this.tagger.batchUpdateLock.EnterWriteLock())
                {
                    if (Interlocked.Decrement(ref tagger.updateCount) == 0)
                    {
                        tagsChangedSpan = this.tagger.batchUpdateSpan;
                        this.tagger.batchUpdateSpan = null;
                    }
                }

                if (tagsChangedSpan != null)
                {
                    this.tagger.TagsChanged?.Invoke(this.tagger, new SnapshotSpanEventArgs(tagsChangedSpan.GetSpan(this.tagger.textBuffer.CurrentSnapshot)));
                }
            }
        }

        /// <summary>
        /// Handles listening to caret and layout updates to the text view in order
        /// to send notifications about the caret entering a tag.
        /// </summary>
        private class TextViewCaretListener
        {
            private readonly ITextView textView;
            private List<SarifLocationTag> previousTagsCaretWasIn;
            private readonly List<SarifLocationTag> sarifTags;

            public TextViewCaretListener(ITextView textView, List<SarifLocationTag> sarifTags)
            {
                this.textView = textView;
                this.sarifTags = sarifTags;
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
                List<SarifLocationTag> possibleTags = null;
                using (TagListLock.EnterReadLock())
                {
                    if (this.sarifTags.Count == 0)
                    {
                        return;
                    }

                    possibleTags = new List<SarifLocationTag>(sarifTags);
                }

                // Keep track of the tags the caret is in now, versus the tags
                // that the caret was previously in. (Yes, there can be multiple tags per text range).
                // This is done so we don't keep re-issuing caret entered notifications while
                // the user is moving the caret around the editor.
                var tagsCaretIsCurrentlyIn = new List<SarifLocationTag>();

                foreach (var possibleTag in possibleTags)
                {
                    SnapshotPoint caretBufferPosition = caretPoisition.BufferPosition;
                    if (possibleTag.DocumentPersistentSpan.Span.GetSpan(caretBufferPosition.Snapshot).Contains(caretBufferPosition))
                    {
                        tagsCaretIsCurrentlyIn.Add(possibleTag);
                    }
                }

                foreach (SarifLocationTag tagCaretIsCurrentlyIn in tagsCaretIsCurrentlyIn)
                {
                    if (this.previousTagsCaretWasIn == null || !this.previousTagsCaretWasIn.Contains(tagCaretIsCurrentlyIn))
                    {
                        tagCaretIsCurrentlyIn.RaiseCaretEnteredTag();
                    }
                }

                this.previousTagsCaretWasIn = tagsCaretIsCurrentlyIn;
            }

            private void TextView_Closed(object sender, EventArgs e)
            {
                this.textView.Closed -= this.TextView_Closed;
                this.textView.LayoutChanged -= this.TextView_LayoutChanged;
                this.textView.Caret.PositionChanged -= this.TextView_Closed;
            }
        }
    }
}

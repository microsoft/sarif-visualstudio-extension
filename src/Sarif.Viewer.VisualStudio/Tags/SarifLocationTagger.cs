// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Tags
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading;
    using Microsoft.CodeAnalysis.Sarif;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.TextManager.Interop;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Handles adding, removing, and tagging SARIF locations in a text buffer for Visual Studio integration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The SARIF location tagger is created per instance of a Visual Studio <see cref="ITextView"/>. The tagger is disposed
    /// when the text view is no longer used. However, the underlying "tags" are associated with a text buffer
    /// which can outlast an <see cref="ITextView"/>. For instance, a split window view on the same document.
    /// That results in multiple <see cref="ITextView"/> instances against one underlying <see cref="ITextBuffer"/>.
    /// There are static dictionaries in this class that instances use to retrieve
    /// existing tag information to present back to Visual Studio. As an example, if file "foo.c" is opened a text buffer
    /// is created, and an instance of this class is created and tags may be added. If file "foo.c" is then closed, this instance
    /// of the tagger is disposed, but the static list of tags remains in the dictionaries. If file "foo.c" is the re-opened,
    /// a new text buffer and tagger instance is created but re-tagging of the document is no longer necessary as the tagger
    /// reconnects to the existing data.
    /// </para>
    /// <para>
    /// A note about Visual Studio's <see cref="ITrackingSpan.GetSpan(ITextSnapshot)"/> method:
    /// "GetSpan" is not really a great name. What is actually happening
    /// is the "Span" that "GetSpan" is called on is "mapped" onto the passed in
    /// text snapshot. In essence what this means is take the "persistent span"
    /// that we have and "replay" any edits that have occurred and return a new
    /// span. So, if the span is no longer relevant (lets say the text has been deleted)
    /// then you'll get back an empty span.
    /// </para>
    /// </remarks>
    internal class SarifLocationTagger : ITagger<TextMarkerTag>, ISarifLocationTagger, IDisposable
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
        private int batchUpdateNestingLevel;

        private readonly IPersistentSpanFactory persistentSpanFactory;
        private readonly string filePath;
        
        /// <summary>
        /// References the list of <see cref="SarifLocationTag"/> objects within <see cref="SourceCodeFileToSarifTags"/>.
        /// </summary>
        /// <remarks>
        /// When a <see cref="SarifLocationTagger"/> is constructed, it retrieves a reference to the SARIF location tag list
        /// from <see cref="SourceCodeFileToSarifTags"/> so that it does not have to retrieve the list on every method call
        /// thereby making it more efficient.
        /// </remarks>
        private List<SarifLocationTag> sarifTags;

        private bool disposed;

        /// <summary>
        /// Create an instance of the <see cref="SarifLocationTagger"/>.
        /// </summary>
        /// <param name="textView">The text view that is displaying the provided <paramref name="textBuffer"/>.</param>
        /// <param name="textBuffer">The Visual Studio text buffer that the tagger will be associated with.</param>
        /// <param name="persistentSpanFactory">The persistent span factory that will be used to create individual tags.</param>
        public SarifLocationTagger(ITextView textView, ITextBuffer textBuffer, IPersistentSpanFactory persistentSpanFactory)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!SdkUIUtilities.TryGetFileNameFromTextBuffer(textBuffer, out this.filePath))
            {
                throw new ArgumentException("Always expect to be able to get file name from text buffer.", nameof(textBuffer));
            }

            this.TextBuffer = textBuffer;
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
                if (!SourceCodeFileToSarifTags.TryGetValue(filePath, out this.sarifTags))
                {
                    this.sarifTags = new List<SarifLocationTag>();
                    SourceCodeFileToSarifTags[filePath] = this.sarifTags;
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

            // Start listening to caret position changes so we can send events
            // that ultimately result in selections occurring in the SARIF explorer
            // tool window.
            new TextViewCaretListener(textView, this.sarifTags);
        }

        /// <inheritdoc/>
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        /// <summary>
        /// Gets the text buffer associated with this tagger.
        /// </summary>
        public ITextBuffer TextBuffer { get; }

        /// <summary>
        /// Tries to find a location tagger associated with the given <see cref="ITextBuffer"/>.
        /// </summary>
        /// <remarks>
        /// There could be multiple taggers active for one text buffer since they are created per <see cref="ITextView"/>
        /// and multiple views can be displaying the same <see cref="ITextBuffer"/>. That is why this
        /// implementation can return any tagger.</remarks>
        /// <param name="textBuffer">The text buffer that will be used to locate an instance of <see cref="SarifLocationTagger"/>.</param>
        /// <param name="sarifLocationTagger">On success, returns the instance of <see cref="SarifLocationTagger"/> associated with the specified <paramref name="textBuffer"/>.</param>
        /// <returns>Returns true if an instance of <see cref="SarifLocationTagger"/> can be found.</returns>
        public static bool TryFindTaggerForBuffer(ITextBuffer textBuffer, out SarifLocationTagger sarifLocationTagger)
        {
            using (SarifTaggersLock.EnterReadLock())
            {
                sarifLocationTagger = SarifTaggers.FirstOrDefault(sarifTagger => sarifTagger.TextBuffer == textBuffer);
            }

            return sarifLocationTagger != null;
        }

        /// <summary>
        /// Removes tags for a given SARIF run index.
        /// </summary>
        /// <param name="runIndex">The run index to remove.</param>
        public static void RemoveAllTagsForRun(int runIndex)
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
                runningTagger.RemoveTagsForRun(runIndex);
            }

            // Next, remove any remaining tags for the run ID from the static lists.
            using (TagListLock.EnterWriteLock())
            {
                if (!RunIdToSarifTags.TryGetValue(runIndex, out List<SarifLocationTag> sarifTagsForRun) ||
                    sarifTagsForRun.Count == 0)
                {
                    return;
                }

                foreach (SarifLocationTag sarifTag in sarifTagsForRun)
                {
                    if (SourceCodeFileToSarifTags.TryGetValue(sarifTag.DocumentPersistentSpan.FilePath, out List<SarifLocationTag> sarifTagsForSourceFile))
                    {
                        sarifTagsForSourceFile.Remove(sarifTag);
                    }

                    sarifTag.Dispose();
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

        /// <summary>
        /// Cleans up the static locks and lists.
        /// </summary>
        public static void DisposeStaticObjects()
        {
            List<SarifLocationTagger> taggersToDispose;
            using (SarifTaggersLock.EnterWriteLock())
            {
                taggersToDispose = new List<SarifLocationTagger>(SarifTaggers);
                SarifTaggers.Clear();
            }

            foreach (SarifLocationTagger sarifLocationTagger in taggersToDispose)
            {
                sarifLocationTagger.Dispose();
            }

            // Note that the lock wrapper implementation does not support IDispose
            // and therefore we still must dispose the "inner lock" held by the wrapper.
            TagListLock.InnerLock.Dispose();
            SarifTaggersLock.InnerLock.Dispose();
        }

        /// <inheritdoc/>
        public ISarifLocationTag AddTag(Region sourceRegion, TextSpan documentSpan, int runId, TextMarkerTag tag)
        {
            // Since it is possible we are already in a nested update call, we use the update semantics
            // in "Add tag" so that this method does not fire a changed tags event to visual studio
            // unless all nesting is complete.
            using (this.Update())
            {
                using (TagListLock.EnterWriteLock())
                {
                    SarifLocationTag existingSarifTag = this.sarifTags.FirstOrDefault(
                        (sarifTag) =>
                            sarifTag.SourceRegion.ValueEquals(sourceRegion)
                            && sarifTag.RunIndex == runId);

                    if (existingSarifTag != null)
                    {
                        return existingSarifTag;
                    }

                    IPersistentSpan persistentSpan = this.persistentSpanFactory.Create(
                        this.TextBuffer.CurrentSnapshot,
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

                    if (!RunIdToSarifTags.TryGetValue(runId, out List<SarifLocationTag> sarifTagsForRun))
                    {
                        sarifTagsForRun = new List<SarifLocationTag>();
                        RunIdToSarifTags.Add(runId, sarifTagsForRun);
                    }

                    sarifTagsForRun.Add(newSarifTag);

                    this.UpdateBatchSpan(newSarifTag.DocumentPersistentSpan.Span);

                    return newSarifTag;
                }
            }
        }

        /// <inheritdoc/>
        public bool TryGetTag(Region sourceRegion, int runIndex, out ISarifLocationTag existingTag)
        {
            using (TagListLock.EnterReadLock())
            {
                existingTag = this.sarifTags.FirstOrDefault(sarifTag =>
                    sarifTag.SourceRegion.ValueEquals(sourceRegion) &&
                    runIndex == sarifTag.RunIndex);
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

            // Since it is possible we are already in a nested update call, we use the update semantics
            // in "remove tag" so that this method does not fire a changed tags event to visual studio
            // unless all nesting is complete.
            using (this.Update())
            {
                using (TagListLock.EnterWriteLock())
                {
                    if (this.sarifTags.Remove(sarifTag))
                    {
                        sarifTag.PropertyChanged -= this.SarifTagPropertyChanged;

                        this.UpdateBatchSpan(sarifTag.DocumentPersistentSpan.Span);

                        // We do not need TryGetValue here because if it exists in the SARIF tags list
                        // it must exist in the run Id to SARIF tag map. If it doesn't, it means the lists
                        // are out of sync which should never happen and is bad.
                        RunIdToSarifTags[sarifTag.RunIndex].Remove(sarifTag);

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

                // Copy so we can update (which can make outgoing calls) outside of lock.
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

            IEnumerable<SarifLocationTag> currentTags = null;
            using (TagListLock.EnterReadLock())
            {
                if (this.sarifTags == null)
                {
                    yield break;
                }

                currentTags = new List<SarifLocationTag>(this.sarifTags);
            }

            if (currentTags == null)
            {
                yield break;
            }

            foreach (SnapshotSpan span in spans)
            {
                foreach (var possibleTag in currentTags.Where(possibleTag => possibleTag.DocumentPersistentSpan.Span != null))
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
                // Important note that we do not dispose or clear
                // the SARIF tag list as that would destroy the whole purpose of using
                // "persistent spans" which survive open and close of a document within
                // a VS session.
                using (TagListLock.EnterReadLock())
                {
                    foreach (SarifLocationTag sarifLocationTag in this.sarifTags)
                    {
                        sarifLocationTag.PropertyChanged -= this.SarifTagPropertyChanged;
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
            using (this.batchUpdateLock.EnterWriteLock())
            {
                if (this.batchUpdateSpan == null)
                {
                    this.batchUpdateSpan = snapshotSpan;
                    return;
                }

                ITextSnapshot snapshot = this.TextBuffer.CurrentSnapshot;

                SnapshotSpan batchSpan = this.batchUpdateSpan.GetSpan(snapshot);
                SnapshotSpan updateSpan = snapshotSpan.GetSpan(snapshot);

                SnapshotPoint newStart = batchSpan.Start.Position < updateSpan.Start.Position ? batchSpan.Start : updateSpan.Start;
                SnapshotPoint newEnd = batchSpan.End.Position > updateSpan.End.Position ? batchSpan.End : updateSpan.End;

                // The tracking mode used here will match the tracking mode of the persistent span that was created in AddTag.
                this.batchUpdateSpan = snapshot.CreateTrackingSpan(new SnapshotSpan(newStart, newEnd), this.batchUpdateSpan.TrackingMode);
            }
        }

        /// <summary>
        /// Used to control batch changes to tags.
        /// </summary>
        /// <remarks>
        /// When this object is disposed by the caller and the <see cref="batchUpdateNestingLevel"/>
        /// reaches zero, tags changed event is sent to Visual Studio.
        /// </remarks>
        private class BatchUpdate : IDisposable
        {
            private readonly SarifLocationTagger tagger;
            public BatchUpdate(SarifLocationTagger tagger)
            {
                this.tagger = tagger;
                Interlocked.Increment(ref tagger.batchUpdateNestingLevel);
            }

            public void Dispose()
            {
                ITrackingSpan tagsChangedSpan = null;

                using (this.tagger.batchUpdateLock.EnterWriteLock())
                {
                    if (Interlocked.Decrement(ref tagger.batchUpdateNestingLevel) == 0)
                    {
                        tagsChangedSpan = this.tagger.batchUpdateSpan;
                        this.tagger.batchUpdateSpan = null;
                    }
                }

                if (tagsChangedSpan != null)
                {
                    this.tagger.TagsChanged?.Invoke(this.tagger, new SnapshotSpanEventArgs(tagsChangedSpan.GetSpan(this.tagger.TextBuffer.CurrentSnapshot)));
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

            private void UpdateAtCaretPosition(CaretPosition caretPosition)
            {
                List<SarifLocationTag> currentTags = null;
                using (TagListLock.EnterReadLock())
                {
                    if (this.sarifTags.Count == 0)
                    {
                        return;
                    }

                    currentTags = new List<SarifLocationTag>(sarifTags);
                }

                // Keep track of the tags the caret is in now, versus the tags
                // that the caret was previously in. (Yes, there can be multiple tags per text range).
                // This is done so we don't keep re-issuing caret entered notifications while
                // the user is moving the caret around the editor.
                var tagsCaretIsCurrentlyIn = new List<SarifLocationTag>();

                SnapshotPoint caretSnapshotPoint = caretPosition.BufferPosition;
                foreach (var currentTag in currentTags.Where(currentTag => currentTag.DocumentPersistentSpan.Span != null))
                {
                    if (currentTag.DocumentPersistentSpan.Span.GetSpan(caretSnapshotPoint.Snapshot).Contains(caretSnapshotPoint))
                    {
                        tagsCaretIsCurrentlyIn.Add(currentTag);
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
                this.textView.Caret.PositionChanged -= this.Caret_PositionChanged;
            }
        }
    }
}

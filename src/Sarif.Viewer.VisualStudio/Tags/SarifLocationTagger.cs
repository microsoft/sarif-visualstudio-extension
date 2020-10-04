// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Tags
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading;
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
    internal class SarifLocationTagger : ITagger<ITextMarkerTag>, ITagger<IErrorTag>, ISarifLocationTagger, IDisposable
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
        private static readonly Dictionary<string, List<ISarifLocationTag>> SourceCodeFileToSarifTags = new Dictionary<string, List<ISarifLocationTag>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Provides a dictionary from SARIF log run Id to a set of tags to display in the VS editor.
        /// </summary>
        /// <remarks>
        /// This is a static instance as a "tagger" is created based on an opened text buffer but the tags" persist beyond that instance
        /// of a tagger and will be re-used if the text buffer is re-opened. (For example in a file close and re-open scenario).
        /// </remarks>
        private static readonly Dictionary<int, List<ISarifLocationTag>> RunIdToSarifTags = new Dictionary<int, List<ISarifLocationTag>>();

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
        /// References the list of <see cref="ISarifLocationTag"/> objects within <see cref="SourceCodeFileToSarifTags"/>.
        /// </summary>
        /// <remarks>
        /// When a <see cref="SarifLocationTagger"/> is constructed, it retrieves a reference to the SARIF location tag list
        /// from <see cref="SourceCodeFileToSarifTags"/> so that it does not have to retrieve the list on every method call
        /// thereby making it more efficient.
        /// </remarks>
        private List<ISarifLocationTag> sarifTags;

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
                    this.sarifTags = new List<ISarifLocationTag>();
                    SourceCodeFileToSarifTags[filePath] = this.sarifTags;
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
                if (!RunIdToSarifTags.TryGetValue(runIndex, out List<ISarifLocationTag> sarifTagsForRun) ||
                    sarifTagsForRun.Count == 0)
                {
                    return;
                }

                foreach (SarifLocationTextMarkerTag sarifTag in sarifTagsForRun)
                {
                    if (SourceCodeFileToSarifTags.TryGetValue(sarifTag.DocumentPersistentSpan.FilePath, out List<ISarifLocationTag> sarifTagsForSourceFile))
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
        public ISarifLocationTextMarkerTag AddTextMarkerTag(TextSpan documentSpan, int runIndex, string textMarkerTagType)
        {
            return this.AddTag<ISarifLocationTextMarkerTag>(documentSpan, runIndex, persistentSpan => new SarifLocationTextMarkerTag(
                        persistentSpan,
                        this.TextBuffer,
                        runIndex,
                        textMarkerTagType));
        }

        /// <summary>
        /// Adds a tag to report to visual studio.
        /// </summary>
        /// <param name="documentSpan">The span to use to create the tag relative to an open document.</param>
        /// <param name="runIndex">The SARIF run index associated with this tag.</param>
        /// <param name="errorType">The error type as defined by <see cref="Microsoft.VisualStudio.Text.Adornments.PredefinedErrorTypeNames"/>.</param>
        /// <param name="tooltipContent">The tool tip content to display in Visual studio.</param>
        /// <returns>Returns a new instance of <see cref="ISarifLocationTag"/></returns>
        /// <remarks>
        /// This <paramref name="documentSpan"/>is not necessarily the same as <paramref name="sourceRegion"/>.
        /// It may have been modified to fix up column and line numbers from the region
        /// present in the SARIF log.
        /// </remarks>
        public ISarifLocationErrorTag AddErrorTag(TextSpan documentSpan, int runIndex, string errorType, object tooltipContent)
        {
            return this.AddTag<ISarifLocationErrorTag>(documentSpan, runIndex, persistentSpan => new SarifLocationErrorTag(
                        persistentSpan,
                        this.TextBuffer,
                        runIndex,
                        errorType,
                        tooltipContent));
        }

        private T AddTag<T>(TextSpan documentSpan, int runId, Func<IPersistentSpan, ISarifLocationTag> createTag)
            where T: ISarifLocationTag
        {
            // Since it is possible we are already in a nested update call, we use the update semantics
            // in "Add tag" so that this method does not fire a changed tags event to visual studio
            // unless all nesting is complete.
            using (this.Update())
            {
                using (TagListLock.EnterWriteLock())
                {
                    IPersistentSpan persistentSpan = this.persistentSpanFactory.Create(
                        this.TextBuffer.CurrentSnapshot,
                        startLine: documentSpan.iStartLine,
                        startIndex: documentSpan.iStartIndex,
                        endLine: documentSpan.iEndLine,
                        endIndex: documentSpan.iEndIndex,
                        trackingMode: SpanTrackingMode.EdgeInclusive);

                    ISarifLocationTag newSarifTag = createTag(persistentSpan);

                    this.sarifTags.Add(newSarifTag);

                    if (!RunIdToSarifTags.TryGetValue(runId, out List<ISarifLocationTag> sarifTagsForRun))
                    {
                        sarifTagsForRun = new List<ISarifLocationTag>();
                        RunIdToSarifTags.Add(runId, sarifTagsForRun);
                    }

                    sarifTagsForRun.Add(newSarifTag);

                    this.UpdateBatchSpan(newSarifTag.DocumentPersistentSpan.Span);

                    return (T)newSarifTag;
                }
            }
        }

        /// <inheritdoc/>
        public void RemoveTag(ISarifLocationTag sarifTag)
        {
            // Since it is possible we are already in a nested update call, we use the update semantics
            // in "remove tag" so that this method does not fire a changed tags event to visual studio
            // unless all nesting is complete.
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
                        RunIdToSarifTags[sarifTag.RunIndex].Remove(sarifTag);

                        sarifTag.Dispose();
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void RemoveTagsForRun(int runId)
        {
            List<ISarifLocationTag> tagsToRemove;

            using (TagListLock.EnterReadLock())
            {
                if (!RunIdToSarifTags.TryGetValue(runId, out List<ISarifLocationTag> sarifTagsForRun) ||
                    sarifTagsForRun.Count == 0)
                {
                    return;
                }

                // Copy so we can update (which can make outgoing calls) outside of lock.
                tagsToRemove = new List<ISarifLocationTag>(sarifTagsForRun);
            }

            using (this.Update())
            {
                foreach (ISarifLocationTag tagToRemove in tagsToRemove)
                {
                    this.RemoveTag(tagToRemove);
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

        private IEnumerable<ITagSpan<T>> GetSarifLocationTags<T>(NormalizedSnapshotSpanCollection spans) where T: ITag
        {
            if (spans.Count == 0)
            {
                yield break;
            }

            IEnumerable<ISarifLocationTag> currentTags = null;
            using (TagListLock.EnterReadLock())
            {
                if (this.sarifTags == null)
                {
                    yield break;
                }

                currentTags = new List<ISarifLocationTag>(this.sarifTags.Where(sarifTag => sarifTag is T));
            }

            if (currentTags == null || !currentTags.Any())
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
                        yield return new TagSpan<T>(possibleTagSnapshotSpan, (T)possibleTag);
                    }
                }
            }
        }

        private void SarifTagPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is SarifLocationTextMarkerTag sarifTag)
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

        IEnumerable<ITagSpan<ITextMarkerTag>> ITagger<ITextMarkerTag>.GetTags(NormalizedSnapshotSpanCollection spans)
        {
            return this.GetSarifLocationTags<ITextMarkerTag>(spans);
        }

        IEnumerable<ITagSpan<IErrorTag>> ITagger<IErrorTag>.GetTags(NormalizedSnapshotSpanCollection spans)
        {
            return this.GetSarifLocationTags<IErrorTag>(spans);
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
            private List<ISarifLocationTag> previousTagsCaretWasIn;
            private readonly List<ISarifLocationTag> sarifTags;

            public TextViewCaretListener(ITextView textView, List<ISarifLocationTag> sarifTags)
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
                List<ISarifLocationTag> currentTags = null;
                using (TagListLock.EnterReadLock())
                {
                    if (this.sarifTags.Count == 0)
                    {
                        return;
                    }

                    currentTags = new List<ISarifLocationTag>(sarifTags);
                }

                // Keep track of the tags the caret is in now, versus the tags
                // that the caret was previously in. (Yes, there can be multiple tags per text range).
                // This is done so we don't keep re-issuing caret entered notifications while
                // the user is moving the caret around the editor.
                var tagsCaretIsCurrentlyIn = new List<ISarifLocationTag>();

                SnapshotPoint caretSnapshotPoint = caretPosition.BufferPosition;
                foreach (var currentTag in currentTags.Where(currentTag => currentTag.DocumentPersistentSpan.Span != null))
                {
                    if (currentTag.DocumentPersistentSpan.Span.GetSpan(caretSnapshotPoint.Snapshot).Contains(caretSnapshotPoint))
                    {
                        tagsCaretIsCurrentlyIn.Add(currentTag);
                    }
                }

                foreach (ISarifLocationTag tagCaretIsCurrentlyIn in tagsCaretIsCurrentlyIn.Where(tag => tag is SarifLocationTextMarkerTag))
                {
                    if (this.previousTagsCaretWasIn == null || !this.previousTagsCaretWasIn.Contains(tagCaretIsCurrentlyIn))
                    {
                        ((SarifLocationTextMarkerTag)tagCaretIsCurrentlyIn).RaiseCaretEnteredTag();
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

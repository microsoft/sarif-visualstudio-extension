﻿// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Tags
{
    using Microsoft.CodeAnalysis.Sarif;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.TextManager.Interop;
    using Microsoft.VisualStudio.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    internal class SarifTagger : ITagger<TextMarkerTag>, ISarifTagger, IDisposable
    {
        private static ReaderWriterLockSlimWrapper tagListLock = new ReaderWriterLockSlimWrapper(new ReaderWriterLockSlim());
        private static readonly Dictionary<string, List<SarifTag>> FileToSarifTags = new Dictionary<string, List<SarifTag>>();

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
            if (!textBuffer.Properties.TryGetProperty(typeof(IVsTextBuffer), out IVsTextBuffer vsTextBuffer))
            {
                throw new ArgumentException("Always expect to be able to get file name from text buffer.", nameof(textBuffer));
            }

            IPersistFileFormat persistFileFormat = vsTextBuffer as IPersistFileFormat;
            if (persistFileFormat == null)
            {
                throw new ArgumentException("Always expect to be able to get file name from text buffer.", nameof(textBuffer));
            }

            if (persistFileFormat.GetCurFile(out string fileName, out uint formatIndex) != VSConstants.S_OK)
            {
                throw new ArgumentException("Always expect to be able to get file name from text buffer.", nameof(textBuffer));
            }

            this.fileName = fileName;
            this.textBuffer = textBuffer;
            this.persistentSpanFactory = persistentSpanFactory;

            // Subscribe to property changed event on any existing tag.
            using (tagListLock.EnterReadLock())
            {
                List<SarifTag> sarifTags = null;
                if (FileToSarifTags.TryGetValue(fileName, out sarifTags))
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
                    List<SarifTag> sarifTags = null;
                    if (FileToSarifTags.TryGetValue(fileName, out sarifTags))
                    {
                        SarifTag existingSarifTag = FileToSarifTags[this.fileName].FirstOrDefault(
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
                            FileToSarifTags[this.fileName] = sarifTags;
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
                List<SarifTag> sarifTags = null;
                if (!FileToSarifTags.TryGetValue(fileName, out sarifTags))
                {
                    existingTag = null;
                    return false;
                }

                existingTag = FileToSarifTags[this.fileName].FirstOrDefault(
                    (sarifTag) =>
                        sarifTag.SourceRegion.ValueEquals(sourceRegion));
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
                    if (tag is SarifTag sarifTag && FileToSarifTags.TryGetValue(sarifTag.DocumentPersistentSpan.FilePath, out List<SarifTag> sarifTags))
                    {
                        sarifTags.Remove(sarifTag);
                        sarifTag.PropertyChanged -= this.SarifTagPropertyChanged;
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
                if (FileToSarifTags.TryGetValue(this.fileName, out List<SarifTag> sarifTags))
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
                    List<SarifTag> sarifTags = null;
                    if (FileToSarifTags.TryGetValue(this.fileName, out sarifTags))
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

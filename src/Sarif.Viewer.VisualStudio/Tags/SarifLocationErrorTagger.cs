// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Tags
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Tagging;

    internal class SarifLocationErrorTagger : ITagger<IErrorTag>, ISarifLocationTagger2, IDisposable
    {
        private bool isDisposed;

        /// <summary>
        /// The file path associated with the <see cref="ITextBuffer"/> given in the constructor.
        /// </summary>
        private readonly string filePath;

        private readonly IPersistentSpanFactory persistentSpanFactory;
        private readonly ITextBuffer textBuffer;
        private readonly ITextView textView;

        private List<ISarifLocationTag> currentTags;
        private bool tagsDirty = true;

        /// <inheritdoc/>
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        /// <inheritdoc/>
        public event EventHandler Disposed;

        public SarifLocationErrorTagger(ITextView textView, ITextBuffer textBuffer, IPersistentSpanFactory persistentSpanFactory)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!SdkUIUtilities.TryGetFileNameFromTextBuffer(textBuffer, out this.filePath))
            {
                throw new ArgumentException("Always expect to be able to get file name from text buffer.", nameof(textBuffer));
            }

            this.textBuffer = textBuffer;
            this.textView = textView;
            this.persistentSpanFactory = persistentSpanFactory;
        }

        /// <inheritdoc/>
        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (this.tagsDirty)
            {
                this.tagsDirty = false;

                // This query is saying, create the error tags for the top-level SARIF result (includeChildTags: false, includeResultTag: true)
                // and then create the tags for the children (includeChildTags: true, includeResultTag: false) but then
                // filter the children to to the ones relevant to the currently selected error item.
                IEnumerable<ISarifLocationTag> possibleTags = CodeAnalysisResultManager.
                    Instance.
                    RunIndexToRunDataCache.
                    Values.
                    SelectMany(runDataCache => runDataCache.SarifErrors).
                    Where(sarifListItem => string.Compare(this.filePath, sarifListItem.FileName, StringComparison.OrdinalIgnoreCase) == 0).
                    SelectMany(sarifListItem => 
                    sarifListItem.GetTags<IErrorTag>(this.textBuffer, this.persistentSpanFactory, includeChildTags: false, includeResultTag: true).Concat(
                        sarifListItem.GetTags<IErrorTag>(this.textBuffer, this.persistentSpanFactory, includeChildTags: true, includeResultTag: false).Where(
                            sarifLocationTag => SarifErrorListEventProcessor.SelectedItem != null && sarifLocationTag.ResultId == SarifErrorListEventProcessor.SelectedItem.ResultId))).
                    ToList();

                // We need to make sure the list isn't modified underneath us while providing the tags, so executing ToList to get our copy.
                this.currentTags = possibleTags.ToList();
            }

            if (!this.currentTags.Any())
            {
                yield break;
            }

            foreach (SnapshotSpan span in spans)
            {
                foreach (var possibleTag in this.currentTags.Where(possibleTag => possibleTag.DocumentPersistentSpan.Span != null))
                {
                    SnapshotSpan possibleTagSnapshotSpan = possibleTag.DocumentPersistentSpan.Span.GetSpan(span.Snapshot);
                    if (span.IntersectsWith(possibleTagSnapshotSpan))
                    {
                        yield return new TagSpan<IErrorTag>(possibleTagSnapshotSpan, (IErrorTag)possibleTag);
                    }
                }
            }
        }

        public void MarkTagsDirty()
        {
            this.tagsDirty = true;

            ITextSnapshot textSnapshot = this.textBuffer.CurrentSnapshot;
            this.TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(textSnapshot, 0, textSnapshot.Length)));
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
                this.Disposed?.Invoke(this, new EventArgs());
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Tags
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Sarif.Viewer.ErrorList;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Tagging;

    /// <summary>
    /// Provides tags to Visual Studio from <see cref="SarifErrorListItem"/>s. The tags are in the form of a "squiggle" or a "tooltip" or both.
    /// </summary>
    /// <remarks>
    /// The tags provided from this class represent all the instances of <see cref="ResultTextMarker"/> that a <see cref="SarifErrorListItem"/> may contain
    /// that are children of the currently selected <see cref="SarifErrorListItem"/> as defined by <see cref="ISarifErrorListEventSelectionService.SelectedItem"/>.
    /// So.. The SARIF error result "itself" will be represented as a "squiggle" and a "tooltip". The locations contained within that result
    /// (code-flows, etc) will be as just a tooltip with no squiggle.
    /// </remarks>
    internal class SarifLocationErrorTagger : ITagger<IErrorTag>, ISarifLocationTagger, IDisposable
    {
        private bool isDisposed;

        /// <summary>
        /// The file path associated with the <see cref="ITextBuffer"/> given in the constructor.
        /// </summary>
        private readonly string filePath;

        private readonly IPersistentSpanFactory persistentSpanFactory;
        private readonly ISarifErrorListEventSelectionService sarifErrorListEventSelectionService;
        private readonly ITextBuffer textBuffer;

        private List<ISarifLocationTag> currentTags;
        private bool tagsDirty = true;

        /// <inheritdoc/>
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        /// <inheritdoc/>
        public event EventHandler Disposed;

        public SarifLocationErrorTagger(ITextBuffer textBuffer, IPersistentSpanFactory persistentSpanFactory, ISarifErrorListEventSelectionService sarifErrorListEventSelectionService)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!SdkUIUtilities.TryGetFileNameFromTextBuffer(textBuffer, out this.filePath))
            {
                throw new ArgumentException("Always expect to be able to get file name from text buffer.", nameof(textBuffer));
            }

            this.textBuffer = textBuffer;
            this.persistentSpanFactory = persistentSpanFactory;
            this.sarifErrorListEventSelectionService = sarifErrorListEventSelectionService;
            this.sarifErrorListEventSelectionService.SelectedItemChanged += SarifErrorListEventSelectionService_SelectedItemChanged;
        }

        /// <inheritdoc/>
        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (this.tagsDirty)
            {
                this.tagsDirty = false;

                // This query is saying, create the error tags for the top-level SARIF result (includeChildTags: false, includeResultTag: true)
                // and then create the tags for the children (includeChildTags: true, includeResultTag: false) but then
                // filter the children (thread-flows, call nodes, etc.) to the ones relevant to the currently selected error item.
                // This is done so that duplicate locations from multiple results in results are rendered in the editor.
                IEnumerable<ISarifLocationTag> possibleTags = CodeAnalysisResultManager.
                    Instance.
                    RunIndexToRunDataCache.
                    Values.
                    SelectMany(runDataCache => runDataCache.SarifErrors).
                    Where(sarifListItem => string.Compare(this.filePath, sarifListItem.FileName, StringComparison.OrdinalIgnoreCase) == 0).
                    SelectMany(sarifListItem => 
                    sarifListItem.GetTags<IErrorTag>(this.textBuffer, this.persistentSpanFactory, includeChildTags: false, includeResultTag: true).Concat(
                        sarifListItem.GetTags<IErrorTag>(this.textBuffer, this.persistentSpanFactory, includeChildTags: true, includeResultTag: false).Where(
                            sarifLocationTag => this.sarifErrorListEventSelectionService.SelectedItem != null && sarifLocationTag.ResultId == this.sarifErrorListEventSelectionService.SelectedItem.ResultId))).
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
                foreach (ISarifLocationTag possibleTag in this.currentTags.Where(currentTag => currentTag.PersistentSpan.Span != null))
                {
                    SnapshotSpan possibleTagSnapshotSpan = possibleTag.PersistentSpan.Span.GetSpan(span.Snapshot);
                    if (span.IntersectsWith(possibleTagSnapshotSpan))
                    {
                        yield return new TagSpan<IErrorTag>(possibleTagSnapshotSpan, (IErrorTag)possibleTag);
                    }
                }
            }
        }

        public void RefreshTags()
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
                this.sarifErrorListEventSelectionService.SelectedItemChanged -= this.SarifErrorListEventSelectionService_SelectedItemChanged;
                this.Disposed?.Invoke(this, new EventArgs());
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void SarifErrorListEventSelectionService_SelectedItemChanged(object sender, SarifErrorListSelectionChangedEventArgs e)
        {
            this.RefreshTags();
        }
    }
}

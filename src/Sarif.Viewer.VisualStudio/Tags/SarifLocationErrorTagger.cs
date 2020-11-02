// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.Sarif.Viewer.Tags
{
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

        private List<ISarifLocationTag> currentTags;
        private bool tagsDirty = true;

        public SarifLocationErrorTagger(ITextBuffer textBuffer, IPersistentSpanFactory persistentSpanFactory, ISarifErrorListEventSelectionService sarifErrorListEventSelectionService)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!SdkUIUtilities.TryGetFileNameFromTextBuffer(textBuffer, out this.filePath))
            {
                throw new ArgumentException("Always expect to be able to get file name from text buffer.", nameof(textBuffer));
            }

            TextBuffer = textBuffer;

            this.persistentSpanFactory = persistentSpanFactory;
            this.sarifErrorListEventSelectionService = sarifErrorListEventSelectionService;
            this.sarifErrorListEventSelectionService.SelectedItemChanged += SarifErrorListEventSelectionService_SelectedItemChanged;
        }

        #region ITagger

        /// <inheritdoc/>
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        /// <inheritdoc/>
        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (this.tagsDirty)
            {
                this.tagsDirty = false;

                IEnumerable<SarifErrorListItem> errorsInCurrentFile = CodeAnalysisResultManager
                    .Instance
                    .RunIndexToRunDataCache
                    .Values
                    .SelectMany(runDataCache => runDataCache.SarifErrors)
                    .Where(sarifListItem =>
                        !sarifListItem.IsFixed // Don't squiggle an error that's already fixed.
                        && string.Compare(this.filePath, sarifListItem.FileName, StringComparison.OrdinalIgnoreCase) == 0);

                IEnumerable<ISarifLocationTag> resultLocationTags = errorsInCurrentFile
                    .SelectMany(sarifListItem =>
                        sarifListItem.GetTags<IErrorTag>(TextBuffer, this.persistentSpanFactory, includeChildTags: false, includeResultTag: true));

                IEnumerable<ISarifLocationTag> associatedLocationTags = this.sarifErrorListEventSelectionService.SelectedItem != null
                    ? (errorsInCurrentFile
                        .SelectMany(sarifListItem =>
                            sarifListItem.GetTags<IErrorTag>(TextBuffer, this.persistentSpanFactory, includeChildTags: true, includeResultTag: false)
                        .Where(sarifLocationTag =>
                            sarifLocationTag.ResultId == this.sarifErrorListEventSelectionService.SelectedItem.ResultId))) : Enumerable.Empty<ISarifLocationTag>();

                IEnumerable<ISarifLocationTag> relevantTags = resultLocationTags.Concat(associatedLocationTags);

                // We need to make sure the list isn't modified underneath us while providing the tags, so executing ToList to get our copy.
                this.currentTags = relevantTags.ToList();
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

        #endregion ITagger

        #region ISarifLocationTagger

        /// <inheritdoc/>
        public event EventHandler Disposed;

        public ITextBuffer TextBuffer { get; }

        public void RefreshTags()
        {
            this.tagsDirty = true;

            ITextSnapshot textSnapshot = TextBuffer.CurrentSnapshot;
            this.TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(textSnapshot, 0, textSnapshot.Length)));
        }

        #endregion ISarifLocationTagger

        #region IDisposable

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
                this.Disposed?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable

        private void SarifErrorListEventSelectionService_SelectedItemChanged(object sender, SarifErrorListSelectionChangedEventArgs e)
        {
            this.RefreshTags();
        }
    }
}

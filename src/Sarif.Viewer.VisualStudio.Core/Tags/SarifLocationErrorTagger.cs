// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;

using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.Sarif.Viewer.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

using Sarif.Viewer.VisualStudio.Core.Models;

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

            this.TextBuffer = textBuffer;

            this.persistentSpanFactory = persistentSpanFactory;
            this.sarifErrorListEventSelectionService = sarifErrorListEventSelectionService;
            this.sarifErrorListEventSelectionService.SelectedItemChanged += this.SarifErrorListEventSelectionService_SelectedItemChanged;
            SarifViewerColorOptions.Instance.InsightSettingsChanged += OnInsightSettingsChanged;
        }

        /// <inheritdoc/>
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        /// <inheritdoc/>
        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (this.tagsDirty)
            {
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
                        sarifListItem.GetTags<IErrorTag>(this.TextBuffer, this.persistentSpanFactory, includeChildTags: false, includeResultTag: true));

                IEnumerable<ISarifLocationTag> associatedLocationTags = this.sarifErrorListEventSelectionService.SelectedItem != null
                    ? errorsInCurrentFile
                        .SelectMany(sarifListItem =>
                            sarifListItem.GetTags<IErrorTag>(this.TextBuffer, this.persistentSpanFactory, includeChildTags: true, includeResultTag: false)
                        .Where(sarifLocationTag =>
                            sarifLocationTag.ResultId == this.sarifErrorListEventSelectionService.SelectedItem.ResultId)) : Enumerable.Empty<ISarifLocationTag>();

                IEnumerable<ISarifLocationTag> relevantTags = resultLocationTags.Concat(associatedLocationTags);

                // We need to make sure the list isn't modified underneath us while providing the tags, so executing ToList to get our copy.
                this.currentTags = relevantTags.ToList();
            }

            if (this.currentTags == null || !this.currentTags.Any())
            {
                yield break;
            }

            var groupedBySpan = new Dictionary<(int start, int end), (List<IErrorTag> tagList, SnapshotSpan snapshotSpan)>();

            foreach (SnapshotSpan span in spans)
            {
                foreach (ISarifLocationTag locationTag in this.currentTags.Where(currentTag => currentTag.PersistentSpan.Span != null))
                {
                    SnapshotSpan snapshotSpan = locationTag.PersistentSpan.Span.GetSpan(span.Snapshot);
                    if (snapshotSpan.IntersectsWith(span))
                    {
                        (int start, int end) spanKey = (snapshotSpan.Start, snapshotSpan.End);
                        if (!groupedBySpan.ContainsKey(spanKey))
                        {
                            groupedBySpan[spanKey] = (new List<IErrorTag>(), snapshotSpan);
                        }

                        groupedBySpan[spanKey].tagList.Add((IErrorTag)locationTag);
                    }
                }
            }

            foreach (KeyValuePair<(int start, int end), (List<IErrorTag> tagList, SnapshotSpan snapshotSpan)> groupedTags in groupedBySpan)
            {
                List<IErrorTag> tags = groupedTags.Value.tagList;
                yield return new TagSpan<IErrorTag>(span: groupedTags.Value.snapshotSpan, tag: new ScrollViewerWrapper(tags, SarifViewerColorOptions.Instance));
            }
        }

        /// <inheritdoc/>
        public event EventHandler Disposed;

        public ITextBuffer TextBuffer { get; }

        /// <inheritdoc/>
        public void RefreshTags()
        {
            this.tagsDirty = true;

            ITextSnapshot textSnapshot = this.TextBuffer.CurrentSnapshot;
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
                SarifViewerColorOptions.Instance.InsightSettingsChanged -= OnInsightSettingsChanged;
                this.Disposed?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void SarifErrorListEventSelectionService_SelectedItemChanged(object sender, SarifErrorListSelectionChangedEventArgs e)
        {
            this.RefreshTags();
        }

        private void OnInsightSettingsChanged(EventArgs e)
        {
            RefreshTags();
        }
    }
}

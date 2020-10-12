// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Tags
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using Microsoft.Sarif.Viewer.ErrorList;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Tagging;

    /// <summary>
    /// Provides text marker tags (which appear text highlighting) to Visual Studio.
    /// </summary>
    /// <remarks>
    /// The tags provided from this class represent all the instances of <see cref="ResultTextMarker"/> that a <see cref="SarifErrorListItem"/> may contain.
    /// </remarks>
    internal class SarifLocationTextMarkerTagger : ITagger<ITextMarkerTag>, ISarifLocationTagger, IDisposable
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

        public SarifLocationTextMarkerTagger(
            ITextView textView,
            ITextBuffer textBuffer,
            IPersistentSpanFactory persistentSpanFactory,
            ITextViewCaretListenerService<ITextMarkerTag> textViewCaretListenerService,
            ISarifErrorListEventSelectionService sarifErrorListEventSelectionService)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!SdkUIUtilities.TryGetFileNameFromTextBuffer(textBuffer, out this.filePath))
            {
                throw new ArgumentException("Always expect to be able to get file name from text buffer.", nameof(textBuffer));
            }

            this.textBuffer = textBuffer;
            this.persistentSpanFactory = persistentSpanFactory;
            this.sarifErrorListEventSelectionService = sarifErrorListEventSelectionService;

            // Subscribe to the SARIF error item being selected from the error list
            // so we can properly filter the tags being shown in the editor
            // to the currently selected item.
            this.sarifErrorListEventSelectionService.SelectedItemChanged += this.SelectedSarifItemChanged;

            // Subscribe to the caret position so we can send enter and exit notifications
            // to the tags so they can decide potentially change their colors.
            textViewCaretListenerService.CreateListener(textView, this);
        }

        private void SelectedSarifItemChanged(object sender, SarifErrorListSelectionChangedEventArgs e) => this.RefreshTags();

        /// <inheritdoc/>
        public IEnumerable<ITagSpan<ITextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (this.tagsDirty)
            {
                this.tagsDirty = false;

                this.UnsubscribeFromTagEvents();

                SarifErrorListItem currentlySelectedItem = this.sarifErrorListEventSelectionService.SelectedItem;

                // We need to make sure the list isn't modified underneath us while providing the tags, so executing ToList to get our copy.
                this.currentTags = (currentlySelectedItem == null ? Enumerable.Empty<ISarifLocationTag>() :
                    CodeAnalysisResultManager.
                    Instance.
                    RunIndexToRunDataCache.
                    Values.
                    SelectMany(runDataCache => runDataCache.SarifErrors).
                    Where(sarifListItem => sarifListItem.ResultId == currentlySelectedItem.ResultId).
                    Where(sarifListItem => string.Compare(this.filePath, sarifListItem.FileName, StringComparison.OrdinalIgnoreCase) == 0).
                    SelectMany(sarifListItem => sarifListItem.GetTags<ITextMarkerTag>(this.textBuffer, this.persistentSpanFactory, includeChildTags: true, includeResultTag: true))).
                    ToList();

                // We need to subscribe to property change events from the provided tags
                // because they will change their highlight color depending on whether
                // the editor caret is within them.
                // When they change their colors, we need to ask Visual Studio to refresh it's tags.
                this.SubscribeToTagEvents();
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
                        yield return new TagSpan<ITextMarkerTag>(possibleTagSnapshotSpan, (ITextMarkerTag)possibleTag);
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

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void SubscribeToTagEvents()
        {
            if (this.currentTags?.Any() != true)
            {
                return;
            }

            foreach (ISarifLocationTag tag in this.currentTags)
            {
                if (tag is INotifyPropertyChanged notifyPropertyChanged)
                {
                    notifyPropertyChanged.PropertyChanged += this.TagPropertyChanged;
                }
            }
        }

        private void UnsubscribeFromTagEvents()
        {
            if (this.currentTags?.Any() != true)
            {
                return;
            }

            foreach (ISarifLocationTag tag in this.currentTags)
            {
                if (tag is INotifyPropertyChanged notifyPropertyChanged)
                {
                    notifyPropertyChanged.PropertyChanged -= this.TagPropertyChanged;
                }
            }
        }

        private void TagPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is ISarifLocationTag tag && tag.PersistentSpan.IsDocumentOpen)
            {
                this.TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(tag.PersistentSpan.Span.GetSpan(textBuffer.CurrentSnapshot)));
            }
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
                this.UnsubscribeFromTagEvents();
                this.sarifErrorListEventSelectionService.SelectedItemChanged -= this.SelectedSarifItemChanged;
                this.Disposed?.Invoke(this, new EventArgs());
            }
        }
    }
}

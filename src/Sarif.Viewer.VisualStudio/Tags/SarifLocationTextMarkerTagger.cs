﻿// Copyright (c) Microsoft. All rights reserved. 
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

    internal class SarifLocationTextMarkerTagger : ITagger<ITextMarkerTag>, ISarifLocationTagger, IDisposable
    {
        private bool isDisposed;

        /// <summary>
        /// The file path associated with the <see cref="ITextBuffer"/> given in the constructor.
        /// </summary>
        private readonly string filePath;

        private readonly IPersistentSpanFactory persistentSpanFactory;
        private readonly ITextBuffer textBuffer;

        private List<ISarifLocationTag> currentTags;
        private bool tagsDirty = true;

        /// <inheritdoc/>
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        /// <inheritdoc/>
        public event EventHandler Disposed;

        public SarifLocationTextMarkerTagger(ITextView textView, ITextBuffer textBuffer, IPersistentSpanFactory persistentSpanFactory)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!SdkUIUtilities.TryGetFileNameFromTextBuffer(textBuffer, out this.filePath))
            {
                throw new ArgumentException("Always expect to be able to get file name from text buffer.", nameof(textBuffer));
            }

            this.textBuffer = textBuffer;
            this.persistentSpanFactory = persistentSpanFactory;

            // Subscribe to the SARIF error item being selected from the error list
            // so we can properly filter the tags being shown in the editor
            // to the currently selected item.
            SarifErrorListEventProcessor.SelectedItemChanged += this.SelectedSarifItemChanged;

            // Subscribe to the caret position so we can send enter and exit notifications
            // to the tags so they can decide potentially change their colors.
            TextViewCaretListener<ITextMarkerTag>.CreateListener(textView, this);
        }

        private void SelectedSarifItemChanged(object sender, SarifErrorListSelectionChangedEventArgs e)
        {
            this.RefreshTags();
        }

        /// <inheritdoc/>
        public IEnumerable<ITagSpan<ITextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (this.tagsDirty)
            {
                this.tagsDirty = false;

                this.UnsubscribeFromTagEvents();

                SarifErrorListItem currentlySelectedItem = SarifErrorListEventProcessor.SelectedItem;

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

                this.SubscribeToTagEvents();
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
            if (this.currentTags == null || !this.currentTags.Any())
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
            if (this.currentTags == null || !this.currentTags.Any())
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
            if (sender is ISarifLocationTag tag && tag.DocumentPersistentSpan.IsDocumentOpen)
            {
                this.TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(tag.DocumentPersistentSpan.Span.GetSpan(textBuffer.CurrentSnapshot)));
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
                SarifErrorListEventProcessor.SelectedItemChanged -= this.SelectedSarifItemChanged;
                this.Disposed?.Invoke(this, new EventArgs());
            }
        }
    }
}

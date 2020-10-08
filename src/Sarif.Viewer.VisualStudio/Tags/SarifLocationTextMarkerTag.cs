// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Tags
{
    using System.ComponentModel;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Tagging;

    /// <summary>
    /// Contains the data necessary to display a text marker tag (a highlight)
    /// inside Visual Studio's text views.
    /// </summary>
    internal class SarifLocationTextMarkerTag : ISarifLocationTag, ITextMarkerTag, INotifyPropertyChanged
    {
        private string textMarkerTagType;
        private string highlightedTextMarkerTagType;
        private string currentTextMarkerTagType;

        /// <summary>
        /// <param name="documentPersistentSpan">The persistent span for the tag within a document.</param>
        /// <param name="runIndex">The SARIF run index associated with this tag.</param>
        /// <param name="resultId">the result ID associated with this tag.</param>
        /// <param name="textMarkerTagType">The text marker tag to display for this tag when it is not highlighted.</param>
        /// <param name="highlightedTextMarkerTagType">The text marker tag to display for this tag when it is highlighted.</param>
        /// <param name="context">Gets the data context for this tag.</param>
        /// </summary>
        public SarifLocationTextMarkerTag(IPersistentSpan documentPersistentSpan, int runIndex, int resultId, string textMarkerTagType, string highlightedTextMarkerTagType, object context)
        {
            this.DocumentPersistentSpan = documentPersistentSpan;
            this.RunIndex = runIndex;
            this.ResultId = resultId;
            this.textMarkerTagType = textMarkerTagType;
            this.highlightedTextMarkerTagType = highlightedTextMarkerTagType;
            this.currentTextMarkerTagType = textMarkerTagType;
            this.Context = context;
        }

        /// <inheritdoc/>
        public IPersistentSpan DocumentPersistentSpan { get; }

        /// <inheritdoc/>
        public int RunIndex { get; }

        /// <inheritdoc/>
        public string Type => this.currentTextMarkerTagType;

        /// <inheritdoc/>
        public int ResultId { get; }

        /// <inheritdoc/>
        public object Context { get; }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc/>
        public void UpdateTextMarkerTagType(string newTextMarkerTagType)
        {
            if (newTextMarkerTagType != this.currentTextMarkerTagType)
            {
                this.currentTextMarkerTagType = newTextMarkerTagType;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Type)));
            }
        }

        /// <inheritdoc/>
        public void NotifyCaretEntered()
        {
            this.UpdateTextMarkerTagType(this.highlightedTextMarkerTagType);
        }

        /// <inheritdoc/>
        public void NotifyCaretLeft()
        {
            this.UpdateTextMarkerTagType(this.textMarkerTagType);
        }
    }
}

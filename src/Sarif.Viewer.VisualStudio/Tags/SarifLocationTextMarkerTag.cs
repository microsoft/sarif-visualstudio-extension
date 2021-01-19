// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.Sarif.Viewer.Tags
{
    /// <summary>
    /// Contains the data necessary to display a text marker tag (a highlight)
    /// inside Visual Studio's text views.
    /// </summary>
    internal class SarifLocationTextMarkerTag : SarifLocationTagBase, ITextMarkerTag, ISarifLocationTagCaretNotify, INotifyPropertyChanged
    {
        private string currentTextMarkerTagType;
        private readonly string highlightedTextMarkerTagType;
        private readonly string nonHighlightedTextMarkerTagType;

        /// <summary>
        /// Initializes a new instance of the <see cref="SarifLocationTextMarkerTag"/> class.
        /// </summary>
        /// <param name="persistentSpan">The persistent span for the tag within a document.</param>
        /// <param name="runIndex">The SARIF run index associated with this tag.</param>
        /// <param name="resultId">the result ID associated with this tag.</param>
        /// <param name="nonHighlightedTextMarkerTagType">The text marker tag to display for this tag when it is not highlighted.</param>
        /// <param name="highlightedTextMarkerTagType">The text marker tag to display for this tag when it is highlighted.</param>
        /// <param name="context">Gets the data context for this tag.</param>
        public SarifLocationTextMarkerTag(IPersistentSpan persistentSpan, int runIndex, int resultId, string nonHighlightedTextMarkerTagType, string highlightedTextMarkerTagType, object context)
            : base(persistentSpan: persistentSpan, runIndex: runIndex, resultId: resultId, context: context)
        {
            this.currentTextMarkerTagType = nonHighlightedTextMarkerTagType;
            this.highlightedTextMarkerTagType = highlightedTextMarkerTagType;
            this.nonHighlightedTextMarkerTagType = nonHighlightedTextMarkerTagType;
        }

        /// <inheritdoc/>
        public string Type => this.currentTextMarkerTagType;

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc/>
        public void OnCaretEntered()
        {
            this.UpdateTextMarkerTagType(this.highlightedTextMarkerTagType);
        }

        /// <inheritdoc/>
        public void OnCaretLeft()
        {
            this.UpdateTextMarkerTagType(this.nonHighlightedTextMarkerTagType);
        }

        private void UpdateTextMarkerTagType(string newTextMarkerTagType)
        {
            if (newTextMarkerTagType != this.currentTextMarkerTagType)
            {
                this.currentTextMarkerTagType = newTextMarkerTagType;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Type)));
            }
        }
    }
}

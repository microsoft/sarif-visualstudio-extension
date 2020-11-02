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
    internal class SarifLocationTextMarkerTag : SarifLocationTagBase, ITextMarkerTag, ISarifLocationTagCaretNotify, INotifyPropertyChanged
    {
        private string currentTextMarkerTagType;
        private string highlightedTextMarkerTagType;
        private string nonHighlightedTextMarkerTagType;

        /// <summary>
        /// <param name="persistentSpan">The persistent span for the tag within a document.</param>
        /// <param name="runIndex">The SARIF run index associated with this tag.</param>
        /// <param name="resultId">the result ID associated with this tag.</param>
        /// <param name="nonHighlightedTextMarkerTagType">The text marker tag to display for this tag when it is not highlighted.</param>
        /// <param name="highlightedTextMarkerTagType">The text marker tag to display for this tag when it is highlighted.</param>
        /// <param name="context">Gets the data context for this tag.</param>
        /// </summary>
        public SarifLocationTextMarkerTag(IPersistentSpan persistentSpan, int runIndex, int resultId, string nonHighlightedTextMarkerTagType, string highlightedTextMarkerTagType, object context)
            : base(persistentSpan: persistentSpan, runIndex: runIndex, resultId: resultId, context: context)
        {
            this.currentTextMarkerTagType = nonHighlightedTextMarkerTagType;
            this.highlightedTextMarkerTagType = highlightedTextMarkerTagType;
            this.nonHighlightedTextMarkerTagType = nonHighlightedTextMarkerTagType;
        }


        #region IErrorTag
        /// <inheritdoc/>
        public string Type => this.currentTextMarkerTagType;
        #endregion IErrorTag

        #region INotifyPropertyChanged
        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion INotifyPropertyChanged

        #region ISarifLocationTagCaretNotify
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
        #endregion

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

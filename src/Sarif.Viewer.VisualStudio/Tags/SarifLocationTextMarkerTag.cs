// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.ComponentModel;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.Sarif.Viewer.Tags
{
    internal class SarifLocationTextMarkerTag : ITextMarkerTag, ISarifLocationTag, IDisposable
    {
        private string textMarkerTagType;
        private bool disposed;

        /// <summary>
        /// Initialize a new instance of <see cref="SarifLocationTextMarkerTag"/>.
        /// </summary>
        /// <param name="documentPersistentSpan">The persistent span for the tag within a document.</param>
        /// <param name="sourceRegion">The original span from the region present in the SARIF log.</param>
        /// <param name="runIndex">The SARIF run index associated with this tag.</param>
        /// <param name="textMarkerTagType">The text marker tag to display for this tag.</param>
        public SarifLocationTextMarkerTag(IPersistentSpan documentPersistentSpan, Region sourceRegion, int runIndex, string textMarkerTagType)
        {
            this.DocumentPersistentSpan = documentPersistentSpan;
            this.SourceRegion = sourceRegion;
            this.RunIndex = runIndex;
            this.textMarkerTagType = textMarkerTagType;
        }

        /// <inheritdoc/>
        public IPersistentSpan DocumentPersistentSpan { get; }

        /// <inheritdoc/>
        public Region SourceRegion { get; }

        /// <inheritdoc/>
        public int RunIndex { get; }

        /// <inheritdoc/>
        public string TextMarkerTagType
        {
            get => this.textMarkerTagType;

            set
            {
                if (value != this.textMarkerTagType)
                {
                    this.textMarkerTagType = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.TextMarkerTagType)));
                }
            }
        }

        /// <inheritdoc/>
        public string Type { get => this.textMarkerTagType; }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc/>
        public event EventHandler CaretEnteredTag;

        /// <summary>
        /// Called by the tagger to when it detects that the caret for a text view has entered a tag.
        /// </summary>
        public void RaiseCaretEnteredTag()
        {
            this.CaretEnteredTag?.Invoke(this, new EventArgs());
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                this.disposed = true;
                if (disposing)
                {
                    this.DocumentPersistentSpan?.Dispose();
                }
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

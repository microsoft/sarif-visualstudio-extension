// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.ComponentModel;

namespace Microsoft.Sarif.Viewer.Tags
{
    internal class SarifTag : ISarifTag, IDisposable
    {
        private TextMarkerTag textMarkerTag;
        private bool disposed;

        /// <summary>
        /// Initialize a new instance of <see cref="SarifTag"/>.
        /// </summary>
        /// <param name="documentPersistentSpan">The persistent span for the tag within a document.</param>
        /// <param name="sourceRegion">The original span from the region present in the SARIF log.</param>
        /// <param name="textMarkerTag">The text marker tag to use.</param>
        public SarifTag(IPersistentSpan documentPersistentSpan, Region sourceRegion, TextMarkerTag textMarkerTag)
        {
            this.DocumentPersistentSpan = documentPersistentSpan;
            this.textMarkerTag = textMarkerTag;
            this.SourceRegion = sourceRegion;
        }

        /// <inheritdoc/>
        public IPersistentSpan DocumentPersistentSpan { get; }

        /// <inheritdoc/>
        public Region SourceRegion { get; }

        /// <inheritdoc/>
        public TextMarkerTag Tag
        {
            get => this.textMarkerTag;

            set
            {
                if (value != this.textMarkerTag)
                {
                    this.textMarkerTag = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Tag)));
                }
            }
        }

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

// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.Sarif.Viewer.Tags
{
    /// <summary>
    /// Contains the data necessary to display a text marker tag (a highlight)
    /// inside Visual Studio's text views.
    /// </summary>
    internal class SarifLocationTextMarkerTag : ISarifLocationTextMarkerTag, INotifyPropertyChanged
    {
        private bool disposed;
        private string textMarkerTagType;

        /// <summary>
        /// Initialize a new instance of <see cref="SarifLocationTextMarkerTag"/>.
        /// </summary>
        /// <param name="documentPersistentSpan">The persistent span for the tag within a document.</param>
        /// <param name="runIndex">The SARIF run index associated with this tag.</param>
        /// <param name="resultId">the result ID associated with this tag.</param>
        /// <param name="textMarkerTagType">The text marker tag to display for this tag.</param>
        public SarifLocationTextMarkerTag(IPersistentSpan documentPersistentSpan, ITextBuffer textBuffer, int runIndex, int resultId, string textMarkerTagType)
        {
            this.DocumentPersistentSpan = documentPersistentSpan;
            this.TextBuffer = textBuffer;
            this.RunIndex = runIndex;
            this.ResultId = resultId;
            this.textMarkerTagType = textMarkerTagType;
        }

        /// <inheritdoc/>
        public IPersistentSpan DocumentPersistentSpan { get; }

        /// <inheritdoc/>
        public int RunIndex { get; }

        /// <inheritdoc/>
        public string Type
        {
            get => this.textMarkerTagType;
        }

        /// <inheritdoc/>
        public ITextBuffer TextBuffer { get; }

        /// <inheritdoc/>
        public int ResultId { get; }

        /// <inheritdoc/>
        public event EventHandler CaretEntered;

        /// <inheritdoc/>
        public event EventHandler CaretLeft;

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc/>
        public void UpdateTextMarkerTagType(string newTextMarkerTagType)
        {
            if (newTextMarkerTagType != this.textMarkerTagType)
            {
                this.textMarkerTagType = newTextMarkerTagType;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Type)));
            }
        }

        /// <inheritdoc/>
        public void NotifyCaretEntered()
        {
            this.CaretEntered?.Invoke(this, new EventArgs());
        }

        /// <inheritdoc/>
        public void NotifyCaretLeft()
        {
            this.CaretLeft?.Invoke(this, new EventArgs());
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

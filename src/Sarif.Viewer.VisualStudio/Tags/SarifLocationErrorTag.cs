// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using Microsoft.VisualStudio.Text;

namespace Microsoft.Sarif.Viewer.Tags
{
    /// <summary>
    /// Contains the data necessary to display a error tag (a underlined squiggle with a tool tip)
    /// inside Visual Studio's text views.
    /// </summary>
    internal class SarifLocationErrorTag : ISarifLocationErrorTag
    {
        private bool disposed;

        /// <summary>
        /// Initialize a new instance of <see cref="SarifLocationErrorTag"/>.
        /// </summary>
        /// <param name="documentPersistentSpan">The persistent span for the tag within a document.</param>
        /// <param name="textBuffer">The Visual Studio <see cref="ITextBuffer"/> this tag is associated with.</param>
        /// <param name="runIndex">The SARIF run index associated with this tag.</param>
        /// <param name="errorType">The Visual Studio error type to display.</param>
        /// <param name="toolTipContet">The content to use when displaying a tool tip for this error. This parameter may be null.</param>
        public SarifLocationErrorTag(IPersistentSpan documentPersistentSpan, ITextBuffer textBuffer, int runIndex, string errorType, object toolTipContet)
        {
            this.DocumentPersistentSpan = documentPersistentSpan;
            this.TextBuffer = textBuffer;
            this.RunIndex = runIndex;
            this.ErrorType = errorType;
            this.ToolTipContent = toolTipContet;
        }

        /// <inheritdoc/>
        public IPersistentSpan DocumentPersistentSpan { get; }
 
        /// <inheritdoc/>
        public int RunIndex { get; }

        /// <inheritdoc/>
        public string ErrorType { get; }

        /// <inheritdoc/>
        public object ToolTipContent { get; }

        /// <inheritdoc/>
        public ITextBuffer TextBuffer { get; }

        /// <inheritdoc/>
        public event EventHandler CaretEnteredTag;

        /// <inheritdoc/>
        public void NotifyCaretWithin()
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

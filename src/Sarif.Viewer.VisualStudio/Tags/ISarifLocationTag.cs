// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Tags
{
    using Microsoft.VisualStudio.Text;
    using System;

    internal interface ISarifLocationTag : IDisposable
    {
        /// <summary>
        /// Gets the persistent span for a document.
        /// </summary>
        /// <remarks>
        /// This span is not necessarily the same as <see cref="SourceRegion"/>.
        /// It may have been modified to fix up column and line numbers from the region
        /// present in the SARIF log.
        /// </remarks>
        IPersistentSpan DocumentPersistentSpan { get; }

        /// <summary>
        /// The Visual Studio buffer this tag is associated with.
        /// </summary>
        ITextBuffer TextBuffer { get; }

        /// <summary>
        /// Gets the SARIF run index associated with this tag.
        /// </summary>
        int RunIndex { get; }

        /// <summary>
        /// Gets the result ID associated with this tag.
        /// </summary>
        int ResultId { get; }

        /// <summary>
        /// Fired when the caret enters a tag.
        /// </summary>
        event EventHandler CaretEntered;

        /// <summary>
        /// Fired when the caret leaves a tag.
        /// </summary>
        event EventHandler CaretLeft;

        /// <summary>
        /// Causes the object to raise a <see cref="CaretEntered"/> event its consumers.
        /// </summary>
        void NotifyCaretEntered();

        /// <summary>
        /// Causes the object to raise a <see cref="CaretLeft"/> event its consumers.
        /// </summary>
        void NotifyCaretLeft();
    }
}

// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Tags
{
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Tagging;
    using System;

    internal interface ISarifLocationTag : ITag, IDisposable
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
        /// Fired when the caret enters a tag.
        /// </summary>
        event EventHandler CaretEnteredTag;

        /// <summary>
        /// Causes the object to raise a <see cref="CaretEnteredTag"/> event its consumers.
        /// </summary>
        void NotifyCaretWithin();
    }
}

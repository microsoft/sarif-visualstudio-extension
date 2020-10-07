// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Tags
{
    using Microsoft.VisualStudio.Text;

    internal interface ISarifLocationTag
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
        /// Gets the SARIF run index associated with this tag.
        /// </summary>
        int RunIndex { get; }

        /// <summary>
        /// Gets the result ID associated with this tag.
        /// </summary>
        int ResultId { get; }

        /// <summary>
        /// Called when the caret from the text editor enters this tag.
        /// </summary>
        void NotifyCaretEntered();

        /// <summary>
        /// Called when the caret from the text editor leaves this tag.
        /// </summary>
        void NotifyCaretLeft();
    }
}

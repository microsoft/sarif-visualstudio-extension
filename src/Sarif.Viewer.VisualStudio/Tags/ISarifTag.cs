// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Tags
{
    using Microsoft.CodeAnalysis.Sarif;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Tagging;
    using System;
    using System.ComponentModel;

    internal interface ISarifTag: INotifyPropertyChanged
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
        /// Gets the original span (SAIRF region) that was present in the SARIF log.
        /// </summary>
        Region SourceRegion { get; }

        /// <summary>
        /// Gets the SARIF log run identifier associated with this tag.
        /// </summary>
        int RunId { get; }

        /// <summary>
        /// Gets the current text tag used for this tag.
        /// </summary>
        TextMarkerTag Tag { get; set; }

        /// <summary>
        /// Fired when the caret enters a tag.
        /// </summary>
        event EventHandler CaretEnteredTag;
    }
}

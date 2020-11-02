// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using Microsoft.Sarif.Viewer.Models;
using Microsoft.VisualStudio.Text;

namespace Microsoft.Sarif.Viewer.Fixes
{
    /// <summary>
    /// Represents an edit to a file. When the edit is applied, the contents of the span will be
    /// replaced by the text.
    /// </summary>
    public class ReplacementEdit
    {
        /// <summary>
        /// Gets the span to be replaced.
        /// </summary>
        public SnapshotSpan Span { get; }

        /// <summary>
        /// Gets the text to be inserted.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplacementEdit"/> class.
        /// </summary>
        /// <param name="edit">The SARIF replacement from which to construct the edit.</param>
        /// <param name="snapshot">The snapshot to which the edit will be applied.</param>
        public ReplacementEdit(ReplacementModel replacement, ITextSnapshot snapshot)
        {
            this.Text = replacement.InsertedString ?? string.Empty;

            ITrackingSpan replacementSpan = replacement.PersistentSpan.Span;
            SnapshotPoint start = replacementSpan.GetStartPoint(snapshot);
            SnapshotPoint end = replacementSpan.GetEndPoint(snapshot);

            this.Span = new SnapshotSpan(start, end);
        }
    }
}

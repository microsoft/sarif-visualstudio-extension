// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.Sarif.Viewer
{
    internal class FixSuggestedActionsSource : ISuggestedActionsSource
    {
        /// <summary>
        /// Creates a new instance of <see cref="FixSuggestedActionsSource"/>.
        /// </summary>
        /// <param name="fixSuggestedActionsSourceProvider">
        /// The provider for this source.
        /// </param>
        /// <param name="textView">
        /// The <see cref="ITextView"/> for which this source will offer fix suggestions.
        /// </param>
        /// <param name="textBuffer">
        /// The <see cref="ITextBuffer"/> associated with the <see cref="ITextView"/> for which this
        /// source will offer fix suggestions.
        /// </param>
        public FixSuggestedActionsSource(FixSuggestedActionsSourceProvider fixSuggestedActionsSourceProvider, ITextView textView, ITextBuffer textBuffer)
        {
            FixSuggestedActionsSourceProvider = fixSuggestedActionsSourceProvider;
            TextView = textView;
            TextBuffer = textBuffer;
        }

        // TODO: Decide if VS actually requires these properties to be public. Once everything
        // is working, try replacing them with private fields.

        /// <summary>
        /// Gets the provider for this source.
        /// </summary>
        public FixSuggestedActionsSourceProvider FixSuggestedActionsSourceProvider { get; }

        /// <summary>
        /// Gets the <see cref="ITextView"/> for which this source will offer fix suggestions.
        /// </summary>
        public ITextView TextView { get; }

        /// <summary>
        /// Gets the <see cref="ITextBuffer"/> associated with the <see cref="ITextView"/> for which
        /// this source will offer fix suggestions.
        /// </summary>
        public ITextBuffer TextBuffer { get; }

#pragma warning disable 0067
        public event EventHandler<EventArgs> SuggestedActionsChanged;
#pragma warning restore 0067

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            yield break;
        }

        /// <inheritdoc/>
        public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        /// <inheritdoc/>
        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
    }
}

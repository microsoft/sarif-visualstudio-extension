// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.Sarif.Viewer
{
    internal class FixSuggestedActionsSource : ISuggestedActionsSource
    {
        private readonly ITextView textView;
        private readonly ITextBuffer textBuffer;

        /// <summary>
        /// Creates a new instance of <see cref="FixSuggestedActionsSource"/>.
        /// </summary>
        /// <param name="textView">
        /// The <see cref="ITextView"/> for which this source will offer fix suggestions.
        /// </param>
        /// <param name="textBuffer">
        /// The <see cref="ITextBuffer"/> associated with the <see cref="ITextView"/> for which this
        /// source will offer fix suggestions.
        /// </param>
        public FixSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            this.textView = textView;
            this.textBuffer = textBuffer;
        }

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
        public async Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            if (!SarifViewerPackage.IsUnitTesting)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            }

            return await System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                return CaretIsOnSarifError();
            },
            cancellationToken,
            TaskCreationOptions.None,
            TaskScheduler.Current); // Use the scheduler associate with the current thread,
                                    // which at this point is known to be the UI thread.
        }

        /// <inheritdoc/>
        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        private bool CaretIsOnSarifError() => false;
    }
}

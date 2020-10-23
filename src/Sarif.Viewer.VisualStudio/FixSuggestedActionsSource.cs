// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Sarif.Viewer
{
    internal class FixSuggestedActionsSource : ISuggestedActionsSource
    {
        private readonly ITextView textView;
        private readonly ITextBuffer textBuffer;
        private readonly IEnumerable<SarifErrorListItem> errorsWithFixes;

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
            ThreadHelper.ThrowIfNotOnUIThread();

            this.textView = textView;
            this.textBuffer = textBuffer;


            // If this text buffer is not associated with a file, it cannot have any SARIF errors.
            if (SdkUIUtilities.TryGetFileNameFromTextBuffer(this.textBuffer, out string fileName))
            {
                this.errorsWithFixes = CodeAnalysisResultManager
                    .Instance
                    .RunIndexToRunDataCache
                    .Values
                    .SelectMany(runDataCache => runDataCache.SarifErrors)
                    .Where(sarifListItem => string.Compare(fileName, sarifListItem.FileName, StringComparison.OrdinalIgnoreCase) == 0)
                    .Where(sarifListItem => sarifListItem.Fixes.Any());
            }
            else
            {
                this.errorsWithFixes = Enumerable.Empty<SarifErrorListItem>();
            }
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
            if (CaretIsOnAnySarifError())
            {
                SarifErrorListItem sarifError = GetSelectedSarifError();
                return CreateActionSetFromSarifError(sarifError);
            }
            else
            {
                return Enumerable.Empty<SuggestedActionSet>();
            }
        }

        /// <inheritdoc/>
        public async Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
            => await Task.FromResult(CaretIsOnAnySarifError());

        /// <inheritdoc/>
        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        private bool CaretIsOnAnySarifError() => this.errorsWithFixes.Any(CaretIsOnSarifError);

        // TODO: Really implement.
        private bool CaretIsOnSarifError(SarifErrorListItem sarifError) => true;

        // TODO: Really implement.
        private SarifErrorListItem GetSelectedSarifError() => this.errorsWithFixes.First();

        private IEnumerable<SuggestedActionSet> CreateActionSetFromSarifError(SarifErrorListItem sarifError)
        {
            IEnumerable<FixSuggestedAction> suggestedActions = sarifError.Fixes.Select(ToSuggestedAction);
            var suggestedActionSet = new SuggestedActionSet(suggestedActions);
            return new List<SuggestedActionSet>
            {
                suggestedActionSet
            };
        }

        private FixSuggestedAction ToSuggestedAction(FixModel fix) => new FixSuggestedAction(fix);
    }
}

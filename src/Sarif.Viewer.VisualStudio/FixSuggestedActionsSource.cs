// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.Sarif.Viewer.Sarif;
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
        private readonly IPersistentSpanFactory persistentSpanFactory;
        private readonly ReadOnlyCollection<SarifErrorListItem> fixableErrors;

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
        public FixSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer, IPersistentSpanFactory persistentSpanFactory)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.textView = textView;
            this.textBuffer = textBuffer;
            this.persistentSpanFactory = persistentSpanFactory;

            // If this text buffer is not associated with a file, it cannot have any SARIF errors.
            if (SdkUIUtilities.TryGetFileNameFromTextBuffer(this.textBuffer, out string fileName))
            {
                IEnumerable<SarifErrorListItem> allErrorsInFile = CodeAnalysisResultManager
                    .Instance
                    .RunIndexToRunDataCache
                    .Values
                    .SelectMany(runDataCache => runDataCache.SarifErrors)
                    .Where(sarifListItem => string.Compare(fileName, sarifListItem.FileName, StringComparison.OrdinalIgnoreCase) == 0);

                this.fixableErrors = allErrorsInFile
                    .Where(error => error.Fixes.Any(fix => fix.CanBeApplied()))
                    .ToList()
                    .AsReadOnly();

                CalculatePersistentSpans(fixableErrors);
            }
            else
            {
                this.fixableErrors = new List<SarifErrorListItem>().AsReadOnly();
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
            IEnumerable<SarifErrorListItem> selectedFixableErrors = GetSelectedFixableErrors();
            return CreateActionSetFromSarifErrors(selectedFixableErrors);
        }

        /// <inheritdoc/>
        public async Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
            => await Task.FromResult(GetSelectedFixableErrors().Any());

        /// <inheritdoc/>
        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        private void CalculatePersistentSpans(ReadOnlyCollection<SarifErrorListItem> sarifErrors)
        {
            IEnumerable<ReplacementModel> replacementsNeedingPersistentSpans = sarifErrors
                .SelectMany(error => error.Fixes)
                .Where(fix => fix.CanBeApplied())
                .SelectMany(fix => fix.ArtifactChanges)
                .SelectMany(ac => ac.Replacements)
                .Where(r => r.PersistentSpan == null);

            foreach (ReplacementModel replacement in replacementsNeedingPersistentSpans)
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (SpanHelper.TryCreatePersistentSpan(replacement.Region, this.textBuffer, this.persistentSpanFactory, out IPersistentSpan persistentSpan))
                {
                    replacement.PersistentSpan = persistentSpan;
                }
            }
        }

        // TODO: Really implement.
        private IEnumerable<SarifErrorListItem> GetSelectedFixableErrors() =>
            new List<SarifErrorListItem>
            {
                this.fixableErrors.First()
            };

        private IEnumerable<SuggestedActionSet> CreateActionSetFromSarifErrors(IEnumerable<SarifErrorListItem> sarifErrors)
        {
            // Every error in the specified list has at least one fix that can be
            // applied, but we must provide only the apply-able ones.
            IEnumerable<FixSuggestedAction> suggestedActions = sarifErrors
                .SelectMany(se => se.Fixes)
                .Where(fix => fix.CanBeApplied())
                .Select(ToSuggestedAction);

            return new List<SuggestedActionSet>
            {
                new SuggestedActionSet(suggestedActions)
            };
        }

        private FixSuggestedAction ToSuggestedAction(FixModel fix) => new FixSuggestedAction(fix);
    }
}

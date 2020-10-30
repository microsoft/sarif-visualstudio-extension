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

namespace Microsoft.Sarif.Viewer.Fixes
{
    internal class FixSuggestedActionsSource : ISuggestedActionsSource
    {
        private readonly ITextView textView;
        private readonly ITextBuffer textBuffer;
        private readonly IPersistentSpanFactory persistentSpanFactory;
        private readonly IPreviewProvider previewProvider;
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
        /// <param name="persistentSpanFactory">
        /// A factory for creating the persistent spans that specify the error locations and the
        /// replacement locations (which are not necessarily the same).
        /// </param>
        /// <param name="previewProvider">
        /// Creates the XAML UIControl that displays the preview.
        /// </param>
        public FixSuggestedActionsSource(
            ITextView textView,
            ITextBuffer textBuffer,
            IPersistentSpanFactory persistentSpanFactory,
            IPreviewProvider previewProvider)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.textView = textView;
            this.textBuffer = textBuffer;
            this.persistentSpanFactory = persistentSpanFactory;
            this.previewProvider = previewProvider;

            if (SdkUIUtilities.TryGetFileNameFromTextBuffer(this.textBuffer, out string fileName))
            {
                IEnumerable<SarifErrorListItem> errorsInFile = GetErrorsInFile(fileName);
                this.fixableErrors = GetFixableErrors(errorsInFile);
                CalculatePersistentSpans(fixableErrors);
            }
            else
            {
                // If this text buffer is not associated with a file, it cannot have any SARIF errors.
                this.fixableErrors = new List<SarifErrorListItem>().AsReadOnly();
            }
        }

        private static IEnumerable<SarifErrorListItem> GetErrorsInFile(string fileName) =>
            CodeAnalysisResultManager
            .Instance
            .RunIndexToRunDataCache
            .Values
            .SelectMany(runDataCache => runDataCache.SarifErrors)
            .Where(error => string.Compare(fileName, error.FileName, StringComparison.OrdinalIgnoreCase) == 0);

        private static ReadOnlyCollection<SarifErrorListItem> GetFixableErrors(IEnumerable<SarifErrorListItem> errors) =>
            errors
            .Where(error => error.IsFixable())
            .ToList()
            .AsReadOnly();

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
            return CreateActionSetFromErrors(selectedFixableErrors);
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

        // Calculate persistent spans for each error location (because we want to display a
        // lightbulb any time the caret intersects such a span, even if the document has been
        // edited) and for each region that must be replaced when the error is fixed (because
        // we want to apply the fix in the right place, even if the document has been edited).
        private void CalculatePersistentSpans(ReadOnlyCollection<SarifErrorListItem> errors)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Calculate persistent spans for the error locations.
            IEnumerable<LocationModel> locationsNeedingPersistentSpans = errors.SelectMany(error => error.Locations);
            foreach (LocationModel location in locationsNeedingPersistentSpans)
            {
                if (SpanHelper.TryCreatePersistentSpan(location.Region, this.textBuffer, this.persistentSpanFactory, out IPersistentSpan persistentSpan))
                {
                    location.PersistentSpan = persistentSpan;
                }
            }

            // Calculate persistent spans for each region that must be replaced in every
            // applyable fix. Not every fix is applyable because (1) it might not have enough
            // information to resolve the absolute path to every file that must be touched,
            // and (2) it might change files other than the file containing the error.
            // There's nothing fundamentally wrong with that, but we don't have a good UI
            // experience for it.
            IEnumerable<FixModel> applyableFixes = new List<FixModel>();
            foreach (SarifErrorListItem error in errors)
            {
                foreach (FixModel fixModel in error.Fixes.Where(fix => fix.CanBeAppliedToFile(error.FileName)))
                {
                    applyableFixes.Append(fixModel);
                }
            }

            IEnumerable<ReplacementModel> replacementsNeedingPersistentSpans = applyableFixes
                .SelectMany(fix => fix.ArtifactChanges)
                .SelectMany(ac => ac.Replacements)
                .Where(r => r.PersistentSpan == null);

            foreach (ReplacementModel replacement in replacementsNeedingPersistentSpans)
            {
                if (SpanHelper.TryCreatePersistentSpan(replacement.Region, this.textBuffer, this.persistentSpanFactory, out IPersistentSpan persistentSpan))
                {
                    replacement.PersistentSpan = persistentSpan;
                }
            }
        }

        // Find all fixable errors any of whose locations intersect the caret. Those are the
        // locations where a lightbulb should appear.
        private IEnumerable<SarifErrorListItem> GetSelectedFixableErrors()
        {
            SnapshotPoint caretSnapshotPoint = this.textView.Caret.Position.BufferPosition;
            var caretSpanCollection =
                new NormalizedSnapshotSpanCollection(new SnapshotSpan(start: caretSnapshotPoint, end: caretSnapshotPoint));

            return this.fixableErrors.Where(error => CaretIntersectsAnyErrorLocation(error, caretSpanCollection));
        }

        private bool CaretIntersectsAnyErrorLocation(SarifErrorListItem error, NormalizedSnapshotSpanCollection caretSpanCollection) =>
            error
            .Locations
            ?.Any(locationModel => CaretIntersectsSingleErrorLocation(locationModel, caretSpanCollection)) == true;

        private bool CaretIntersectsSingleErrorLocation(LocationModel locationModel, NormalizedSnapshotSpanCollection caretSpanCollection) =>
            caretSpanCollection.Any(
                caretSpan => caretSpan.IntersectsWith(locationModel.PersistentSpan.Span.GetSpan(caretSpan.Snapshot)));

        private IEnumerable<SuggestedActionSet> CreateActionSetFromErrors(IEnumerable<SarifErrorListItem> errors)
        {
            // Every error in the specified list has at least one fix that can be
            // applied, but we must provide only the apply-able ones.
            var suggestedActions = new List<ISuggestedAction>();
            foreach (SarifErrorListItem error in errors)
            {
                suggestedActions.AddRange(error.Fixes
                    .Where(fix => fix.CanBeAppliedToFile(error.FileName))
                    .Select(ToSuggestedAction));
            }

            return new List<SuggestedActionSet>
            {
                new SuggestedActionSet(suggestedActions)
            };
        }

        private FixSuggestedAction ToSuggestedAction(FixModel fix) =>
            new FixSuggestedAction(fix, this.textBuffer, this.previewProvider);
    }
}

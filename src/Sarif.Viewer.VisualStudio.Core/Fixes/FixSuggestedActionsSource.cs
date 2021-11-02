// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.Sarif.Viewer.Sarif;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.Sarif.Viewer.Fixes
{
    internal class FixSuggestedActionsSource : ISuggestedActionsSource
    {
        private static readonly string SuggestionCategory = "Sarif Viewer Suggestions";

        private readonly ITextView textView;
        private readonly ITextBuffer textBuffer;
        private readonly IPersistentSpanFactory persistentSpanFactory;
        private readonly IPreviewProvider previewProvider;

        private readonly IDictionary<FixSuggestedAction, SarifErrorListItem> fixToErrorDictionary;
        private readonly IWpfTableControl errorListTableControl;
        private readonly ISarifErrorListEventSelectionService sarifErrorListEventSelectionService;

        private IList<SarifErrorListItem> errorsInFile;

        /// <summary>
        /// Initializes a new instance of the <see cref="FixSuggestedActionsSource"/> class.
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

            if (this.previewProvider is EditActionPreviewProvider editActionPreviewProvider)
            {
                editActionPreviewProvider.ApplyFixesInDocument += this.PreviewProdiver_ApplyFixesInDocument;
            }

            // when text changed and sarif errors item changes, need to refresh errorInFile
            var errorList = ServiceProvider.GlobalProvider.GetService(typeof(SVsErrorList)) as IErrorList;
            this.errorListTableControl = errorList?.TableControl;
            if (this.errorListTableControl != null)
            {
                this.errorListTableControl.EntriesChanged += this.ErrorListTableControl_EntriesChanged;
            }

            var component = ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel)) as IComponentModel;
            this.sarifErrorListEventSelectionService = component?.GetService<ISarifErrorListEventSelectionService>();
            if (this.sarifErrorListEventSelectionService != null)
            {
                this.sarifErrorListEventSelectionService.NavigatedItemChanged += this.SarifListErrorItemNavigated;
            }

            this.RefreshPersistentSpans();

            // Keep track of which error is associated with each suggested action, so that when
            // the action is invoked, the associated error can be marked as fixed. When we mark
            // an error as fixed, we tell VS to recompute the list of suggested actions, so that
            // it doesn't suggest actions for errors that are already fixed.
            this.fixToErrorDictionary = new Dictionary<FixSuggestedAction, SarifErrorListItem>();
        }

        private void PreviewProdiver_ApplyFixesInDocument(object sender, ApplyFixEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // get same fixable errors in this document
            IEnumerable<SarifErrorListItem> selectedFixableErrors = this.errorsInFile
                .Where(error => error.Rule.Id.Equals(e.ErrorItem.Rule.Id, StringComparison.Ordinal))
                .Where(error => error.IsFixable());

            if (selectedFixableErrors == null || !selectedFixableErrors.Any())
            {
                return;
            }

            // execute action
            IEnumerable<ISuggestedAction> suggestedActions = this.CreateActionSetFromErrors(selectedFixableErrors).SelectMany(set => set.Actions);
            suggestedActions.ToList().ForEach(action => action.Invoke(CancellationToken.None));
        }

#pragma warning disable 0067

        /// <inheritdoc/>
        public event EventHandler<EventArgs> SuggestedActionsChanged;
#pragma warning restore 0067

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.previewProvider != null &&
                this.previewProvider is EditActionPreviewProvider editActionPreviewProvider)
            {
                editActionPreviewProvider.ApplyFixesInDocument -= this.PreviewProdiver_ApplyFixesInDocument;
            }

            if (this.errorListTableControl != null)
            {
                this.errorListTableControl.EntriesChanged -= this.ErrorListTableControl_EntriesChanged;
            }

            if (this.sarifErrorListEventSelectionService != null)
            {
                this.sarifErrorListEventSelectionService.NavigatedItemChanged -= this.SarifListErrorItemNavigated;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Recompute the list of fixable errors each time VS asks, because we might have fixed
            // some of them.
            IList<SarifErrorListItem> selectedErrors = this.GetSelectedErrors(this.errorsInFile);
            IList<SarifErrorListItem> selectedFixableErrors = GetFixableErrors(selectedErrors);
            var actionSet = new List<SuggestedActionSet>();
            actionSet.AddRange(this.CreateActionSetFromErrors(selectedFixableErrors) ?? Enumerable.Empty<SuggestedActionSet>());
            actionSet.AddRange(this.CreateSuppressionActionSetFromErrors(selectedErrors) ?? Enumerable.Empty<SuggestedActionSet>());
            return actionSet;
        }

        /// <inheritdoc/>
        public async Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            return this.GetSuggestedActions(requestedActionCategories, range, cancellationToken)?.Any() == true;
        }

        /// <inheritdoc/>
        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        private static IList<SarifErrorListItem> GetErrorsInFile(string fileName) =>
            CodeAnalysisResultManager
            .Instance
            .RunIndexToRunDataCache
            .Values
            .SelectMany(runDataCache => runDataCache.SarifErrors)
            .Where(error => string.Compare(fileName, error.FileName, StringComparison.OrdinalIgnoreCase) == 0)
            .ToList();

        private static IList<SarifErrorListItem> GetFixableErrors(IEnumerable<SarifErrorListItem> errors) =>
            errors
            .Where(error => error.IsFixable())
            .ToList();

        // Calculate persistent spans for each error location (because we want to display a
        // lightbulb any time the caret intersects such a span, even if the document has been
        // edited) and for each region that must be replaced when the error is fixed (because
        // we want to apply the fix in the right place, even if the document has been edited).
        private void CalculatePersistentSpans(IEnumerable<SarifErrorListItem> errors)
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
            var applyableFixes = new List<FixModel>();
            foreach (SarifErrorListItem error in errors)
            {
                applyableFixes.AddRange(error.Fixes.Where(fix => fix.CanBeAppliedToFile(error.FileName)));
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

        // Find all errors in the given list any of whose locations intersect the caret. Those are
        // the locations where a lightbulb should appear.
        private IList<SarifErrorListItem> GetSelectedErrors(IEnumerable<SarifErrorListItem> errors)
        {
            SnapshotPoint caretSnapshotPoint = this.textView.Caret.Position.BufferPosition;
            var caretSpanCollection =
                new NormalizedSnapshotSpanCollection(new SnapshotSpan(start: caretSnapshotPoint, end: caretSnapshotPoint));

            return errors.Where(error => this.CaretIntersectsAnyErrorLocation(error, caretSpanCollection)).ToList();
        }

        private bool CaretIntersectsAnyErrorLocation(SarifErrorListItem error, NormalizedSnapshotSpanCollection caretSpanCollection) =>
            error
            .Locations
            ?.Any(locationModel => this.CaretIntersectsSingleErrorLocation(locationModel, caretSpanCollection)) == true;

        private bool CaretIntersectsSingleErrorLocation(LocationModel locationModel, NormalizedSnapshotSpanCollection caretSpanCollection) =>
            caretSpanCollection.Any(
                caretSpan => locationModel.PersistentSpan != null && caretSpan.IntersectsWith(locationModel.PersistentSpan.Span.GetSpan(caretSpan.Snapshot)));

        private IEnumerable<SuggestedActionSet> CreateActionSetFromErrors(IEnumerable<SarifErrorListItem> errors)
        {
            this.fixToErrorDictionary.Clear();

            // Every error in the specified list has at least one fix that can be
            // applied, but we must provide only the apply-able ones.
            var suggestedActions = new List<ISuggestedAction>();
            foreach (SarifErrorListItem error in errors)
            {
                foreach (FixModel fix in error.Fixes.Where(fix => fix.CanBeAppliedToFile(error.FileName)))
                {
                    var suggestedAction = new FixSuggestedAction(error, fix, this.textBuffer, this.previewProvider);
                    this.fixToErrorDictionary.Add(suggestedAction, error);
                    suggestedAction.FixApplied += this.SuggestedAction_FixApplied;
                    suggestedActions.Add(suggestedAction);
                }
            }

            // If there are no actions, return null rather than an empty list. Otherwise VS will display
            // a light bulb with no suggestions in its dropdown. This way, VS refrains from displaying
            // the light bulb.
            return suggestedActions.Any()
                ? new List<SuggestedActionSet> { new SuggestedActionSet(SuggestionCategory, suggestedActions) }
                : null;
        }

        private IEnumerable<SuggestedActionSet> CreateSuppressionActionSetFromErrors(IEnumerable<SarifErrorListItem> errors)
        {
            var suggestedActions = new List<ISuggestedAction>();
            foreach (SarifErrorListItem error in errors)
            {
                if (error.VSSuppressionState != VSSuppressionState.Suppressed)
                {
                    suggestedActions.Add(new SuppressSuggestedAction(error));
                }
            }

            // If there are no actions, return null rather than an empty list. Otherwise VS will display
            // a light bulb with no suggestions in its dropdown. This way, VS refrains from displaying
            // the light bulb.
            return suggestedActions.Any() ?
                new List<SuggestedActionSet>
                {
                    new SuggestedActionSet(SuggestionCategory, suggestedActions),
                }
                :
                null;
        }

        private void SuggestedAction_FixApplied(object sender, EventArgs e)
        {
            if (sender is FixSuggestedAction suggestedAction)
            {
                if (this.fixToErrorDictionary.TryGetValue(suggestedAction, out SarifErrorListItem error))
                {
                    error.IsFixed = true;

                    SarifTableDataSource.Instance.RemoveError(error);

                    // Tell VS to recompute the list of suggested actions so we don't offer
                    // a fix for an error that's already fixed.
                    SuggestedActionsChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void ErrorListTableControl_EntriesChanged(object sender, EntriesChangedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.RefreshPersistentSpans();
        }

        private void SarifListErrorItemNavigated(object sender, SarifErrorListSelectionChangedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.RefreshPersistentSpans();
        }

        private void RefreshPersistentSpans()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // If this text buffer is not associated with a file, it cannot have any SARIF errors.
            this.errorsInFile = SdkUIUtilities.TryGetFileNameFromTextBuffer(this.textBuffer, out string fileName)
                ? GetErrorsInFile(fileName)
                : Enumerable.Empty<SarifErrorListItem>().ToList();
            this.CalculatePersistentSpans(this.errorsInFile);
        }
    }
}

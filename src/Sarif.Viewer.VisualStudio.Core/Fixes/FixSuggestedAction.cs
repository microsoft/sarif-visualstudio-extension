// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Sarif.Viewer.Models;
using Microsoft.Sarif.Viewer.Tags;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;

namespace Microsoft.Sarif.Viewer.Fixes
{
    /// <summary>
    /// An suggested action to fix a result in a SARIF log file.
    /// </summary>
    internal class FixSuggestedAction : ISuggestedAction
    {
        private readonly SarifErrorListItem sarifErrorListItem;
        private readonly FixModel fix;
        private readonly ITextBuffer textBuffer;
        private readonly IPreviewProvider previewProvider;
        private readonly IReadOnlyCollection<ReplacementEdit> edits;

        /// <summary>
        /// Initializes a new instance of the <see cref="FixSuggestedAction"/> class.
        /// </summary>
        /// <param name="errorListItem">
        /// SarifErrorListItem object which corresponds to this fix action.
        /// </param>
        /// <param name="fix">
        /// The SARIF <see cref="Fix"/> object that describes the action.
        /// </param>
        /// <param name="textBuffer">
        /// The text buffer to which the fix will be applied.
        /// </param>
        /// <param name="previewProvider">
        /// Creates the XAML UIControl that displays the preview.
        /// </param>
        public FixSuggestedAction(
            SarifErrorListItem errorListItem,
            FixModel fix,
            ITextBuffer textBuffer,
            IPreviewProvider previewProvider)
        {
            this.sarifErrorListItem = errorListItem;
            this.fix = fix;
            this.textBuffer = textBuffer;
            this.previewProvider = previewProvider;
            this.DisplayText = fix.Description;

            this.edits = this.GetEditsFromFix(fix).AsReadOnly();
        }

        public event EventHandler FixApplied;

        /// <inheritdoc/>
        public bool HasActionSets => false;

        /// <inheritdoc/>
        public string DisplayText { get; }

        /// <inheritdoc/>
        public ImageMoniker IconMoniker => default;

        /// <inheritdoc/>
        public string IconAutomationText => null;

        /// <inheritdoc/>
        public string InputGestureText => null;

        /// <inheritdoc/>
        public bool HasPreview => true;

        /// <inheritdoc/>
        public void Dispose() { }

        /// <inheritdoc/>
        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        /// <inheritdoc/>
        public async Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            if (this.edits.Count != 0)
            {
                return await this.previewProvider.CreateChangePreviewAsync(
                    this.sarifErrorListItem, this.textBuffer, this.ApplyTextEdits, this.DisplayText);
            }

            return null;
        }

        /// <inheritdoc/>
        public void Invoke(CancellationToken cancellationToken)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (this.edits.Count != 0)
            {
                try
                {
                    ITextSnapshot currentSnapshot = this.textBuffer.CurrentSnapshot;
                    this.ApplyTextEdits(this.textBuffer, currentSnapshot);

                    FixApplied?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                }
            }
        }

        /// <inheritdoc/>
        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        private List<ReplacementEdit> GetEditsFromFix(FixModel fix) =>
            fix.ArtifactChanges.SelectMany(ac => ac.Replacements).Select(this.ToEdit).ToList();

        private ReplacementEdit ToEdit(ReplacementModel replacement) =>
            new ReplacementEdit(replacement, this.textBuffer.CurrentSnapshot);

        private void ApplyTextEdits(ITextBuffer textbuffer, ITextSnapshot snapshot)
        {
            using (ITextEdit bufferEdit = textbuffer.CreateEdit())
            {
                foreach (ReplacementEdit edit in this.edits)
                {
                    SnapshotSpan translatedSpan = edit.Span.TranslateTo(snapshot, SpanTrackingMode.EdgeExclusive);
                    bufferEdit.Replace(translatedSpan.Span, edit.Text);
                }

                bufferEdit.Apply();
            }

            SarifLocationTagHelpers.RefreshTags(this.textBuffer);
        }
    }
}

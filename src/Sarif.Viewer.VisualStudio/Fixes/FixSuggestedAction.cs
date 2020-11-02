// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
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
        private readonly FixModel fix;
        private readonly ITextBuffer textBuffer;
        private readonly IPreviewProvider previewProvider;
        private readonly IReadOnlyCollection<ReplacementEdit> edits;

        /// <summary>
        /// Creates a new instance of <see cref="FixSuggestedAction"/>.
        /// </summary>
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
            FixModel fix,
            ITextBuffer textBuffer,
            IPreviewProvider previewProvider)
        {
            this.fix = fix;
            this.textBuffer = textBuffer;
            this.previewProvider = previewProvider;
            DisplayText = fix.Description;

            this.edits = GetEditsFromFix(fix).AsReadOnly();
        }

        public event EventHandler FixApplied;

        #region ISuggestedAction

        /// <inheritdoc/>
        public bool HasActionSets => false;

        /// <inheritdoc/>
        public string DisplayText { get; }

        /// <inheritdoc/>
        public ImageMoniker IconMoniker => default(ImageMoniker);

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
                    this.textBuffer, ApplyTextEdits, DisplayText);
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
                    var currentSnapshot = this.textBuffer.CurrentSnapshot;
                    ApplyTextEdits(this.textBuffer, currentSnapshot);

                    FixApplied?.Invoke(this, EventArgs.Empty);
                }
                catch
                {
                    // TODO: better handling.
                }
            }
        }

        /// <inheritdoc/>
        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        #endregion ISuggestedActionsSource

        private List<ReplacementEdit> GetEditsFromFix(FixModel fix) =>
            fix.ArtifactChanges.SelectMany(ac => ac.Replacements).Select(ToEdit).ToList();

        private ReplacementEdit ToEdit(ReplacementModel replacement) =>
            new ReplacementEdit(replacement, this.textBuffer.CurrentSnapshot);

        private void ApplyTextEdits(ITextBuffer textbuffer, ITextSnapshot snapshot)
        {
            using (ITextEdit bufferEdit = textbuffer.CreateEdit())
            {
                foreach (var edit in this.edits)
                {
                    var translatedSpan = edit.Span.TranslateTo(snapshot, SpanTrackingMode.EdgeExclusive);
                    bufferEdit.Replace(translatedSpan.Span, edit.Text);
                }

                bufferEdit.Apply();
            }

            SarifLocationTagHelpers.RefreshTags(textBuffer);
        }
    }
}

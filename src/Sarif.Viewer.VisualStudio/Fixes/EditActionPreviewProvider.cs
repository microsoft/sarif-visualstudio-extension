// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;

namespace Microsoft.Sarif.Viewer.Fixes
{
    [Export(typeof(IPreviewProvider))]
    internal class EditActionPreviewProvider : IPreviewProvider
    {
        internal event EventHandler<ApplyFixEventArgs> ApplyFixesInDocument;

        private readonly ITextViewRoleSet previewRoleSet;
        private readonly ITextBufferFactoryService textBufferFactoryService;
        private readonly ITextDocumentFactoryService textDocumentFactoryService;
        private readonly ITextDifferencingSelectorService textDifferencingSelectorService;
        private readonly IProjectionBufferFactoryService projectionBufferFactoryService;
        private readonly IEditorOptionsFactoryService editorOptionsFactoryService;
        private readonly IDifferenceBufferFactoryService differenceBufferFactoryService;
        private readonly IWpfDifferenceViewerFactoryService wpfDifferenceViewerFactoryService;

        [ImportingConstructor]
        internal EditActionPreviewProvider(
            ITextBufferFactoryService textBufferFactoryService,
            ITextDocumentFactoryService textDocumentFactoryService,
            ITextDifferencingSelectorService textDifferencingSelectorService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            IDifferenceBufferFactoryService differenceBufferFactoryService,
            IWpfDifferenceViewerFactoryService wpfDifferenceViewerFactoryService,
            ITextEditorFactoryService textEditorFactoryService)
        {
            this.textBufferFactoryService = textBufferFactoryService;
            this.textDocumentFactoryService = textDocumentFactoryService;
            this.textDifferencingSelectorService = textDifferencingSelectorService;
            this.projectionBufferFactoryService = projectionBufferFactoryService;
            this.editorOptionsFactoryService = editorOptionsFactoryService;
            this.differenceBufferFactoryService = differenceBufferFactoryService;
            this.wpfDifferenceViewerFactoryService = wpfDifferenceViewerFactoryService;

            this.previewRoleSet = textEditorFactoryService.CreateTextViewRoleSet(PredefinedTextViewRoles.Analyzable);
        }

        public Task<object> CreateChangePreviewAsync(
            SarifErrorListItem errorListItem,
            ITextBuffer buffer,
            Action<ITextBuffer,
            ITextSnapshot> applyEdit,
            string description = null,
            FrameworkElement additionalContent = null)
        {
            ITextBuffer bufferClone = this.CloneBuffer(buffer);

            applyEdit(bufferClone, buffer.CurrentSnapshot);

            IHierarchicalDifferenceCollection diffResult = this.ComputeDifferences(buffer, bufferClone);
            NormalizedSpanCollection originalSpans = this.GetOriginalSpans(diffResult);
            NormalizedSpanCollection changedSpans = this.GetChangedSpans(diffResult);

            List<LineSpan> originalLineSpans = this.CreateLineSpans(buffer.CurrentSnapshot, originalSpans);
            List<LineSpan> changedLineSpans = this.CreateLineSpans(bufferClone.CurrentSnapshot, changedSpans);

            if (!originalLineSpans.Any())
            {
                originalLineSpans = changedLineSpans;
            }

            if (!(originalLineSpans.Any() && changedLineSpans.Any()))
            {
                return Task.FromResult<object>(null);
            }

            const string Separator = "\u2026"; // Horizontal ellipsis.

            IProjectionBuffer originalProjectionBuffer = this.projectionBufferFactoryService.CreateProjectionBufferWithoutIndentation(
                this.editorOptionsFactoryService.GlobalOptions,
                buffer.CurrentSnapshot,
                Separator,
                originalLineSpans.ToArray());

            IProjectionBuffer newProjectionBuffer = this.projectionBufferFactoryService.CreateProjectionBufferWithoutIndentation(
                this.editorOptionsFactoryService.GlobalOptions,
                bufferClone.CurrentSnapshot,
                Separator,
                changedLineSpans.ToArray());

            return this.CreateNewDifferenceViewerAsync(errorListItem, originalProjectionBuffer, newProjectionBuffer, description, additionalContent);
        }

        private async Task<object> CreateNewDifferenceViewerAsync(
            SarifErrorListItem errorListItem,
            IProjectionBuffer originalBuffer,
            IProjectionBuffer newBuffer,
            string description,
            FrameworkElement additionalContent)
        {
            IDifferenceBuffer diffBuffer = this.differenceBufferFactoryService.CreateDifferenceBuffer(
                originalBuffer,
                newBuffer,
                options: default,
                disableEditing: true);

            IWpfDifferenceViewer diffViewer = this.wpfDifferenceViewerFactoryService.CreateDifferenceView(diffBuffer, this.previewRoleSet);

            diffViewer.Closed += (s, e) =>
            {
                // Workaround Editor bug.  The editor has an issue where they sometimes crash when
                // trying to apply changes to projection buffer.  So, when the user actually invokes
                // a SuggestedAction we may then edit a text buffer, which the editor will then
                // try to propagate through the projections we have here over that buffer.  To ensure
                // that that doesn't happen, we clear out the projections first so that this crash
                // won't happen.
                originalBuffer.DeleteSpans(0, originalBuffer.CurrentSnapshot.SpanCount);
                newBuffer.DeleteSpans(0, newBuffer.CurrentSnapshot.SpanCount);
            };

            diffViewer.ViewMode = DifferenceViewMode.Inline;
            diffViewer.InlineView.ZoomLevel *= 0.75;
            diffViewer.InlineHost.GetTextViewMargin("deltadifferenceViewerOverview").VisualElement.Visibility = Visibility.Collapsed;

            // Disable focus / tab stop for the diff viewer.
            diffViewer.RightView.VisualElement.Focusable = false;
            diffViewer.LeftView.VisualElement.Focusable = false;
            diffViewer.InlineView.VisualElement.Focusable = false;

            // We use ConfigureAwait(true) to stay on the UI thread.
            var sizeFitter = new SizeToFitHelper(diffViewer, 400.0);
            await sizeFitter.SizeToFitAsync();

            var diffViewerControl = new DisposableDifferenceViewerControl(errorListItem, diffViewer, description, additionalContent);
            diffViewerControl.ApplyFixesInDocument += (sender, e) =>
            {
                ApplyFixesInDocument?.Invoke(this, e);
            };
            return diffViewerControl;
        }

        private ITextBuffer CloneBuffer(ITextBuffer buffer)
        {
            return this.textBufferFactoryService.CreateTextBuffer(buffer.CurrentSnapshot.GetText(), buffer.ContentType);
        }

        private IHierarchicalDifferenceCollection ComputeDifferences(ITextBuffer oldBuffer, ITextBuffer newBuffer)
        {
            string oldText = oldBuffer.CurrentSnapshot.GetText();
            string newText = newBuffer.CurrentSnapshot.GetText();

            ITextDifferencingService diffService = this.textDifferencingSelectorService.GetTextDifferencingService(oldBuffer.ContentType)
                ?? this.textDifferencingSelectorService.DefaultTextDifferencingService;

            return diffService.DiffStrings(oldText, newText, new StringDifferenceOptions()
            {
                DifferenceType = StringDifferenceTypes.Word | StringDifferenceTypes.Line,
            });
        }

        private NormalizedSpanCollection GetOriginalSpans(IHierarchicalDifferenceCollection diffResult)
        {
            var lineSpans = new List<Span>();

            foreach (Difference difference in diffResult)
            {
                Span mappedSpan = diffResult.LeftDecomposition.GetSpanInOriginal(difference.Left);
                lineSpans.Add(mappedSpan);
            }

            return new NormalizedSpanCollection(lineSpans);
        }

        private NormalizedSpanCollection GetChangedSpans(IHierarchicalDifferenceCollection diffResult)
        {
            var lineSpans = new List<Span>();

            foreach (Difference difference in diffResult)
            {
                Span mappedSpan = diffResult.RightDecomposition.GetSpanInOriginal(difference.Right);
                lineSpans.Add(mappedSpan);
            }

            return new NormalizedSpanCollection(lineSpans);
        }

        private List<LineSpan> CreateLineSpans(ITextSnapshot textSnapshot, NormalizedSpanCollection allSpans)
        {
            var result = new List<LineSpan>();

            foreach (Span span in allSpans)
            {
                LineSpan lineSpan = this.GetLineSpan(textSnapshot, span);
                this.MergeLineSpans(result, lineSpan);
            }

            return result;
        }

        // Find the lines that surround the span of the difference.  Try to expand the span to
        // include both the previous and next lines so that we can show more context to the
        // user.
        private LineSpan GetLineSpan(
            ITextSnapshot snapshot,
            Span span)
        {
            int startLine = snapshot.GetLineNumberFromPosition(span.Start);
            int endLine = snapshot.GetLineNumberFromPosition(span.End);

            if (startLine > 0)
            {
                startLine--;
            }

            if (endLine < snapshot.LineCount)
            {
                endLine++;
            }

            return new LineSpan(startLine, endLine);
        }

        // Adds a line span to the spans we've been collecting.  If the line span overlaps or
        // abuts a previous span then the two are merged.
        private void MergeLineSpans(List<LineSpan> lineSpans, LineSpan nextLineSpan)
        {
            if (lineSpans.Count > 0)
            {
                LineSpan lastLineSpan = lineSpans.Last();

                // We merge them if there's no more than one line between the two.  Otherwise
                // we'd show "..." between two spans where we could just show the actual code.
                if (nextLineSpan.Start >= lastLineSpan.Start && nextLineSpan.Start <= (lastLineSpan.End + 1))
                {
                    nextLineSpan = new LineSpan(lastLineSpan.Start, nextLineSpan.End);
                    lineSpans.RemoveAt(lineSpans.Count - 1);
                }
            }

            lineSpans.Add(nextLineSpan);
        }
    }
}

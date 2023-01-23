// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;

using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.Sarif.Viewer.Tags
{
    internal class KeyEventAdornmentsTagger : IntraTextAdornmentTagger<ITextMarkerTag, KeyEventAdornment>
    {
        internal static ITagger<IntraTextAdornmentTag> GetTagger(
            IWpfTextView view,
            IViewTagAggregatorFactoryService tagAggregatorFactoryService,
            Lazy<ITagAggregator<ITextMarkerTag>> sarifTextMarkerTagger,
            ISarifErrorListEventSelectionService sarifErrorListEventSelectionService)
        {
            return view.Properties.GetOrCreateSingletonProperty(
                () => new KeyEventAdornmentsTagger(
                    view,
                    tagAggregatorFactoryService.CreateTagAggregator<ITextMarkerTag>(view),
                    sarifTextMarkerTagger.Value,
                    sarifErrorListEventSelectionService));
        }

        private SarifErrorListItem currentErrorListItem;

        private readonly ITagAggregator<ITextMarkerTag> tagAggregator;

        private readonly ITagAggregator<ITextMarkerTag> sarifTextMarkerTagger;

        private readonly ISarifErrorListEventSelectionService sarifErrorListEventSelectionService;

        private KeyEventAdornmentsTagger(IWpfTextView view, ITagAggregator<ITextMarkerTag> tagAggregator, ITagAggregator<ITextMarkerTag> sarifTextMarkerTagger, ISarifErrorListEventSelectionService sarifErrorListEventSelectionService)
            : base(view)
        {
            this.tagAggregator = tagAggregator;
            this.sarifTextMarkerTagger = sarifTextMarkerTagger;
            this.sarifErrorListEventSelectionService = sarifErrorListEventSelectionService;
            this.sarifErrorListEventSelectionService.NavigatedItemChanged += this.SarifErrorListEventSelectionService_NavigatedItemChanged;
            this.tagAggregator.BatchedTagsChanged += this.TagAggregator_BatchedTagsChanged;
        }

        private void SarifErrorListEventSelectionService_NavigatedItemChanged(object sender, SarifErrorListSelectionChangedEventArgs e)
        {
            if (this.currentErrorListItem != e.NewItem)
            {
                this.currentErrorListItem = e.NewItem;
            }

            this.adornmentCache.Clear();
        }

        private void TagAggregator_BatchedTagsChanged(object sender, BatchedTagsChangedEventArgs e)
        {
        }

        public void Dispose()
        {
            sarifTextMarkerTagger.Dispose();

            view.Properties.RemoveProperty(typeof(KeyEventAdornmentsTagger));
        }

        // To produce adornments that don't obscure the text, the adornment tags
        // should have zero length spans. Overriding this method allows control
        // over the tag spans.
        protected override IEnumerable<Tuple<SnapshotSpan, PositionAffinity?, ITextMarkerTag, int>> GetAdornmentData(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0 || this.view.TextViewLines == null)
            {
                yield break;
            }

            ITextSnapshot snapshot = spans[0].Snapshot;

            /*
            IEnumerable<IMappingTagSpan<ITextMarkerTag>> textMarkerTags =
                this.sarifTextMarkerTagger.GetTags(spans).Where(t => t.Tag is SarifLocationTextMarkerTag sarifTag && sarifTag.Context is AnalysisStepNode).ToList();
            */

            IEnumerable<IMappingTagSpan<ITextMarkerTag>> tags =
                this.tagAggregator.GetTags(spans)
                .Where(t => t.Tag is SarifLocationTextMarkerTag sarifTag && sarifTag.Context is AnalysisStepNode).ToList();

            int maxLineLength = this.GetMaxCharLength(tags);

            foreach (IMappingTagSpan<ITextMarkerTag> dataTagSpan in tags)
            {
                if (this.TryMapToSingleSnapshotSpan(dataTagSpan.Span, this.view.TextSnapshot, out SnapshotSpan span) &&
                        this.view.TextViewLines.IntersectsBufferSpan(span))
                {
                    IWpfTextViewLine containingBufferPosition =
                            this.view.GetTextViewLineContainingBufferPosition(span.Start);
                    if (containingBufferPosition != null)
                    {
                        int prefixLength = maxLineLength - this.NormalizeTextLength(containingBufferPosition);

                        SnapshotSpan adornmentSpan = new SnapshotSpan(containingBufferPosition.Extent.End, 0);

                        yield return Tuple.Create(adornmentSpan, (PositionAffinity?)PositionAffinity.Successor, dataTagSpan.Tag, prefixLength);
                    }
                }

                /*
                NormalizedSnapshotSpanCollection textMarkerTagSpans = dataTagSpan.Span.GetSpans(snapshot);

                // Ignore data tags that are split by projection.
                // This is theoretically possible but unlikely in current scenarios.
                if (textMarkerTagSpans.Count != 1)
                {
                    continue;
                }

                IWpfTextViewLine containingBufferPosition =
                    this.view.GetTextViewLineContainingBufferPosition(textMarkerTagSpans[0].Start);

                if (containingBufferPosition != null)
                {
                    int prefixLength = maxLineLength - this.NormalizeTextLength(containingBufferPosition);

                    SnapshotSpan adornmentSpan = new SnapshotSpan(containingBufferPosition.Extent.End, 0);

                    yield return Tuple.Create(adornmentSpan, (PositionAffinity?)PositionAffinity.Successor, dataTagSpan.Tag, prefixLength);
                }
                */
            }
        }

        protected override KeyEventAdornment CreateAdornment(IList<ITextMarkerTag> dataTags, SnapshotSpan span, int prefixLength)
        {
            if (this.view.FormattedLineSource == null)
            {
                return null;
            }

            return new KeyEventAdornment(
                dataTags,
                prefixLength,
                this.view.FormattedLineSource.DefaultTextProperties.FontRenderingEmSize,
                this.view.FormattedLineSource.DefaultTextProperties.Typeface.FontFamily,
                InlineLink_Click);
        }

        protected override bool UpdateAdornment(KeyEventAdornment adornment, IList<ITextMarkerTag> dataTag)
        {
            adornment.Update(dataTag);
            return true;
        }

        internal void InlineLink_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!(sender is Hyperlink hyperLink))
            {
                return;
            }

            if (hyperLink.Tag is int id)
            {
                AnalysisStepNode node = this.currentErrorListItem.AnalysisSteps?.First()?.TopLevelNodes?.FirstOrDefault(n => n.Index == id);

                if (node == null)
                {
                    return;
                }

                node.NavigateTo(usePreviewPane: false, moveFocusToCaretLocation: true);
            }

            if (hyperLink.Tag is LinkTag linkTag)
            {
                IOrderedEnumerable<AnalysisStepNode> nodes = this.currentErrorListItem.AnalysisSteps?.First()?.TopLevelNodes?
                    .Where(n => n.State.Any(s => s.Expression == linkTag.StateKey)).OrderBy(n => n.Index);

                AnalysisStepNode node;
                if (linkTag.Forward)
                {
                    node = nodes?.FirstOrDefault(n => n.Index > linkTag.Index);
                }
                else
                {
                    node = nodes?.FirstOrDefault(n => n.Index < linkTag.Index);
                }

                node?.NavigateTo(usePreviewPane: false, moveFocusToCaretLocation: true);
            }
        }

        private int GetMaxCharLength(IEnumerable<IMappingTagSpan<ITextMarkerTag>> tags)
        {
            int maxLength = 0;
            foreach (IMappingTagSpan<ITextMarkerTag> imappingTagSpan in tags)
            {
                if (imappingTagSpan.Tag is SarifLocationTextMarkerTag sarifTag &&
                    sarifTag.Context is AnalysisStepNode stepNode)
                {
                    if (this.TryMapToSingleSnapshotSpan(imappingTagSpan.Span, this.view.TextSnapshot, out SnapshotSpan span) &&
                        this.view.TextViewLines.IntersectsBufferSpan(span))
                    {
                        IWpfTextViewLine line =
                            this.view.TextViewLines.GetTextViewLineContainingBufferPosition(span.Start);
                        maxLength = Math.Max(maxLength, this.NormalizeTextLength(line));
                    }
                }
            }

            return maxLength;
        }

        private bool TryMapToSingleSnapshotSpan(
            IMappingSpan mappingSpan,
            ITextSnapshot viewSnapshot,
            out SnapshotSpan span)
        {
            if (viewSnapshot != null && mappingSpan.AnchorBuffer == viewSnapshot.TextBuffer)
            {
                SnapshotPoint? point1 = mappingSpan.Start.GetPoint(viewSnapshot, PositionAffinity.Predecessor);
                SnapshotPoint? point2 = mappingSpan.End.GetPoint(viewSnapshot, PositionAffinity.Successor);
                span = new SnapshotSpan(point1.Value, point2.Value);
                return true;
            }

            NormalizedSnapshotSpanCollection spans = mappingSpan.GetSpans(viewSnapshot);
            if (spans.Count >= 1)
            {
                span = spans[0];
                return true;
            }

            span = default(SnapshotSpan);
            return false;
        }

        private int NormalizeTextLength(IWpfTextViewLine line)
        {
            int tabWidth = 4;
            string text;

            if (line == null || (text = line.Extent.GetText()) == null)
            {
                return 0;
            }

            int tabCount = text.Count(c => c == '\t');
            return (tabCount * (tabWidth - 1)) + text.Length;
        }
    }
}

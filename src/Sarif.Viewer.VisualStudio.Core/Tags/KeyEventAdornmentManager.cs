// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.Sarif.Viewer.Options;
using Microsoft.Sarif.Viewer.Services;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.Sarif.Viewer.Tags
{
    /// <summary>
    /// Places key event text adornments in the editor window.
    /// </summary>
    internal class KeyEventAdornmentManager
    {
        /// <summary>
        /// The layer of the adornment.
        /// </summary>
        private readonly IAdornmentLayer layer;

        /// <summary>
        /// Text view where the adornment is created.
        /// </summary>
        private readonly IWpfTextView view;

        private readonly ISarifErrorListEventSelectionService sarifEventService;

        private readonly ITagAggregator<ITextMarkerTag> tagAggregator;

        private IList<IMappingTagSpan<ITextMarkerTag>> currentSarifTags;

        private SarifErrorListItem currentErrorListItem;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyEventAdornmentManager"/> class.
        /// </summary>
        /// <param name="view">Text view to create the adornment for.</param>
        /// <param name="sarifErrorListEventService"><see cref="ISarifErrorListEventSelectionService" />.</param>
        /// <param name="tagAggregatorFactoryService"><see cref="IViewTagAggregatorFactoryService" />.</param>
        internal KeyEventAdornmentManager(
            IWpfTextView view,
            ISarifErrorListEventSelectionService sarifErrorListEventService,
            IViewTagAggregatorFactoryService tagAggregatorFactoryService)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            this.currentSarifTags = new List<IMappingTagSpan<ITextMarkerTag>>();

            this.layer = view.GetAdornmentLayer(nameof(KeyEventAdornmentManager));

            this.view = view;
            this.view.LayoutChanged += this.View_LayoutChanged;
            this.view.GotAggregateFocus += this.View_GotAggregateFocus;
            this.view.Closed += this.View_Closed;

            this.sarifEventService = sarifErrorListEventService;
            this.sarifEventService.NavigatedItemChanged += this.SarifEventService_NavigatedItemChanged;

            // Not able to get tag aggregator using type SarifLocationTextMarkerTag
            this.tagAggregator = tagAggregatorFactoryService.CreateTagAggregator<ITextMarkerTag>(this.view);

            SarifViewerPackage.LoadViewerPackage();
        }

        private void View_GotAggregateFocus(object sender, EventArgs e)
        {
            this.RefreshAdornments();
        }

        private void View_Closed(object sender, EventArgs e)
        {
            ErrorListService.CloseSarifLogs(new[] { DataService.EnhancedResultDataLogName });
        }

        /// <summary>
        /// Handles whenever the text displayed in the view changes by adding the adornment to any reformatted lines.
        /// </summary>
        /// <remarks><para>This event is raised whenever the rendered text displayed in the <see cref="ITextView"/> changes.</para>
        /// <para>It is raised whenever the view does a layout (which happens when DisplayTextLineContainingBufferPosition is called or in response to text or classification changes).</para>
        /// <para>It is also raised whenever the view scrolls horizontally or when its size changes.</para>
        /// </remarks>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        internal void View_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            this.RefreshAdornments();
        }

        private void RefreshAdornments()
        {
            this.InvalidateAdornments();

            if (SarifViewerOption.Instance?.IsKeyEventAdornmentEnabled != true)
            {
                return;
            }

            this.currentSarifTags = this.GetCurrentSarifTextMarkerTags();

            if (this.currentSarifTags.Any())
            {
                // There may be multiple tags for 1 line.
                IDictionary<IWpfTextViewLine, IList<ITextMarkerTag>> linesContainsKeyEvent =
                    GetLinesContainsTextMarker(this.currentSarifTags);

                // Get length of the longest line.
                int maxSpaceChar = linesContainsKeyEvent.Any() ?
                    linesContainsKeyEvent.Keys.Max(line => line.End.Position - line.Start.Position) :
                    0;

                foreach (KeyValuePair<IWpfTextViewLine, IList<ITextMarkerTag>> line in linesContainsKeyEvent)
                {
                    this.CreateVisuals(line.Key, line.Value, maxSpaceChar);
                }
            }
        }

        private void SarifEventService_NavigatedItemChanged(object sender, SarifErrorListSelectionChangedEventArgs e)
        {
            // Not able to get all SarifLocationTextMarkerTags after this event fired
            // may because the editor UI updated async
            if (this.currentErrorListItem != e.NewItem)
            {
                this.currentErrorListItem = e.NewItem;
            }
        }

        private void InvalidateAdornments()
        {
            this.layer.RemoveAllAdornments();
        }

        private IList<IMappingTagSpan<ITextMarkerTag>> GetCurrentSarifTextMarkerTags()
        {
            IEnumerable<IMappingTagSpan<ITextMarkerTag>> tags =
                this.tagAggregator.GetTags(this.view.TextViewLines.FormattedSpan)
                .Where(t => t.Tag is SarifLocationTextMarkerTag sarifTag && sarifTag.Context is AnalysisStepNode);

            return tags.ToList();
        }

        private IDictionary<IWpfTextViewLine, IList<ITextMarkerTag>> GetLinesContainsTextMarker(IEnumerable<IMappingTagSpan<ITextMarkerTag>> tags)
        {
            var tagMaps = new Dictionary<IWpfTextViewLine, IList<ITextMarkerTag>>();
            foreach (IMappingTagSpan<ITextMarkerTag> imappingTagSpan in tags)
            {
                if (imappingTagSpan.Tag is SarifLocationTextMarkerTag sarifTag &&
                    sarifTag.Context is AnalysisStepNode stepNode)
                {
                    if (this.TryMapToSingleSnapshotSpan(imappingTagSpan.Span, this.view.TextSnapshot, out SnapshotSpan span) &&
                        this.view.TextViewLines.IntersectsBufferSpan(span))
                    {
                        IWpfTextViewLine containingBufferPosition =
                            this.view.TextViewLines.GetTextViewLineContainingBufferPosition(span.Start);
                        if (containingBufferPosition != null)
                        {
                            if (!tagMaps.ContainsKey(containingBufferPosition))
                            {
                                tagMaps.Add(containingBufferPosition, new List<ITextMarkerTag>());
                            }

                            tagMaps[containingBufferPosition].Add(imappingTagSpan.Tag);
                        }
                    }
                }
            }

            return tagMaps;
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

        /// <summary>
        /// Create TextBlock control at end of the given lines.
        /// </summary>
        /// <param name="line">Line to add the adornments.</param>
        private void CreateVisuals(ITextViewLine line, IList<ITextMarkerTag> tags, int maxSpaceChar)
        {
            SnapshotSpan extent = line.Extent;
            Geometry geometry = this.view.TextViewLines.GetMarkerGeometry(extent);
            if (geometry != null)
            {
                int charLength = line.End.Position - line.Start.Position;
                int numberOfSpace = maxSpaceChar - charLength;

                UIElement tagGraphic = new KeyEventAdornment(
                    tags,
                    numberOfSpace,
                    this.view.FormattedLineSource.DefaultTextProperties.FontRenderingEmSize,
                    this.view.FormattedLineSource.DefaultTextProperties.Typeface.FontFamily);

                // element.Measure(new Size(1000.0, 20.0));
                Canvas.SetLeft(tagGraphic, geometry.Bounds.Right);
                Canvas.SetTop(tagGraphic, geometry.Bounds.Top);

                if (tagGraphic != null)
                {
                    this.layer.AddAdornment(
                        AdornmentPositioningBehavior.TextRelative,
                        new SnapshotSpan?(extent),
                        tags[0],
                        tagGraphic,
                        null);
                }
            }
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.Sarif.Viewer.Tags
{
    internal abstract class IntraTextAdornmentTagger<TData, TAdornment>
            : ITagger<IntraTextAdornmentTag>
            where TAdornment : UIElement
    {
        private readonly List<SnapshotSpan> invalidatedSpans = new List<SnapshotSpan>();

        protected Dictionary<SnapshotSpan, TAdornment> adornmentCache = new Dictionary<SnapshotSpan, TAdornment>();

        protected readonly IWpfTextView view;

        protected ITextSnapshot Snapshot { get; private set; }

        protected IntraTextAdornmentTagger(IWpfTextView view)
        {
            this.view = view;
            Snapshot = view.TextBuffer.CurrentSnapshot;

            this.view.LayoutChanged += HandleLayoutChanged;
            this.view.TextBuffer.Changed += HandleBufferChanged;
        }

        protected abstract TAdornment CreateAdornment(IList<TData> data, SnapshotSpan span, int prefixLength);

        protected abstract bool UpdateAdornment(TAdornment adornment, IList<TData> data);

        protected abstract IEnumerable<Tuple<SnapshotSpan, PositionAffinity?, TData, int>> GetAdornmentData(NormalizedSnapshotSpanCollection spans);

        private void HandleLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            SnapshotSpan visibleSpan = view.TextViewLines.FormattedSpan;

            // Filter out the adornments that are no longer visible.
            var toRemove = new List<SnapshotSpan>(
                from keyValuePair
                in adornmentCache
                where !keyValuePair.Key.TranslateTo(visibleSpan.Snapshot, SpanTrackingMode.EdgeExclusive).IntersectsWith(visibleSpan)
                select keyValuePair.Key);

            foreach (SnapshotSpan span in toRemove)
            {
                // adornmentCache.Remove(span);
            }

            _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                this.AsyncUpdate();
            });
        }

        private void HandleBufferChanged(object sender, TextContentChangedEventArgs args)
        {
            var editedSpans = args.Changes.Select(change => new SnapshotSpan(args.After, change.NewSpan)).ToList();
            InvalidateSpans(editedSpans);
        }

        protected void InvalidateSpans(IList<SnapshotSpan> spans)
        {
            lock (invalidatedSpans)
            {
                bool wasEmpty = invalidatedSpans.Count == 0;
                invalidatedSpans.AddRange(spans);

                if (wasEmpty && this.invalidatedSpans.Count > 0)
                {
                    _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        this.AsyncUpdate();
                    });

                    // _ = view.VisualElement.Dispatcher.BeginInvoke(new Action(AsyncUpdate));
                }
            }
        }

        protected void AsyncUpdate()
        {
            // Store the snapshot that we're now current with and send an event
            // for the text that has changed.
            if (Snapshot != view.TextBuffer.CurrentSnapshot)
            {
                Snapshot = view.TextBuffer.CurrentSnapshot;

                var translatedAdornmentCache = new Dictionary<SnapshotSpan, TAdornment>();

                foreach (KeyValuePair<SnapshotSpan, TAdornment> keyValuePair in adornmentCache)
                {
                    translatedAdornmentCache.Add(keyValuePair.Key.TranslateTo(Snapshot, SpanTrackingMode.EdgeExclusive), keyValuePair.Value);
                }

                adornmentCache = translatedAdornmentCache;
            }

            List<SnapshotSpan> translatedSpans;
            lock (invalidatedSpans)
            {
                translatedSpans = invalidatedSpans.Select(s => s.TranslateTo(Snapshot, SpanTrackingMode.EdgeInclusive)).ToList();
                invalidatedSpans.Clear();
            }

            if (translatedSpans.Count == 0)
            {
                return;
            }

            SnapshotPoint start = translatedSpans.Select(span => span.Start).Min();
            SnapshotPoint end = translatedSpans.Select(span => span.End).Max();

            RaiseTagsChanged(new SnapshotSpan(start, end));
        }

        protected void RaiseTagsChanged(SnapshotSpan span)
        {
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
        }

        // Produces tags on the snapshot that the tag consumer asked for.
        public virtual IEnumerable<ITagSpan<IntraTextAdornmentTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans == null || spans.Count == 0)
            {
                yield break;
            }

            // Translate the request to the snapshot that this tagger is current with.
            ITextSnapshot requestedSnapshot = spans[0].Snapshot;

            var translatedSpans = new NormalizedSnapshotSpanCollection(spans.Select(span => span.TranslateTo(Snapshot, SpanTrackingMode.EdgeExclusive)));

            // Grab the adornments.
            foreach (TagSpan<IntraTextAdornmentTag> tagSpan in GetAdornmentTagsOnSnapshot(translatedSpans))
            {
                // Translate each adornment to the snapshot that the tagger was asked about.
                SnapshotSpan span = tagSpan.Span.TranslateTo(requestedSnapshot, SpanTrackingMode.EdgeExclusive);
                var adornment = tagSpan.Tag.Adornment as KeyEventAdornment;

                // double textHeight = this.view.FormattedLineSource.LineHeight * adornment.
                var tag = new IntraTextAdornmentTag(
                    tagSpan.Tag.Adornment,
                    tagSpan.Tag.RemovalCallback,
                    topSpace: 0.0,
                    baseline: 12.0,
                    textHeight: adornment.ActualHeight,
                    bottomSpace: 0.0,
                    tagSpan.Tag.Affinity);
                yield return new TagSpan<IntraTextAdornmentTag>(span, tag);
            }
        }

        // Produces tags on the snapshot that this tagger is current with.
        private IEnumerable<TagSpan<IntraTextAdornmentTag>> GetAdornmentTagsOnSnapshot(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
            {
                yield break;
            }

            ITextSnapshot snapshot = spans[0].Snapshot;

            System.Diagnostics.Debug.Assert(snapshot == this.Snapshot, "Snapshot should be same.");

            // Since WPF UI objects have state (like mouse hover or animation) and are relatively expensive to create and lay out,
            // this code tries to reuse controls as much as possible.
            // The controls are stored in this.adornmentCache between the calls.

            // Mark which adornments fall inside the requested spans with Keep=false
            // so that they can be removed from the cache if they no longer correspond to data tags.
            var toRemove = new HashSet<SnapshotSpan>();
            foreach (KeyValuePair<SnapshotSpan, TAdornment> ar in adornmentCache)
            {
                if (spans.IntersectsWith(new NormalizedSnapshotSpanCollection(ar.Key)))
                {
                    toRemove.Add(ar.Key);
                }
            }

            var adornmentsData = GetAdornmentData(spans).ToList();

            // var distinctAdormentData = adornmentsData.Distinct(new AdornmentDataComparer()).ToList();
            var spanTagsMap = new Dictionary<SnapshotSpan, IList<Tuple<SnapshotSpan, PositionAffinity?, TData, int>>>();
            foreach (var data in adornmentsData)
            {
                if (!spanTagsMap.ContainsKey(data.Item1))
                {
                    spanTagsMap[data.Item1] = new List<Tuple<SnapshotSpan, PositionAffinity?, TData, int>>();
                }

                spanTagsMap[data.Item1].Add(data);
            }

            foreach (var spanDataPairs in spanTagsMap)
            {
                var spanDataPairList = spanDataPairs.Value;

                // Look up the corresponding adornment or create one if it's new.
                TAdornment adornment;
                SnapshotSpan snapshotSpan = spanDataPairs.Key;
                PositionAffinity? affinity = spanDataPairList.First().Item2;
                IList<TData> adornmentData = spanDataPairList.Select(x => x.Item3).ToList();
                int prefixLength = spanDataPairList.First().Item4;

                if (adornmentCache.TryGetValue(snapshotSpan, out adornment))
                {
                    if (UpdateAdornment(adornment, adornmentData))
                    {
                        toRemove.Remove(snapshotSpan);
                    }
                }
                else
                {
                    adornment = CreateAdornment(adornmentData, snapshotSpan, prefixLength);

                    if (adornment == null)
                    {
                        continue;
                    }

                    // Get the adornment to measure itself. Its DesiredSize property is used to determine
                    // how much space to leave between text for this adornment.
                    // Note: If the size of the adornment changes, the line will be reformatted to accommodate it.
                    // Note: Some adornments may change size when added to the view's visual tree due to inherited
                    // dependency properties that affect layout. Such options can include SnapsToDevicePixels,
                    // UseLayoutRounding, TextRenderingMode, TextHintingMode, and TextFormattingMode. Making sure
                    // that these properties on the adornment match the view's values before calling Measure here
                    // can help avoid the size change and the resulting unnecessary re-format.
                    adornment.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                    adornmentCache.Add(snapshotSpan, adornment);
                }

                yield return new TagSpan<IntraTextAdornmentTag>(snapshotSpan, new IntraTextAdornmentTag(adornment, null, affinity));
            }

            foreach (SnapshotSpan snapshotSpan in toRemove)
            {
                adornmentCache.Remove(snapshotSpan);
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        private class AdornmentDataComparer : IEqualityComparer<Tuple<SnapshotSpan, PositionAffinity?, TData, int>>
        {
            public bool Equals(Tuple<SnapshotSpan, PositionAffinity?, TData, int> x, Tuple<SnapshotSpan, PositionAffinity?, TData, int> y)
            {
                if (x == null && y == null)
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                return x.Item1.Equals(y.Item1);
            }

            public int GetHashCode(Tuple<SnapshotSpan, PositionAffinity?, TData, int> obj)
            {
                return obj.Item1.GetHashCode();
            }
        }
    }
}

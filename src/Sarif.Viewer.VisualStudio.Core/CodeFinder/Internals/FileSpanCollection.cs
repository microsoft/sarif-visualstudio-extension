// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Sarif.Viewer.CodeFinder.Internal
{
    /// <summary>
    /// This is an immutable collection of FileSpan objects, optimized for retrieving the next
    /// (or previous) FileSpan from a given position.
    /// </summary>
    public class FileSpanCollection
    {
        // How this class works:
        // Instead of having a single list or hashset of FileSpans, we try to optimize for
        // requests for the next or previous FileSpan for a given position by defining
        // buckets of FileSpans. Each bucket contains a sorted list of FileSpans.
        // We maintain 2 sets of buckets:
        //   1. "Start Buckets" where the bucket is determined by dividing the FileSpan's Start by the bucket size.
        //      This set is used for looking up the next FileSpan for a given position. The given position
        //      is divided by the bucket size, which gives us the bucket index. We then go through the
        //      FileSpans in that bucket until we find the first one whose Start is greater than the given
        //      position. (We may search subsequent buckets if necessary.)
        //   2. "End Buckets" where the bucket is determined by dividing the FileSpan's End by the bucket size.
        //      This set is used for looking up the previous FileSpan for a given position, the algorithm
        //      for which is similar to looking up the next FileSpan, but looking at FileSpan.End instead.
        // The idea is that by breaking the list of FileSpans into buckets we can quickly jump to the set
        // of relevant FileSpans in the collection for any given position.

        // This is the size of each bucket, in file positions. The smaller this is, the more buckets we will
        // have and the smaller the list of FileSpans each bucket will have.
        private readonly int bucketSize;

        // Bucketized sets of file spans, sorted by starting position.
        private readonly Dictionary<int, SortedSet<FileSpan>> startBuckets;
        private readonly FileSpanComparer startComparer;
        private readonly int lastStartIndex; // The last index of the startBuckets dictionary.

        // Bucketized sets of file spans, sorted by ending position.
        private readonly Dictionary<int, SortedSet<FileSpan>> endBuckets;
        private readonly FileSpanReverseEndComparer endComparer;

        /// <summary>
        /// Gets the number of FileSpans contained within the collection.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Gets the smallest start position of the collection.
        /// </summary>
        public int MinPosition { get; private set; }

        /// <summary>
        /// Gets the largest end position in the collection.
        /// </summary>
        public int MaxPosition { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSpanCollection"/> class.
        /// Creates a FileSpanCollection from a list of FileSpans.
        /// </summary>
        /// <param name="spans">The list of FileSpans to build into a collection. The FileSpans should not overlap.</param>
        /// <param name="bucketSize">Optional and not recommended except for testing. Sets the size of each bucket into which the
        /// given spans are placed. The smaller the number the higher the number of buckets.</param>
        public FileSpanCollection(List<FileSpan> spans, int? bucketSize = null)
        {
            if (bucketSize != null && bucketSize.Value <= 0)
            {
                throw new ArgumentException("bucketSize must be non-zero");
            }

            startBuckets = new Dictionary<int, SortedSet<FileSpan>>();
            endBuckets = new Dictionary<int, SortedSet<FileSpan>>();

            startComparer = new FileSpanComparer();
            endComparer = new FileSpanReverseEndComparer();

            if (spans == null || spans.Count == 0)
            {
                // If there aren't any spans, set some basic values so we don't fall over and then return.
                this.bucketSize = 1;
                this.lastStartIndex = 0;
                Count = 0;
                MinPosition = 0;
                MaxPosition = 0;
                return;
            }

            Count = spans.Count;

            if (bucketSize != null)
            {
                // Accept the given bucket size.
                this.bucketSize = bucketSize.Value;
            }
            else
            {
                // Sort the spans so that we can determine the min and max.
                spans.Sort(startComparer);
                int startMin = spans.First().Start;
                int startMax = spans.Last().Start;

                // Calculate the bucket size.
                // We want to try to keep the number of spans in each bucket small so we'll lean towards having more buckets rather than fewer.
                int bucketCount = Math.Max(1, Count / 10);
                this.bucketSize = Math.Max(1, (startMax - startMin) / bucketCount);
            }

            // Add spans to the buckets sorted by Start.
            foreach (FileSpan span in spans)
            {
                int index = GetIndex(span.Start);
                if (startBuckets.ContainsKey(index) == false)
                {
                    startBuckets.Add(index, new SortedSet<FileSpan>(startComparer));
                }

                startBuckets[index].Add(span);

                lastStartIndex = index;
            }

            // Add spans to the buckets sorted by End.
            foreach (FileSpan span in spans)
            {
                int index = GetIndex(span.End);
                if (endBuckets.ContainsKey(index) == false)
                {
                    endBuckets.Add(index, new SortedSet<FileSpan>(endComparer));
                }

                endBuckets[index].Add(span);
            }

            MinPosition = startBuckets.Values.First().First().Start;
            MaxPosition = endBuckets.Values.Last().First().End;
        }

        private int GetIndex(int pos)
        {
            return pos / bucketSize;
        }

        private SortedSet<FileSpan> GetSpansInStartBucket(int index)
        {
            if (startBuckets.ContainsKey(index))
            {
                return startBuckets[index];
            }

            return new SortedSet<FileSpan>();
        }

        private SortedSet<FileSpan> GetSpansInEndBucket(int index)
        {
            if (endBuckets.ContainsKey(index))
            {
                return endBuckets[index];
            }

            return new SortedSet<FileSpan>();
        }

        /// <summary>
        /// Returns true if the given start + length pair is contained within any span in the collection.
        /// </summary>
        /// <param name="start">The starting position to check.</param>
        /// <param name="length">The length of the span to check. Optional, defaults to 1.</param>
        /// <param name="excludeTags">Optional. Won't check against spans with at least one of these tags.</param>
        /// <returns>True if the start + length is contained in a span in this collection.</returns>
        public bool Contains(int start, int length = 1, List<FileSpan.FileSpanTag> excludeTags = null)
        {
            return Contains(new FileSpan(start, start + length - 1), excludeTags);
        }

        /// <summary>
        /// Returns true if the given span is contained within any span in the collection.
        /// </summary>
        /// <param name="fileSpan">The file span to seek for.</param>
        /// <param name="excludeTags">Optional. Won't check against spans with at least one of these tags.</param>
        /// <returns>True if the given span is contained in this collection.</returns>
        public bool Contains(FileSpan fileSpan, List<FileSpan.FileSpanTag> excludeTags = null)
        {
            int index = GetIndex(fileSpan.Start);

            while (index >= 0)
            {
                SortedSet<FileSpan> spans = GetSpansInStartBucket(index);
                foreach (FileSpan span in spans)
                {
                    if (excludeTags == null || excludeTags.Contains(span.Tag) == false)
                    {
                        if (span.Contains(fileSpan))
                        {
                            return true;
                        }
                    }
                }

                // We didn't find any spans that contain the given span in this bucket.
                // Check against spans in previous buckets.
                index--;
            }

            return false;
        }

        /// <summary>
        /// Returns the span that contains the given position, or null if there is no such span.
        /// </summary>
        /// <param name="position">Location to find the span for.</param>
        /// <returns>The span with the given location.</returns>
        public FileSpan GetContainingSpan(int position)
        {
            // Start with the index of the given position and work backwards until we find
            // a span that contains this position (or not).
            int index = GetIndex(position);
            for (int i = index; i >= 0; i--)
            {
                SortedSet<FileSpan> spans = GetSpansInStartBucket(i);
                foreach (FileSpan span in spans)
                {
                    if (span.Contains(position))
                    {
                        return span;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the span in the collection that follows the given position.
        /// If the given position is contained within a span, the next span, if any, will be returned.
        /// </summary>
        /// <param name="position">The position to seek.</param>
        /// <returns>The next span or null if there is no subsequent span.</returns>
        public FileSpan GetNextSpan(int position)
        {
            int index = GetIndex(position);

            while (index <= lastStartIndex)
            {
                SortedSet<FileSpan> spans = GetSpansInStartBucket(index);
                foreach (FileSpan bucketSpan in spans)
                {
                    if (bucketSpan.Start > position)
                    {
                        return bucketSpan;
                    }
                }

                // We didn't find the next span in this bucket, so try the next one.
                index++;
            }

            return null;
        }

        /// <summary>
        /// Returns the span in the collection that precedes the given position.
        /// If the given position is contained within a span, the previous span, if any, will be returned.
        /// </summary>
        /// <param name="position">The position to seek.</param>
        /// <returns>The previous span or null if there is no previous span.</returns>
        public FileSpan GetPreviousSpan(int position)
        {
            int index = GetIndex(position);

            while (index >= 0)
            {
                SortedSet<FileSpan> spans = GetSpansInEndBucket(index);
                foreach (FileSpan bucketSpan in spans)
                {
                    if (bucketSpan.End < position)
                    {
                        return bucketSpan;
                    }
                }

                // We didn't find the previous span in this bucket, so try the previous bucket.
                index--;
            }

            return null;
        }
    }
}

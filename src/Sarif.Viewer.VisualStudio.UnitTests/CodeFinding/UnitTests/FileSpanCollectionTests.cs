using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using FluentAssertions;

using Microsoft.Sarif.Viewer.CodeFinding;
using Microsoft.Sarif.Viewer.CodeFinding.Internal;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests.CodeFinding
{
    public class FileSpanCollectionTests
    {

        /// <summary>
        /// Returns a random set of file spans within the given range. The spans will not overlap.
        /// </summary>
        /// <param name="count"></param>
        /// <param name="rangeStart"></param>
        /// <param name="rangeEnd"></param>
        /// <param name="seed"></param>
        /// <returns></returns>
        private List<FileSpan> GenerateSpans(int count, int rangeStart, int rangeEnd, int? seed = null)
        {
            Random rand;
            if (seed != null)
            {
                rand = new Random(seed.Value);
            }
            else
            {
                rand = new Random();
            }

            // General strategy:
            // 1. Divide the range into partitions by dividing the range by the number of spans desired.
            // 2. Pick a random starting position within each partition.
            // 3. For each starting position, pick an end position before the next starting position (ignoring the partitions).

            int rangeSize = rangeEnd - rangeStart;
            int partitionSize = rangeSize / count;
            int rem = rangeSize % count;

            if (partitionSize < 2)
            {
                throw new ArgumentException($"Unable to generate {count} file spans using the range {rangeStart}-{rangeEnd}.");
            }

            // Pick a random starting position within each partition.
            var starts = new List<int>();
            int partitionStart = rangeStart;
            for (int i = 0; i < count; i++)
            {
                // If the range size didn't divide nicely by the requested count,
                // increase the size of each partition by 1 until we've consumed the remainder.
                int size = partitionSize;
                if (rem > 0)
                {
                    size++;
                    rem--;
                }

                // Make sure we don't go outside the given range.
                // Subtract 1 to guarantee there is always room for the end position.
                int partitionEnd = Math.Min(partitionStart + size, rangeEnd) - 1;

                // Note that Next() returns a value in the range [Min, Max).
                int start = rand.Next(partitionStart, partitionEnd);
                starts.Add(start);

                // The next partition starts immediately afer this one.
                partitionStart = partitionEnd + 1;
            }

            // For each starting position, pick a random end position between the current and next starting positions.
            var spans = new List<FileSpan>(count);
            for (int i = 0; i < starts.Count; i++)
            {
                int start = starts[i];

                int nextStart = rangeEnd + 1;
                if (i < starts.Count - 1)
                {
                    nextStart = starts[i + 1];
                }

                // Pick a random end position after this starting position but before the next starting position.
                // Note that Next() returns a value in the range [Min, Max).
                int end = rand.Next(start + 1, nextStart);
                spans.Add(new FileSpan(start, end));
            }

            return spans;
        }

        /// <summary>
        /// Tests basic file collection creation.
        /// </summary>
        [Theory]
        [InlineData(10, 1, 1000, 1)]
        [InlineData(100, 1, 1000, 1)]
        [InlineData(1000, 1, 10000, 1)]
        [InlineData(10000, 1, 100000, 1)]
        [InlineData(100000, 1, 1000000, 1)]
        [InlineData(200000, 1, 1000000, 1)]
        [InlineData(500000, 1, 1100000, 1)]
        [InlineData(10, 500, 1500, 2)]
        [InlineData(100, 20, 6500, 2)]
        [InlineData(1200, 5400, 22000, 2)]
        [InlineData(9999, 1, 100000, 2)]
        [InlineData(100000, 1, 1000000, 2)]
        [InlineData(200000, 1, 1000000, 2)]
        [InlineData(500000, 1, 1100000, 2)]
        public void TestFileCollectionCreation(int count, int rangeStart, int rangeEnd, int seed)
        {
            List<FileSpan> spans = null;
            int minPosition = 0;
            int maxPosition = 0;
            if (count > 0)
            {
                spans = GenerateSpans(count, rangeStart, rangeEnd, seed);

                minPosition = spans.First().Start;
                maxPosition = spans.Last().End;
            }

            var fileCollection = new FileSpanCollection(spans);

            count.Should().Be(fileCollection.Count);
            minPosition.Should().Be(fileCollection.MinPosition);
            maxPosition.Should().Be(fileCollection.MaxPosition);
        }

        /// <summary>
        /// Basic test for Contains().
        /// </summary>
        [Fact]
        public void TestContains1()
        {
            var spans = new List<FileSpan>
            {
                new FileSpan(0, 10),
                new FileSpan(20, 30),
                new FileSpan(50, 60),
                new FileSpan(80, 90)
            };

            var fileCollection = new FileSpanCollection(spans, 10);

            0.Should().Be(fileCollection.MinPosition);
            90.Should().Be(fileCollection.MaxPosition);

            fileCollection.Contains(new FileSpan(0, 10)).Should().BeTrue();
            fileCollection.Contains(new FileSpan(3, 7)).Should().BeTrue();
            fileCollection.Contains(new FileSpan(20, 30)).Should().BeTrue();
            fileCollection.Contains(new FileSpan(21, 22)).Should().BeTrue();
            fileCollection.Contains(new FileSpan(50, 60)).Should().BeTrue();
            fileCollection.Contains(new FileSpan(58, 59)).Should().BeTrue();
            fileCollection.Contains(new FileSpan(80, 90)).Should().BeTrue();
            fileCollection.Contains(new FileSpan(84, 88)).Should().BeTrue();

            fileCollection.Contains(new FileSpan(10, 15)).Should().BeFalse();
            fileCollection.Contains(new FileSpan(5, 15)).Should().BeFalse();
            fileCollection.Contains(new FileSpan(15, 25)).Should().BeFalse();
            fileCollection.Contains(new FileSpan(100, 200)).Should().BeFalse();
            fileCollection.Contains(new FileSpan(1000, 1200)).Should().BeFalse();
            fileCollection.Contains(new FileSpan(10000, 10200)).Should().BeFalse();
        }

        /// <summary>
        /// Tests that Contains() correctly returns true for a span in a bucket different from the span in the collection that contains it.
        /// </summary>
        [Fact]
        public void TestContains2()
        {
            var spans = new List<FileSpan>
            {
                new FileSpan(1, 100), // Starts at index 0, but actually spans several buckets.
                new FileSpan(200, 225),
                new FileSpan(240, 250),
                new FileSpan(260, 300)
            };

            // Force bucket size to 10 so that the spans above span several buckets.
            var fileCollection = new FileSpanCollection(spans, 10);

            1.Should().Be(fileCollection.MinPosition);
            300.Should().Be(fileCollection.MaxPosition);

            // Check that smaller spans in different buckets show up as being contained within the
            // first span in the collection (1, 100).
            fileCollection.Contains(new FileSpan(1, 10)).Should().BeTrue();
            fileCollection.Contains(new FileSpan(3, 7)).Should().BeTrue();
            fileCollection.Contains(new FileSpan(20, 30)).Should().BeTrue();
            fileCollection.Contains(new FileSpan(21, 22)).Should().BeTrue();
            fileCollection.Contains(new FileSpan(50, 60)).Should().BeTrue();
            fileCollection.Contains(new FileSpan(58, 59)).Should().BeTrue();
            fileCollection.Contains(new FileSpan(80, 90)).Should().BeTrue();
            fileCollection.Contains(new FileSpan(84, 88)).Should().BeTrue();
            fileCollection.Contains(new FileSpan(95, 100)).Should().BeTrue();
            fileCollection.Contains(new FileSpan(1, 100)).Should().BeTrue();
            fileCollection.Contains(new FileSpan(1, 50)).Should().BeTrue();
            fileCollection.Contains(new FileSpan(50, 100)).Should().BeTrue();

            fileCollection.Contains(new FileSpan(0, 10)).Should().BeFalse();
            fileCollection.Contains(new FileSpan(50, 150)).Should().BeFalse();
            fileCollection.Contains(new FileSpan(100, 125)).Should().BeFalse();
            fileCollection.Contains(new FileSpan(150, 160)).Should().BeFalse();
            fileCollection.Contains(new FileSpan(1000, 1100)).Should().BeFalse();
            fileCollection.Contains(new FileSpan(10000, 10100)).Should().BeFalse();
        }

        [Fact]
        public void TestGetContainingSpan1()
        {
            var spans = new List<FileSpan>
            {
                new FileSpan(5, 10),
                new FileSpan(20, 30),
                new FileSpan(50, 60),
                new FileSpan(80, 90)
            };

            var fileCollection = new FileSpanCollection(spans, 10);

            FileSpan span = fileCollection.GetContainingSpan(0);
            span.Should().Be(null);

            span = fileCollection.GetContainingSpan(4);
            span.Should().Be(null);

            span = fileCollection.GetContainingSpan(5);
            span.Equals(spans[0]).Should().BeTrue();

            span = fileCollection.GetContainingSpan(8);
            span.Equals(spans[0]).Should().BeTrue();

            span = fileCollection.GetContainingSpan(10);
            span.Equals(spans[0]).Should().BeTrue();

            span = fileCollection.GetContainingSpan(11);
            span.Should().Be(null);

            span = fileCollection.GetContainingSpan(20);
            span.Equals(spans[1]).Should().BeTrue();

            span = fileCollection.GetContainingSpan(25);
            span.Equals(spans[1]).Should().BeTrue();

            span = fileCollection.GetContainingSpan(30);
            span.Equals(spans[1]).Should().BeTrue();

            span = fileCollection.GetContainingSpan(49);
            span.Should().Be(null);

            span = fileCollection.GetContainingSpan(80);
            span.Equals(spans[3]).Should().BeTrue();

            span = fileCollection.GetContainingSpan(83);
            span.Equals(spans[3]).Should().BeTrue();

            span = fileCollection.GetContainingSpan(90);
            span.Equals(spans[3]).Should().BeTrue();

            span = fileCollection.GetContainingSpan(91);
            span.Should().Be(null);

            span = fileCollection.GetContainingSpan(100);
            span.Should().Be(null);

            span = fileCollection.GetContainingSpan(1000);
            span.Should().Be(null);

            span = fileCollection.GetContainingSpan(10000);
            span.Should().Be(null);
        }

        [Fact]
        public void TestGetNextSpan1()
        {
            var spans = new List<FileSpan>
            {
                new FileSpan(5, 10),
                new FileSpan(20, 30),
                new FileSpan(50, 60),
                new FileSpan(80, 90)
            };

            var fileCollection = new FileSpanCollection(spans, 10);

            FileSpan nextSpan = fileCollection.GetNextSpan(0);
            nextSpan.Equals(spans[0]).Should().BeTrue();

            nextSpan = fileCollection.GetNextSpan(1);
            nextSpan.Equals(spans[0]).Should().BeTrue();

            nextSpan = fileCollection.GetNextSpan(5);
            nextSpan.Equals(spans[1]).Should().BeTrue();

            nextSpan = fileCollection.GetNextSpan(7);
            nextSpan.Equals(spans[1]).Should().BeTrue();

            nextSpan = fileCollection.GetNextSpan(11);
            nextSpan.Equals(spans[1]).Should().BeTrue();

            nextSpan = fileCollection.GetNextSpan(20);
            nextSpan.Equals(spans[2]).Should().BeTrue();

            nextSpan = fileCollection.GetNextSpan(25);
            nextSpan.Equals(spans[2]).Should().BeTrue();

            nextSpan = fileCollection.GetNextSpan(49);
            nextSpan.Equals(spans[2]).Should().BeTrue();

            nextSpan = fileCollection.GetNextSpan(59);
            nextSpan.Equals(spans[3]).Should().BeTrue();

            nextSpan = fileCollection.GetNextSpan(60);
            nextSpan.Equals(spans[3]).Should().BeTrue();

            nextSpan = fileCollection.GetNextSpan(65);
            nextSpan.Equals(spans[3]).Should().BeTrue();

            nextSpan = fileCollection.GetNextSpan(85);
            nextSpan.Should().Be(null);

            nextSpan = fileCollection.GetNextSpan(95);
            nextSpan.Should().Be(null);

            nextSpan = fileCollection.GetNextSpan(1000);
            nextSpan.Should().Be(null);
        }

        /// <summary>
        /// Builds a FileCollection from the given spans and then verifies that each span is
        /// accessible via GetNextSpan.
        /// </summary>
        /// <param name="spans"></param>
        private void VerifyGetNextSpan(List<FileSpan> spans)
        {
            var fileCollection = new FileSpanCollection(spans);
            int rangeStart = fileCollection.MinPosition;
            int rangeEnd = fileCollection.MaxPosition;

            int expectedSpanCount = spans.Count;
            expectedSpanCount.Should().Be(fileCollection.Count, $"Expected the collection to contain {expectedSpanCount} spans, but it actually contains {fileCollection.Count}.");

            // Go through the whole range, calling GetNextSpan.
            int encounteredSpanCount = 0;
            FileSpan curSpan = null;
            var stopWatch = new Stopwatch();
            for (int i = rangeStart - 1; i <= rangeEnd + 1; i++)
            {
                stopWatch.Restart();
                FileSpan nextSpan = fileCollection.GetNextSpan(i);
                stopWatch.Stop();

                // Verify GetNextSpan has good performance, 2ms per span in the collection, capping at 1s.
                // This is a sanity check to make sure we don't have a performance regression.
                // If this test fails, it's not a big deal, but it's a good idea to investigate.
                int expectedTime = Math.Min(spans.Count * 2, 1000);
                long actualTime = stopWatch.ElapsedMilliseconds;
                (actualTime < expectedTime).Should().BeTrue($"GetNextSpan should take less than {expectedTime}ms, but actually took {actualTime}ms.");

                if (nextSpan != null)
                {
                    i = nextSpan.Start;
                    encounteredSpanCount++;

                    if (curSpan != null)
                    {
                        // Verify the current span is different from the next one.
                        nextSpan.Equals(curSpan).Should().BeFalse();
                    }
                    curSpan = nextSpan;
                }
            }

            expectedSpanCount.Should().Be(encounteredSpanCount, $"Expected to encounter {expectedSpanCount} spans but actually encountered {encounteredSpanCount} spans.");
        }

        /// <summary>
        /// Tests GetNextSpan with pseudo-randomly generated FileSpan collections.
        /// </summary>
        [Theory]
        [InlineData(10, 1, 1000, 1)]
        [InlineData(100, 1, 1000, 1)]
        [InlineData(1000, 1, 10000, 1)]
        [InlineData(10000, 1, 100000, 1)]
        [InlineData(100000, 1, 1000000, 1)]
        [InlineData(200000, 1, 1000000, 1)]
        [InlineData(500000, 1, 1100000, 1)]
        public void TestGetNextSpan2(int count, int rangeStart, int rangeEnd, int seed)
        {
            List<FileSpan> spans = GenerateSpans(count, rangeStart, rangeEnd, seed);
            VerifyGetNextSpan(spans);
        }

        /// <summary>
        /// Tests GetNextSpan using skewed, pseudo-randomly generated FileSpan collections.
        /// To skew the collection we generate spans for the upper half of the range and then we
        /// insert a single span near the start of the range.
        /// </summary>
        /// <param name="count"></param>
        /// <param name="rangeStart"></param>
        /// <param name="rangeEnd"></param>
        /// <param name="seed"></param>
        [Theory]
        [InlineData(10, 500, 1000, 1)]
        [InlineData(100, 500, 1000, 1)]
        [InlineData(1000, 5000, 10000, 1)]
        [InlineData(10000, 50000, 100000, 1)]
        [InlineData(100000, 500000, 1000000, 1)]
        [InlineData(10000, 900000, 1100000, 1)]
        public void TestGetNextSpan3(int count, int rangeStart, int rangeEnd, int seed)
        {
            List<FileSpan> spans = GenerateSpans(count, rangeStart, rangeEnd, seed);

            // Insert a span with a very low starting position to skew the distribution of buckets in the collection.
            rangeStart = 5;
            spans.Insert(0, new FileSpan(rangeStart, rangeStart + 1));

            VerifyGetNextSpan(spans);
        }

        [Fact]
        public void TestGetPrevSpan1()
        {
            var spans = new List<FileSpan>
            {
                new FileSpan(5, 10),
                new FileSpan(20, 30),
                new FileSpan(50, 60),
                new FileSpan(80, 90)
            };

            var fileCollection = new FileSpanCollection(spans, 10);

            FileSpan prevSpan = fileCollection.GetPreviousSpan(0);
            prevSpan.Should().Be(null);

            prevSpan = fileCollection.GetPreviousSpan(1);
            prevSpan.Should().Be(null);

            prevSpan = fileCollection.GetPreviousSpan(5);
            prevSpan.Should().Be(null);

            prevSpan = fileCollection.GetPreviousSpan(10);
            prevSpan.Should().Be(null);

            prevSpan = fileCollection.GetPreviousSpan(11);
            prevSpan.Equals(spans[0]).Should().BeTrue();

            prevSpan = fileCollection.GetPreviousSpan(19);
            prevSpan.Equals(spans[0]).Should().BeTrue();

            prevSpan = fileCollection.GetPreviousSpan(20);
            prevSpan.Equals(spans[0]).Should().BeTrue();

            prevSpan = fileCollection.GetPreviousSpan(25);
            prevSpan.Equals(spans[0]).Should().BeTrue();

            prevSpan = fileCollection.GetPreviousSpan(30);
            prevSpan.Equals(spans[0]).Should().BeTrue();

            prevSpan = fileCollection.GetPreviousSpan(31);
            prevSpan.Equals(spans[1]).Should().BeTrue();

            prevSpan = fileCollection.GetPreviousSpan(45);
            prevSpan.Equals(spans[1]).Should().BeTrue();

            prevSpan = fileCollection.GetPreviousSpan(50);
            prevSpan.Equals(spans[1]).Should().BeTrue();

            prevSpan = fileCollection.GetPreviousSpan(55);
            prevSpan.Equals(spans[1]).Should().BeTrue();

            prevSpan = fileCollection.GetPreviousSpan(60);
            prevSpan.Equals(spans[1]).Should().BeTrue();

            prevSpan = fileCollection.GetPreviousSpan(65);
            prevSpan.Equals(spans[2]).Should().BeTrue();

            prevSpan = fileCollection.GetPreviousSpan(70);
            prevSpan.Equals(spans[2]).Should().BeTrue();

            prevSpan = fileCollection.GetPreviousSpan(75);
            prevSpan.Equals(spans[2]).Should().BeTrue();

            prevSpan = fileCollection.GetPreviousSpan(80);
            prevSpan.Equals(spans[2]).Should().BeTrue();

            prevSpan = fileCollection.GetPreviousSpan(85);
            prevSpan.Equals(spans[2]).Should().BeTrue();

            prevSpan = fileCollection.GetPreviousSpan(90);
            prevSpan.Equals(spans[2]).Should().BeTrue();

            prevSpan = fileCollection.GetPreviousSpan(91);
            prevSpan.Equals(spans[3]).Should().BeTrue();

            prevSpan = fileCollection.GetPreviousSpan(100);
            prevSpan.Equals(spans[3]).Should().BeTrue();

            prevSpan = fileCollection.GetPreviousSpan(500);
            prevSpan.Equals(spans[3]).Should().BeTrue();

            prevSpan = fileCollection.GetPreviousSpan(1000);
            prevSpan.Equals(spans[3]).Should().BeTrue();

            prevSpan = fileCollection.GetPreviousSpan(10000);
            prevSpan.Equals(spans[3]).Should().BeTrue();

            prevSpan = fileCollection.GetPreviousSpan(100000);
            prevSpan.Equals(spans[3]).Should().BeTrue();

            prevSpan = fileCollection.GetPreviousSpan(1000000);
            prevSpan.Equals(spans[3]).Should().BeTrue();
        }

        /// <summary>
        /// Builds a FileCollection from the given spans and then verifies that each span is
        /// accessible via GetPreviousSpan.
        /// </summary>
        /// <param name="spans"></param>
        private void VerifyGetPrevSpan(List<FileSpan> spans)
        {
            var fileCollection = new FileSpanCollection(spans);
            int rangeStart = fileCollection.MinPosition;
            int rangeEnd = fileCollection.MaxPosition;

            int expectedSpanCount = spans.Count;
            expectedSpanCount.Should().Be(fileCollection.Count, $"Expected the collection to contain {expectedSpanCount} spans, but it actually contains {fileCollection.Count}.");

            // Go through the whole range, calling GetPreviousSpan.
            int encounteredSpanCount = 0;
            FileSpan curSpan = null;
            var stopWatch = new Stopwatch();
            for (int i = rangeEnd + 1; i >= rangeStart - 1; i--)
            {
                stopWatch.Restart();
                FileSpan prevSpan = fileCollection.GetPreviousSpan(i);
                stopWatch.Stop();

                // Verify GetPreviousSpan has good performance, 1ms per span in the collection, capping at 1s.
                int expectedTime = Math.Min(spans.Count, 1000);
                long actualTime = stopWatch.ElapsedMilliseconds;
                (actualTime < expectedTime).Should().BeTrue($"GetPreviousSpan should take less than {expectedTime}ms, but actually took {actualTime}ms.");

                if (prevSpan != null)
                {
                    i = prevSpan.End;
                    encounteredSpanCount++;

                    if (curSpan != null)
                    {
                        // Verify the current span is different from the next one.
                        prevSpan.Equals(curSpan).Should().BeFalse();
                    }
                    curSpan = prevSpan;
                }
            }

            expectedSpanCount.Should().Be(encounteredSpanCount, $"Expected to encounter {expectedSpanCount} spans but actually encountered {encounteredSpanCount} spans.");
        }

        /// <summary>
        /// Tests GetPreviousSpan with pseudo-randomly generated FileSpan collections.
        /// </summary>
        [Theory]
        [InlineData(10, 1, 1000, 1)]
        [InlineData(100, 1, 1000, 1)]
        [InlineData(1000, 1, 10000, 1)]
        [InlineData(10000, 1, 100000, 1)]
        [InlineData(100000, 1, 1000000, 1)]
        [InlineData(200000, 1, 1000000, 1)]
        [InlineData(500000, 1, 1100000, 1)]
        public void TestGetPrevSpan2(int count, int rangeStart, int rangeEnd, int seed)
        {
            List<FileSpan> spans = GenerateSpans(count, rangeStart, rangeEnd, seed);
            VerifyGetPrevSpan(spans);
        }

        /// <summary>
        /// Tests GetPreviousSpan using skewed, pseudo-randomly generated FileSpan collections.
        /// To skew the collection we generate spans for the upper half of the range and then we
        /// insert a single span near the start of the range.
        /// </summary>
        [Theory]
        [InlineData(10, 500, 1000, 1)]
        [InlineData(100, 500, 1000, 1)]
        [InlineData(1000, 5000, 10000, 1)]
        [InlineData(10000, 50000, 100000, 1)]
        [InlineData(100000, 500000, 1100000, 1)]
        [InlineData(10000, 900000, 1100000, 1)]
        public void TestGetPrevSpan3(int count, int rangeStart, int rangeEnd, int seed)
        {
            List<FileSpan> spans = GenerateSpans(count, rangeStart, rangeEnd, seed);

            // Insert a span with a very low starting position to skew the distribution of buckets in the collection.
            rangeStart = 5;
            spans.Insert(0, new FileSpan(rangeStart, rangeStart + 1));

            VerifyGetPrevSpan(spans);
        }
    }
}

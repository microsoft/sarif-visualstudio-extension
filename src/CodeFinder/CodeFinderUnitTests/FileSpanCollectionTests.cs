using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CodeFinder;
using CodeFinder.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeFinderUnitTests
{
    [TestClass]
    public class FileSpanCollectionTests
    {

        /// <summary>
        /// Returns a random set of file spans within the given range. The spans will not overlap.
        /// </summary>
        /// <param name="count"></param>
        /// <param name="rangeStart"></param>
        /// <param name="rangeEnd"></param>
        /// <param name="allowOverlap"></param>
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

            var rangeSize = rangeEnd - rangeStart;
            var partitionSize = rangeSize / count;
            var rem = rangeSize % count;

            if (partitionSize < 2)
            {
                throw new ArgumentException($"Unable to generate {count} file spans using the range {rangeStart}-{rangeEnd}.");
            }

            // Pick a random starting position within each partition.
            var starts = new List<int>();
            var partitionStart = rangeStart;
            for (int i = 0; i < count; i++)
            {
                // If the range size didn't divide nicely by the requested count,
                // increase the size of each partition by 1 until we've consumed the remainder.
                var size = partitionSize;
                if (rem > 0)
                {
                    size++;
                    rem--;
                }

                // Make sure we don't go outside the given range.
                // Subtract 1 to guarantee there is always room for the end position.
                var partitionEnd = Math.Min(partitionStart + size, rangeEnd) - 1;

                // Note that Next() returns a value in the range [Min, Max).
                var start = rand.Next(partitionStart, partitionEnd);
                starts.Add(start);

                // The next partition starts immediately afer this one.
                partitionStart = partitionEnd + 1;
            }

            // For each starting position, pick a random end position between the current and next starting positions.
            var spans = new List<FileSpan>(count);
            for (int i = 0; i < starts.Count; i++)
            {
                var start = starts[i];

                var nextStart = rangeEnd + 1;
                if (i < starts.Count - 1)
                {
                    nextStart = starts[i + 1];
                }

                // Pick a random end position after this starting position but before the next starting position.
                // Note that Next() returns a value in the range [Min, Max).
                var end = rand.Next(start + 1, nextStart);
                spans.Add(new FileSpan(start, end));
            }

            return spans;
        }

        /// <summary>
        /// Tests basic file collection creation.
        /// </summary>
        [TestMethod]
        [DataRow(10, 1, 1000, 1)]
        [DataRow(100, 1, 1000, 1)]
        [DataRow(1000, 1, 10000, 1)]
        [DataRow(10000, 1, 100000, 1)]
        [DataRow(100000, 1, 1000000, 1)]
        [DataRow(200000, 1, 1000000, 1)]
        [DataRow(500000, 1, 1100000, 1)]
        [DataRow(10, 500, 1500, 2)]
        [DataRow(100, 20, 6500, 2)]
        [DataRow(1200, 5400, 22000, 2)]
        [DataRow(9999, 1, 100000, 2)]
        [DataRow(100000, 1, 1000000, 2)]
        [DataRow(200000, 1, 1000000, 2)]
        [DataRow(500000, 1, 1100000, 2)]
        public void TestFileCollectionCreation(int count, int rangeStart, int rangeEnd, int seed)
        {
            List<FileSpan> spans = null;
            var minPosition = 0;
            var maxPosition = 0;
            if (count > 0)
            {
                spans = GenerateSpans(count, rangeStart, rangeEnd, seed);

                minPosition = spans.First().Start;
                maxPosition = spans.Last().End;
            }

            var fileCollection = new FileSpanCollection(spans);

            Assert.AreEqual(count, fileCollection.Count);
            Assert.AreEqual(minPosition, fileCollection.MinPosition);
            Assert.AreEqual(maxPosition, fileCollection.MaxPosition);
        }

        /// <summary>
        /// Basic test for Contains().
        /// </summary>
        [TestMethod]
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

            Assert.AreEqual(0, fileCollection.MinPosition);
            Assert.AreEqual(90, fileCollection.MaxPosition);

            Assert.IsTrue(fileCollection.Contains(new FileSpan(0, 10)));
            Assert.IsTrue(fileCollection.Contains(new FileSpan(3, 7)));
            Assert.IsTrue(fileCollection.Contains(new FileSpan(20, 30)));
            Assert.IsTrue(fileCollection.Contains(new FileSpan(21, 22)));
            Assert.IsTrue(fileCollection.Contains(new FileSpan(50, 60)));
            Assert.IsTrue(fileCollection.Contains(new FileSpan(58, 59)));
            Assert.IsTrue(fileCollection.Contains(new FileSpan(80, 90)));
            Assert.IsTrue(fileCollection.Contains(new FileSpan(84, 88)));

            Assert.IsFalse(fileCollection.Contains(new FileSpan(10, 15)));
            Assert.IsFalse(fileCollection.Contains(new FileSpan(5, 15)));
            Assert.IsFalse(fileCollection.Contains(new FileSpan(15, 25)));
            Assert.IsFalse(fileCollection.Contains(new FileSpan(100, 200)));
            Assert.IsFalse(fileCollection.Contains(new FileSpan(1000, 1200)));
            Assert.IsFalse(fileCollection.Contains(new FileSpan(10000, 10200)));
        }

        /// <summary>
        /// Tests that Contains() correctly returns true for a span in a bucket different from the span in the collection that contains it.
        /// </summary>
        [TestMethod]
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

            Assert.AreEqual(1, fileCollection.MinPosition);
            Assert.AreEqual(300, fileCollection.MaxPosition);

            // Check that smaller spans in different buckets show up as being contained within the
            // first span in the collection (1, 100).
            Assert.IsTrue(fileCollection.Contains(new FileSpan(1, 10)));
            Assert.IsTrue(fileCollection.Contains(new FileSpan(3, 7)));
            Assert.IsTrue(fileCollection.Contains(new FileSpan(20, 30)));
            Assert.IsTrue(fileCollection.Contains(new FileSpan(21, 22)));
            Assert.IsTrue(fileCollection.Contains(new FileSpan(50, 60)));
            Assert.IsTrue(fileCollection.Contains(new FileSpan(58, 59)));
            Assert.IsTrue(fileCollection.Contains(new FileSpan(80, 90)));
            Assert.IsTrue(fileCollection.Contains(new FileSpan(84, 88)));
            Assert.IsTrue(fileCollection.Contains(new FileSpan(95, 100)));
            Assert.IsTrue(fileCollection.Contains(new FileSpan(1, 100)));
            Assert.IsTrue(fileCollection.Contains(new FileSpan(1, 50)));
            Assert.IsTrue(fileCollection.Contains(new FileSpan(50, 100)));

            Assert.IsFalse(fileCollection.Contains(new FileSpan(0, 10)));
            Assert.IsFalse(fileCollection.Contains(new FileSpan(50, 150)));
            Assert.IsFalse(fileCollection.Contains(new FileSpan(100, 125)));
            Assert.IsFalse(fileCollection.Contains(new FileSpan(150, 160)));
            Assert.IsFalse(fileCollection.Contains(new FileSpan(1000, 1100)));
            Assert.IsFalse(fileCollection.Contains(new FileSpan(10000, 10100)));
        }

        [TestMethod]
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

            var span = fileCollection.GetContainingSpan(0);
            Assert.AreEqual(null, span);

            span = fileCollection.GetContainingSpan(4);
            Assert.AreEqual(null, span);

            span = fileCollection.GetContainingSpan(5);
            Assert.IsTrue(span.Equals(spans[0]));

            span = fileCollection.GetContainingSpan(8);
            Assert.IsTrue(span.Equals(spans[0]));

            span = fileCollection.GetContainingSpan(10);
            Assert.IsTrue(span.Equals(spans[0]));

            span = fileCollection.GetContainingSpan(11);
            Assert.AreEqual(null, span);

            span = fileCollection.GetContainingSpan(20);
            Assert.IsTrue(span.Equals(spans[1]));

            span = fileCollection.GetContainingSpan(25);
            Assert.IsTrue(span.Equals(spans[1]));

            span = fileCollection.GetContainingSpan(30);
            Assert.IsTrue(span.Equals(spans[1]));

            span = fileCollection.GetContainingSpan(49);
            Assert.AreEqual(null, span);

            span = fileCollection.GetContainingSpan(80);
            Assert.IsTrue(span.Equals(spans[3]));

            span = fileCollection.GetContainingSpan(83);
            Assert.IsTrue(span.Equals(spans[3]));

            span = fileCollection.GetContainingSpan(90);
            Assert.IsTrue(span.Equals(spans[3]));

            span = fileCollection.GetContainingSpan(91);
            Assert.AreEqual(null, span);

            span = fileCollection.GetContainingSpan(100);
            Assert.AreEqual(null, span);

            span = fileCollection.GetContainingSpan(1000);
            Assert.AreEqual(null, span);

            span = fileCollection.GetContainingSpan(10000);
            Assert.AreEqual(null, span);
        }

        [TestMethod]
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

            var nextSpan = fileCollection.GetNextSpan(0);
            Assert.IsTrue(nextSpan.Equals(spans[0]));

            nextSpan = fileCollection.GetNextSpan(1);
            Assert.IsTrue(nextSpan.Equals(spans[0]));

            nextSpan = fileCollection.GetNextSpan(5);
            Assert.IsTrue(nextSpan.Equals(spans[1]));

            nextSpan = fileCollection.GetNextSpan(7);
            Assert.IsTrue(nextSpan.Equals(spans[1]));

            nextSpan = fileCollection.GetNextSpan(11);
            Assert.IsTrue(nextSpan.Equals(spans[1]));

            nextSpan = fileCollection.GetNextSpan(20);
            Assert.IsTrue(nextSpan.Equals(spans[2]));

            nextSpan = fileCollection.GetNextSpan(25);
            Assert.IsTrue(nextSpan.Equals(spans[2]));

            nextSpan = fileCollection.GetNextSpan(49);
            Assert.IsTrue(nextSpan.Equals(spans[2]));

            nextSpan = fileCollection.GetNextSpan(59);
            Assert.IsTrue(nextSpan.Equals(spans[3]));

            nextSpan = fileCollection.GetNextSpan(60);
            Assert.IsTrue(nextSpan.Equals(spans[3]));

            nextSpan = fileCollection.GetNextSpan(65);
            Assert.IsTrue(nextSpan.Equals(spans[3]));

            nextSpan = fileCollection.GetNextSpan(85);
            Assert.AreEqual(null, nextSpan);

            nextSpan = fileCollection.GetNextSpan(95);
            Assert.AreEqual(null, nextSpan);

            nextSpan = fileCollection.GetNextSpan(1000);
            Assert.AreEqual(null, nextSpan);
        }

        /// <summary>
        /// Builds a FileCollection from the given spans and then verifies that each span is
        /// accessible via GetNextSpan.
        /// </summary>
        /// <param name="spans"></param>
        private void VerifyGetNextSpan(List<FileSpan> spans)
        {
            var fileCollection = new FileSpanCollection(spans);
            var rangeStart = fileCollection.MinPosition;
            var rangeEnd = fileCollection.MaxPosition;

            var expectedSpanCount = spans.Count;
            Assert.AreEqual(expectedSpanCount, fileCollection.Count, $"Expected the collection to contain {expectedSpanCount} spans, but it actually contains {fileCollection.Count}.");

            // Go through the whole range, calling GetNextSpan.
            var encounteredSpanCount = 0;
            FileSpan curSpan = null;
            var stopWatch = new Stopwatch();
            for (int i = rangeStart - 1; i <= rangeEnd + 1; i++)
            {
                stopWatch.Restart();
                var nextSpan = fileCollection.GetNextSpan(i);
                stopWatch.Stop();

                // Verify GetNextSpan has good performance, 1ms per span in the collection, capping at 1s.
                var expectedTime = Math.Min(spans.Count, 1000);
                var actualTime = stopWatch.ElapsedMilliseconds;
                Assert.IsTrue(actualTime < expectedTime, $"GetNextSpan should take less than {expectedTime}ms, but actually took {actualTime}ms.");

                if (nextSpan != null)
                {
                    i = nextSpan.Start;
                    encounteredSpanCount++;

                    if (curSpan != null)
                    {
                        // Verify the current span is different from the next one.
                        Assert.IsFalse(nextSpan.Equals(curSpan));
                    }
                    curSpan = nextSpan;
                }
            }

            Assert.AreEqual(expectedSpanCount, encounteredSpanCount, $"Expected to encounter {expectedSpanCount} spans but actually encountered {encounteredSpanCount} spans.");
        }

        /// <summary>
        /// Tests GetNextSpan with pseudo-randomly generated FileSpan collections.
        /// </summary>
        [TestMethod]
        [DataRow(10, 1, 1000, 1)]
        [DataRow(100, 1, 1000, 1)]
        [DataRow(1000, 1, 10000, 1)]
        [DataRow(10000, 1, 100000, 1)]
        [DataRow(100000, 1, 1000000, 1)]
        [DataRow(200000, 1, 1000000, 1)]
        [DataRow(500000, 1, 1100000, 1)]
        public void TestGetNextSpan2(int count, int rangeStart, int rangeEnd, int seed)
        {
            var spans = GenerateSpans(count, rangeStart, rangeEnd, seed);
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
        [TestMethod]
        [DataRow(10, 500, 1000, 1)]
        [DataRow(100, 500, 1000, 1)]
        [DataRow(1000, 5000, 10000, 1)]
        [DataRow(10000, 50000, 100000, 1)]
        [DataRow(100000, 500000, 1000000, 1)]
        [DataRow(10000, 900000, 1100000, 1)]
        public void TestGetNextSpan3(int count, int rangeStart, int rangeEnd, int seed)
        {
            var spans = GenerateSpans(count, rangeStart, rangeEnd, seed);

            // Insert a span with a very low starting position to skew the distribution of buckets in the collection.
            rangeStart = 5;
            spans.Insert(0, new FileSpan(rangeStart, rangeStart + 1));

            VerifyGetNextSpan(spans);
        }

        [TestMethod]
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

            var prevSpan = fileCollection.GetPreviousSpan(0);
            Assert.AreEqual(null, prevSpan);

            prevSpan = fileCollection.GetPreviousSpan(1);
            Assert.AreEqual(null, prevSpan);

            prevSpan = fileCollection.GetPreviousSpan(5);
            Assert.AreEqual(null, prevSpan);

            prevSpan = fileCollection.GetPreviousSpan(10);
            Assert.AreEqual(null, prevSpan);

            prevSpan = fileCollection.GetPreviousSpan(11);
            Assert.IsTrue(prevSpan.Equals(spans[0]));

            prevSpan = fileCollection.GetPreviousSpan(19);
            Assert.IsTrue(prevSpan.Equals(spans[0]));

            prevSpan = fileCollection.GetPreviousSpan(20);
            Assert.IsTrue(prevSpan.Equals(spans[0]));

            prevSpan = fileCollection.GetPreviousSpan(25);
            Assert.IsTrue(prevSpan.Equals(spans[0]));

            prevSpan = fileCollection.GetPreviousSpan(30);
            Assert.IsTrue(prevSpan.Equals(spans[0]));

            prevSpan = fileCollection.GetPreviousSpan(31);
            Assert.IsTrue(prevSpan.Equals(spans[1]));

            prevSpan = fileCollection.GetPreviousSpan(45);
            Assert.IsTrue(prevSpan.Equals(spans[1]));

            prevSpan = fileCollection.GetPreviousSpan(50);
            Assert.IsTrue(prevSpan.Equals(spans[1]));

            prevSpan = fileCollection.GetPreviousSpan(55);
            Assert.IsTrue(prevSpan.Equals(spans[1]));

            prevSpan = fileCollection.GetPreviousSpan(60);
            Assert.IsTrue(prevSpan.Equals(spans[1]));

            prevSpan = fileCollection.GetPreviousSpan(65);
            Assert.IsTrue(prevSpan.Equals(spans[2]));

            prevSpan = fileCollection.GetPreviousSpan(70);
            Assert.IsTrue(prevSpan.Equals(spans[2]));

            prevSpan = fileCollection.GetPreviousSpan(75);
            Assert.IsTrue(prevSpan.Equals(spans[2]));

            prevSpan = fileCollection.GetPreviousSpan(80);
            Assert.IsTrue(prevSpan.Equals(spans[2]));

            prevSpan = fileCollection.GetPreviousSpan(85);
            Assert.IsTrue(prevSpan.Equals(spans[2]));

            prevSpan = fileCollection.GetPreviousSpan(90);
            Assert.IsTrue(prevSpan.Equals(spans[2]));

            prevSpan = fileCollection.GetPreviousSpan(91);
            Assert.IsTrue(prevSpan.Equals(spans[3]));

            prevSpan = fileCollection.GetPreviousSpan(100);
            Assert.IsTrue(prevSpan.Equals(spans[3]));

            prevSpan = fileCollection.GetPreviousSpan(500);
            Assert.IsTrue(prevSpan.Equals(spans[3]));

            prevSpan = fileCollection.GetPreviousSpan(1000);
            Assert.IsTrue(prevSpan.Equals(spans[3]));

            prevSpan = fileCollection.GetPreviousSpan(10000);
            Assert.IsTrue(prevSpan.Equals(spans[3]));

            prevSpan = fileCollection.GetPreviousSpan(100000);
            Assert.IsTrue(prevSpan.Equals(spans[3]));

            prevSpan = fileCollection.GetPreviousSpan(1000000);
            Assert.IsTrue(prevSpan.Equals(spans[3]));
        }

        /// <summary>
        /// Builds a FileCollection from the given spans and then verifies that each span is
        /// accessible via GetPreviousSpan.
        /// </summary>
        /// <param name="spans"></param>
        private void VerifyGetPrevSpan(List<FileSpan> spans)
        {
            var fileCollection = new FileSpanCollection(spans);
            var rangeStart = fileCollection.MinPosition;
            var rangeEnd = fileCollection.MaxPosition;

            var expectedSpanCount = spans.Count;
            Assert.AreEqual(expectedSpanCount, fileCollection.Count, $"Expected the collection to contain {expectedSpanCount} spans, but it actually contains {fileCollection.Count}.");

            // Go through the whole range, calling GetPreviousSpan.
            var encounteredSpanCount = 0;
            FileSpan curSpan = null;
            var stopWatch = new Stopwatch();
            for (int i = rangeEnd + 1; i >= rangeStart - 1; i--)
            {
                stopWatch.Restart();
                var prevSpan = fileCollection.GetPreviousSpan(i);
                stopWatch.Stop();

                // Verify GetPreviousSpan has good performance, 1ms per span in the collection, capping at 1s.
                var expectedTime = Math.Min(spans.Count, 1000);
                var actualTime = stopWatch.ElapsedMilliseconds;
                Assert.IsTrue(actualTime < expectedTime, $"GetPreviousSpan should take less than {expectedTime}ms, but actually took {actualTime}ms.");

                if (prevSpan != null)
                {
                    i = prevSpan.End;
                    encounteredSpanCount++;

                    if (curSpan != null)
                    {
                        // Verify the current span is different from the next one.
                        Assert.IsFalse(prevSpan.Equals(curSpan));
                    }
                    curSpan = prevSpan;
                }
            }

            Assert.AreEqual(expectedSpanCount, encounteredSpanCount, $"Expected to encounter {expectedSpanCount} spans but actually encountered {encounteredSpanCount} spans.");
        }

        /// <summary>
        /// Tests GetPreviousSpan with pseudo-randomly generated FileSpan collections.
        /// </summary>
        [TestMethod]
        [DataRow(10, 1, 1000, 1)]
        [DataRow(100, 1, 1000, 1)]
        [DataRow(1000, 1, 10000, 1)]
        [DataRow(10000, 1, 100000, 1)]
        [DataRow(100000, 1, 1000000, 1)]
        [DataRow(200000, 1, 1000000, 1)]
        [DataRow(500000, 1, 1100000, 1)]
        public void TestGetPrevSpan2(int count, int rangeStart, int rangeEnd, int seed)
        {
            var spans = GenerateSpans(count, rangeStart, rangeEnd, seed);
            VerifyGetPrevSpan(spans);
        }

        /// <summary>
        /// Tests GetPreviousSpan using skewed, pseudo-randomly generated FileSpan collections.
        /// To skew the collection we generate spans for the upper half of the range and then we
        /// insert a single span near the start of the range.
        /// </summary>
        [TestMethod]
        [DataRow(10, 500, 1000, 1)]
        [DataRow(100, 500, 1000, 1)]
        [DataRow(1000, 5000, 10000, 1)]
        [DataRow(10000, 50000, 100000, 1)]
        [DataRow(100000, 500000, 1100000, 1)]
        [DataRow(10000, 900000, 1100000, 1)]
        public void TestGetPrevSpan3(int count, int rangeStart, int rangeEnd, int seed)
        {
            var spans = GenerateSpans(count, rangeStart, rangeEnd, seed);

            // Insert a span with a very low starting position to skew the distribution of buckets in the collection.
            rangeStart = 5;
            spans.Insert(0, new FileSpan(rangeStart, rangeStart + 1));

            VerifyGetPrevSpan(spans);
        }
    }
}

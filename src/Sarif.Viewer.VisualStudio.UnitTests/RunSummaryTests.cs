// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class RunSummaryTests
    {
        private readonly Random random;

        public RunSummaryTests(ITestOutputHelper testOutput)
        {
            this.random = this.GetRandom(testOutput);
        }

        [Fact]
        public void Count_Tests()
        {
            int ErrorResultCount = this.random.Next(0, 30);
            int WarningResultCount = this.random.Next(0, 20);
            int NoteResultCount = this.random.Next(0, 10);
            int NoneResultCount = this.random.Next(0, 5);

            List<SarifErrorListItem> items = new List<SarifErrorListItem>();
            items.AddRange(this.GenerateTestSarifErrorListItems(ErrorResultCount, FailureLevel.Error));
            items.AddRange(this.GenerateTestSarifErrorListItems(WarningResultCount, FailureLevel.Warning));
            items.AddRange(this.GenerateTestSarifErrorListItems(NoteResultCount, FailureLevel.Note));
            items.AddRange(this.GenerateTestSarifErrorListItems(NoneResultCount, FailureLevel.None));

            var runCache = new RunDataCache();
            foreach (SarifErrorListItem sarifItem in items)
            {
                runCache.AddSarifResult(sarifItem);
            }

            runCache.RunSummary.TotalResults.Should().Be(ErrorResultCount + WarningResultCount + NoteResultCount + NoneResultCount);
            runCache.RunSummary.ErrorResultsCount.Should().Be(ErrorResultCount);
            runCache.RunSummary.WarningResultsCount.Should().Be(WarningResultCount);
            runCache.RunSummary.MessageResultsCount.Should().Be(NoteResultCount + NoneResultCount);
        }

        private IEnumerable<SarifErrorListItem> GenerateTestSarifErrorListItems(int count, FailureLevel level)
        {
            foreach (int n in Enumerable.Range(0, count))
            {
                yield return new SarifErrorListItem { Level = level };
            }
        }

        private Random GetRandom(ITestOutputHelper output, [CallerMemberName] string testName = "", int? seed = null)
        {
            int randomSeed = seed ?? (new Random()).Next();

            output.WriteLine($"TestName: {testName} has seed {randomSeed}");

            return new Random(randomSeed);
        }
    }
}

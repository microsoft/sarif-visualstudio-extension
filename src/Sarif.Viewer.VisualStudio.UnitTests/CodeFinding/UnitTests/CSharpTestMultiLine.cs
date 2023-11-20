// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Sarif.Viewer.CodeFinding;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests.CodeFinding.UnitTests
{
    public class CSharpTestMultiLine
    {
        /// <summary>
        /// Tests if we are able to match multi line snippets
        /// </summary>
        [Theory]
        [InlineData("\r")]
        [InlineData("\r\n")]
        [InlineData("\n")]
        public void TestMultiLine(string lineEndings)
        { 
            string filePath = @"CodeFinding\TestFiles\CSharp2.cs";
            CodeFinder codeFinder = new CodeFinder(filePath);

            string textToFind = $"            Console.WriteLine(\"return a + b + c\");{lineEndings}            return a + b + c;";

            MatchQuery query = new MatchQuery(textToFind);
            List<MatchResult> matches = codeFinder.FindMatches(query);
            matches.Count().Should().Be(1);
            var bestMatch = MatchResult.GetBestMatch(matches);
            bestMatch.LineNumber.Should().Be(38);
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using Microsoft.Sarif.Viewer.CodeFinding;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests.CodeFinding
{
    /// <summary>
    /// Encapsulates code common to interacting with the CodeFinder library.
    /// All unit test classes should inherit from this so that they don't have to re-implement it.
    /// </summary>
    public class CodeFinderUnitTestBase
    {
        protected CodeFinder Finder;

        /// <summary>
        /// Initializes this object by loading the given file.
        /// Derived classes should call this from their constructor(s) and provide the appropriate file path to the code file to use.
        /// </summary>
        /// <param name="filePath">The path to the code file against which matches will be found and validated.</param>
        public CodeFinderUnitTestBase(string filePath)
        {
            Finder = new CodeFinder(filePath);
        }

        /// <summary>
        /// Returns matches within the file from the given parameters.
        /// </summary>
        /// <param name="textToFind"></param>
        /// <param name="lineNumberHint"></param>
        /// <param name="functionSignature"></param>
        /// <param name="typeHint"></param>
        /// <returns></returns>
        public List<MatchResult> GetMatches(string textToFind, int lineNumberHint = 0, string functionSignature = "", MatchQuery.MatchTypeHint typeHint = MatchQuery.MatchTypeHint.Code)
        {
            Console.WriteLine($"Finding matches for \"{textToFind}\" with function signature \"{functionSignature}\" near line {lineNumberHint}...");

            var query = new MatchQuery(textToFind, lineNumberHint, functionSignature, "0", typeHint);
            List<MatchResult> results = Finder.FindMatchesWithFunction(query);

            Console.WriteLine($"Found {results.Count} match(es).");
            foreach (MatchResult result in results)
            {
                Console.WriteLine($"Line: {result.LineNumber}, ScopeMatchDiff: {result.ScopeMatchDiff}");
            }

            return results;
        }

        /// <summary>
        /// Validates that the given actual results represent a single match with the given expected values.
        /// </summary>
        /// <param name="actualResults"></param>
        /// <param name="expectedLineNumber"></param>
        /// <param name="expectedDistanceFromLineHint"></param>
        /// <param name="expectedScopeChecked"></param>
        /// <param name="expectedScopeMatchDiff"></param>
        public static void ValidateMatch(List<MatchResult> actualResults, int expectedLineNumber = 0, int expectedDistanceFromLineHint = 0, bool expectedScopeChecked = true, int? expectedScopeMatchDiff = 0)
        {
            1.Should().Be(actualResults.Count, $"Expected 1 match, but found {actualResults.Count} match(es).");
            if (actualResults.Count > 0)
            {
                MatchResult match = actualResults[0];
                ValidateMatch(match, expectedLineNumber, expectedDistanceFromLineHint, expectedScopeChecked, expectedScopeMatchDiff);
            }
        }

        /// <summary>
        /// Validates that the given actual results represent 0 matches.
        /// </summary>
        /// <param name="actualResults"></param>
        public static void ValidateNoMatches(List<MatchResult> actualResults)
        {
            0.Should().Be(actualResults.Count, $"Expected 0 matches, but found {actualResults.Count} match(es).");
        }

        /// <summary>
        /// Validates that the given actual results match the given expected results.
        /// </summary>
        /// <param name="actualResults"></param>
        /// <param name="expectedResults"></param>
        public static void ValidateMatches(List<MatchResult> actualResults, List<MatchResult> expectedResults)
        {
            expectedResults.Count.Should().Be(actualResults.Count, $"Expected {expectedResults.Count} matches, but found {actualResults.Count} match(es).");

            foreach (MatchResult expectedResult in expectedResults)
            {
                var actualResultsForThisLine = actualResults.Where(m => m.LineNumber == expectedResult.LineNumber).ToList();

                foreach (MatchResult actualResult in actualResultsForThisLine)
                {
                    actualResults.Remove(actualResult);
                }

                1.Should().Be(actualResultsForThisLine.Count, $"Expected 1 match for line {expectedResult.LineNumber} but found {actualResultsForThisLine.Count} match(es).");
                if (actualResultsForThisLine.Count > 0)
                {
                    ValidateMatch(actualResultsForThisLine[0], expectedResult);
                }
            }

            0.Should().Be(actualResults.Count);
        }

        /// <summary>
        /// Validates the given actual result is the same as the given expected result.
        /// </summary>
        /// <param name="actualResult"></param>
        /// <param name="expectedResult"></param>
        public static void ValidateMatch(MatchResult actualResult, MatchResult expectedResult)
        {
            ValidateMatch(actualResult, expectedResult.LineNumber, expectedResult.DistanceFromLineHint, expectedResult.ScopeChecked, expectedResult.ScopeMatchDiff);
        }

        /// <summary>
        /// Actual implementation of logic that validates the given actual match with the given expected values.
        /// </summary>
        /// <param name="actualResult"></param>
        /// <param name="expectedLineNumber"></param>
        /// <param name="expectedDistanceFromLineHint"></param>
        /// <param name="expectedScopeChecked"></param>
        /// <param name="expectedScopeMatchDiff"></param>
        private static void ValidateMatch(MatchResult actualResult, int expectedLineNumber, int expectedDistanceFromLineHint, bool expectedScopeChecked, int? expectedScopeMatchDiff)
        {
            expectedLineNumber.Should().Be(actualResult.LineNumber, $"Expected match on line {expectedLineNumber}, but match was found on line {actualResult.LineNumber}");
            expectedDistanceFromLineHint.Should().Be(actualResult.DistanceFromLineHint, $"Expected match to be {expectedDistanceFromLineHint} line(s) away from hint, but match was actually {actualResult.DistanceFromLineHint} line(s) away.");
            expectedScopeChecked.Should().Be(actualResult.ScopeChecked, $"Expected the scope check to be {expectedScopeChecked} but it was {actualResult.ScopeChecked}.");
            expectedScopeMatchDiff.Should().Be(actualResult.ScopeMatchDiff, $"Expected scope match diff of {expectedScopeMatchDiff}, but actually got {actualResult.ScopeMatchDiff}.");
        }
    }
}

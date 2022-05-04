using CodeFinder;
using Microsoft.Internal.Fungates.DeveloperCanvas.Insight;
using Microsoft.Internal.Fungates.DeveloperCanvas.Insight.Insights;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace CodeFinderTest
{
    internal class InsightTest
    {
        private int findMatchesVersion;

        internal InsightTest(int findMatchesVersion = 2)
        {
            if (findMatchesVersion < 1 || findMatchesVersion > 2)
            {
                throw new ArgumentOutOfRangeException($"FindMatches version specified was {findMatchesVersion}, but must be 1 or 2");
            }

            this.findMatchesVersion = findMatchesVersion;
        }

        /// <summary>
        /// Gets insights for the given file path, finds the potential line matches, finds the actual line matches using CodeFinder,
        /// and prints out the results.
        /// </summary>
        /// <param name="filePath"></param>
        internal void RunTest(string filePath)
        {
            Console.WriteLine($"Getting insights for {filePath}...");

            // Get the insights for this file.
            var insights = InsightsWebApiClient.GetInsightV8Async(filePath, "CodeFinderTest", "1.0.0.0").Result;

            Console.WriteLine($"Got {insights.Count} insights for {filePath}");

            if (insights.Count == 0)
            {
                return;
            }

            // Order the insights by estimated line number.
            insights = insights.OrderBy(i => i.EstimatedLineNumber).ToList();

            // Get the potential matches (just a basic search for text).
            var allPotentialMatches = GetPotentialMatches(filePath, insights);

            // Construct a query for reach insight.
            var queries = new List<MatchQuery>(insights.Count);
            foreach (var insight in insights)
            {
                var id = GetInsightId(insight);
                var textToFind = insight.InsightText;
                var typeHint = MatchQuery.MatchTypeHint.Code;

                // If the text to find is the same as the calling function then we need to give that as a hint
                // in addition to ensuring that textToFind is only the function name (and not prepended by
                // namespace, class, etc.).
                if (textToFind == insight.CallingFunction)
                {
                    typeHint = MatchQuery.MatchTypeHint.Function;
                    textToFind = textToFind.Split(new string[] { ".", "::" }, StringSplitOptions.RemoveEmptyEntries).Last();
                }

                var query = new MatchQuery(textToFind, insight.EstimatedLineNumber, insight.CallingFunction, id, typeHint);
                queries.Add(query);
            }

            Console.WriteLine($"Finding matches using v{findMatchesVersion}...");

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var codeFinder = new CodeFinder.CodeFinder(filePath);
            var allMatches = new List<MatchResult>();
            if (findMatchesVersion == 1)
            {
                allMatches.AddRange(codeFinder.FindMatches(queries));
            }
            else
            {
                allMatches.AddRange(codeFinder.FindMatches2(queries));
            }
            stopWatch.Stop();

            Console.WriteLine($"Found {allMatches.Count} total match(es) in {stopWatch.ElapsedMilliseconds}ms");

            var passCount = 0;
            var failCount = 0;
            var bestCount = 0;
            foreach (var insight in insights)
            {
                var id = GetInsightId(insight);

                Console.WriteLine($"{insight.InsightName} {id}, Text: \"{insight.InsightText}\", Function: \"{insight.CallingFunction}\", Line: {insight.EstimatedLineNumber}");

                var query = queries.Where(q => q.Id == id).FirstOrDefault();
                Console.WriteLine($"\tQuery: Text: \"{query.TextToFind}\", Function: \"{query.FunctionSignature}\", TypeHint: {query.TypeHint.ToString()}");

                // Get the potential matches and the actual matches for this insight.
                var potentialMatches = allPotentialMatches.Where(m => m.Id == id).OrderBy(m => m.LineNumber);
                var matches = allMatches.Where(m => m.Id == id).OrderBy(m => m.LineNumber);

                // Get the potential matches that are present in the actual matches.
                var foundMatches = potentialMatches.Join(matches, potential => potential.Id, actual => actual.Id, (potential, actual) => new MatchResult(actual.Id, actual.Span, actual.LineNumber, actual.DistanceFromLineHint));

                Console.WriteLine("\tPotential match(es):");
                foreach (var match in potentialMatches)
                {
                    Console.WriteLine($"\t\tLine: {match.LineNumber}, DistanceFromLineHint: {match.DistanceFromLineHint}");
                }

                Console.WriteLine($"\tFound {matches.Count()} match(es):");
                if (matches.Count() == 0 && potentialMatches.Count() > 0)
                {
                    // If there are potential matches, but no actual matches, mark it as a fail.
                    Console.WriteLine("\t\tNone - FAIL");
                    failCount++;
                }
                else
                {
                    var bestMatch = MatchResult.GetBestMatch(matches.ToList());
                    foreach (var match in matches)
                    {
                        Console.Write($"\t\tLine: {match.LineNumber}, DistanceFromLineHint: {match.DistanceFromLineHint}, ScopesChecked: {match.ScopeChecked}, ScopeMatchDiff: {match.ScopeMatchDiff}");

                        if (match == bestMatch)
                        {
                            Console.Write(" - BEST");
                            bestCount++;
                        }

                        var foundMatch = foundMatches.Where(m => m.LineNumber == match.LineNumber).FirstOrDefault();
                        if (matches.Count() <= potentialMatches.Count())
                        {
                            if (foundMatch != null)
                            {
                                // This actual match corresponds to a potential match. Mark it as a pass.
                                Console.WriteLine(" - PASS");
                                passCount++;
                            }
                            else
                            {
                                Console.WriteLine(" - FAIL");
                                failCount++;
                            }
                        }
                        else
                        {
                            // There are more actual matches than potential matches. This is a failure.
                            Console.WriteLine(" - FAIL");
                            failCount++;
                        }
                    }
                }
            }

            Console.WriteLine("Summary:");
            Console.WriteLine($"\tTime: {stopWatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"\tInsights: {insights.Count}");
            Console.WriteLine($"\tPassed: {passCount}");
            Console.WriteLine($"\tFailed: {failCount}");
            Console.WriteLine($"\tBest: {bestCount}");
            Console.WriteLine();
        }

        /// <summary>
        /// Returns a list of all matches that may be found by the CodeFinder library.
        /// Does a basic search for all instances of the text for each insight and returns a list of MatchResults.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="insights"></param>
        /// <returns></returns>
        private static List<MatchResult> GetPotentialMatches(string filePath, List<InsightV8> insights)
        {
            var allMatches = new List<MatchResult>();

            var lines = File.ReadAllLines(filePath);

            foreach (var insight in insights)
            {
                var id = GetInsightId(insight);
                var lineNumber = 1;
                var matches = new List<MatchResult>();
                foreach (var line in lines)
                {
                    var textToFind = insight.InsightText;

                    // If insight.InsightText == insight.CallingFunction that indicates we're actually supposed to look for the function definition.
                    // We need make sure we only search for the actual function name (ignoring anything else in the function signature).
                    if (insight.InsightText == insight.CallingFunction)
                    {
                        textToFind = textToFind.Split(new string[] { ".", "::" }, StringSplitOptions.RemoveEmptyEntries).Last();
                    }

                    if (line.Contains(textToFind))
                    {                  
                        var distanceFromLineHint = Math.Abs(insight.EstimatedLineNumber - lineNumber);
                        matches.Add(new MatchResult(id, null, lineNumber, distanceFromLineHint));
                    }

                    lineNumber++;
                }

                allMatches.AddRange(matches);
            }

            return allMatches;
        }

        /// <summary>
        /// Returns a unique ID for the given insight.
        /// </summary>
        /// <param name="insight"></param>
        /// <returns></returns>
        private static string GetInsightId(InsightV8 insight)
        {
            StringBuilder id = new StringBuilder(insight.InsightId.ToString());

            foreach (var info in insight.InsightInfoList)
            {
                id.Append($"_{info.Id}");
            }

            var guid = InsightUtilities.GenerateGuidForString(id.ToString());
            return guid.ToString();
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Sarif.Viewer.CodeFinding
{
    /// <summary>
    /// A match result. This represents an found instance of the text from a match query.
    /// </summary>
    public class MatchResult
    {
        /// <summary>
        /// Gets the ID of the query that corresponds to this result.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the portion of the file where the match was found.
        /// </summary>
        public FileSpan Span { get; }

        /// <summary>
        /// Gets the line number where the start of the match was found.
        /// Use <see cref="Span"/> to get the absolute file positions of the match.
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// Gets a value indicating whether a calling scope was given for the query and the algorithm checked for it.
        /// False if the algorithm did not check against any scopes for this match.
        /// </summary>
        public bool ScopeChecked { get; }

        /// <summary>
        /// Gets how closely the scope(s) in the given function signature were matched against the actual scopes found.
        /// Only relevant if <see cref="ScopeChecked"/> is true.
        /// Matching is done from innermost scope to outermost. If at least one scope level does not match then it is not an overall match (and null is returned).
        /// 0 indicates an exact match.
        /// A positive value indicates the given function signature specified more scopes than were actually found, but those that were found matched.
        /// A negative value indicates the given function signature specified fewer scopes than were actually found, but all the given scopes matched.
        /// A null value indicates the scopes did not match.
        /// <br/>
        /// Examples:
        /// <list type="bullet">
        /// <item>
        /// If 2 scopes were given, 2 scopes were found, and they matched, then this would return 0.<br/>
        /// Given: MyClass::Foo<br/>
        /// Found: MyClass::Foo
        /// </item>
        /// <item>
        /// If 4 scopes were given but only 2 were found, and they matched, then this would return 2.<br/>
        /// Given: Microsoft.Windows.System.IO<br/>
        /// Found: System.IO
        /// </item>
        /// <item>
        ///  If 1 scope was given but 2 were found, and the single given scope matched the innermost found scope, then this would return -1.<br/>
        /// Given: Foo<br/>
        /// Found: MyClass::Foo
        /// </item>
        /// <item>
        /// If 2 scopes were given and 2 scopes were found, but only the first scope matched, then this would return null.<br/>
        /// Given: MyClass::Foo<br/>
        /// Found: OtherClass::Foo
        /// </item>
        /// </list>
        /// </summary>
        public int? ScopeMatchDiff { get; }

        /// <summary>
        /// Gets the absolute distance, in line numbers, from the original line number hint to the line where this match was found.
        /// </summary>
        public int DistanceFromLineHint { get; }

        /// <summary>
        /// Gets a value indicating whether indicates this result is a string literal.
        /// </summary>
        public bool StringLiteral { get; }

        public MatchResult(string id, FileSpan span, int lineNumber, int distanceFromLineHint, bool scopeChecked = false, int? scopeMatchDiff = null, bool stringLiteral = false)
        {
            Id = id;
            Span = span;
            LineNumber = lineNumber;
            DistanceFromLineHint = distanceFromLineHint;
            ScopeChecked = scopeChecked;
            ScopeMatchDiff = scopeMatchDiff;
            StringLiteral = stringLiteral;
        }

        public override bool Equals(object obj)
        {
            if (obj is MatchResult other)
            {
                if (Id == other.Id &&
                    Span.Id == other.Span.Id &&
                    LineNumber == other.LineNumber &&
                    DistanceFromLineHint == other.DistanceFromLineHint &&
                    ScopeChecked == other.ScopeChecked &&
                    ScopeMatchDiff == other.ScopeMatchDiff &&
                    StringLiteral == other.StringLiteral)
                {
                    return true;
                }
            }

            return false;
        }

        public override string ToString()
        {
            return $"{Id}_{Span.Id}_{LineNumber}_{DistanceFromLineHint}_{ScopeChecked}_{ScopeMatchDiff}_{StringLiteral}";
        }

        /// <summary>
        /// Returns the best match for a given set of matches.
        /// </summary>
        /// <param name="matches">The set of matches.</param>
        /// <param name="lineHintThreshold">Optional, defaults to 50. How close a match needs to be to the original line hint to be a candidate for best match.</param>
        /// <param name="preferStringLiterals">Optional, defaults to false. If true, only results that are string literals will be considered. If there are no string literals then all results will be considered.</param>
        /// <returns>The best match in a set of matches.</returns>
        public static MatchResult GetBestMatch(List<MatchResult> matches, int lineHintThreshold = 50, bool preferStringLiterals = false)
        {
            if (matches.Count == 1)
            {
                MatchResult match = matches[0];

                // There is only one match. Return it if:
                //  * We checked containing scopes and some number of scopes matched; or
                //  * We didn't check containing scopes and this match is within the line hint threshold.
                if ((match.ScopeChecked && match.ScopeMatchDiff != null) ||
                    (match.ScopeChecked == false && match.DistanceFromLineHint <= lineHintThreshold))
                {
                    return match;
                }
            }
            else if (matches.Count > 1)
            {
                // There are multiple matches. Try to return the best one as follows:
                // 1. If string literals are preferred and any exist, only consider those. Otherwise consider all matches.
                // 2. If containing scopes were checked, return the match that best matched the given scopes.
                //    a. If there are still multiple matches, return the match closest to the line hint.
                // 3. If containing scopes were not checked, return the match closest to the line hint.

                if (preferStringLiterals && matches.Where(m => m.StringLiteral).Any())
                {
                    matches = matches.Where(m => m.StringLiteral).ToList();
                }

                IEnumerable<MatchResult> matchesInScopes = matches.Where(m => m.ScopeChecked); // Get matches where scopes were checked.
                if (matchesInScopes.Any())
                {
                    matchesInScopes = matchesInScopes.Where(m => m.ScopeMatchDiff != null) // Get matches where the scopes actually matched.
                                                        .OrderBy(m => Math.Abs(m.ScopeMatchDiff.Value)); // Order them so that the one with the best ScopeMatchDiff value (closest to 0) is first.

                    // There is at least one match in a verified scope. Since they are ordered, the first one has the best ScopeMatchDiff value.
                    int minScopeMatcDiff = matchesInScopes.First().ScopeMatchDiff.Value;

                    return matchesInScopes.Where(m => m.ScopeMatchDiff.Value == minScopeMatcDiff) // Get all matches that have the best ScopeMatchDiff value.
                                            .OrderBy(m => m.DistanceFromLineHint) // Order by distance from line hint in case there are multiples matches still.
                                            .FirstOrDefault(); // Return the first match (best ScopeMatchDiff value and closest to line hint).
                }
                else
                {
                    // Scopes were not checked for any matches, so return the match closest to the original line hint (within the threshold).
                    return matches.Where(m => m.DistanceFromLineHint <= lineHintThreshold)
                                    .OrderBy(m => m.DistanceFromLineHint)
                                    .FirstOrDefault();
                }
            }

            return null;
        }

        public override int GetHashCode()
        {
            int hashCode = -509415362;
            int hashFactor = -1521134295;

            hashCode = (hashCode * hashFactor) + Id.GetHashCode();
            hashCode = (hashCode * hashFactor) + Span.Id.GetHashCode();
            hashCode = (hashCode * hashFactor) + LineNumber.GetHashCode();
            hashCode = (hashCode * hashFactor) + DistanceFromLineHint.GetHashCode();
            hashCode = (hashCode * hashFactor) + ScopeChecked.GetHashCode();
            hashCode = (hashCode * hashFactor) + ScopeMatchDiff.GetHashCode();
            hashCode = (hashCode * hashFactor) + StringLiteral.GetHashCode();
            return hashCode;
        }
    }
}

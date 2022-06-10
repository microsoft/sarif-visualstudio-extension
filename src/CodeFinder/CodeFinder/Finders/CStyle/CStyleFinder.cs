// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using Microsoft.CodeFinder.Extensions;

// Make this visible to the unit test project.
[assembly: InternalsVisibleTo("CodeFinderUnitTests")]

namespace Microsoft.CodeFinder.Finders.CStyle
{
    /// <summary>
    /// Implements functionality common to finding code in files of C-style languages (e.g. C, C++, C#).
    /// Language-specific C-style matchers should inherit from this class and override methods as necessary.
    /// </summary>
    internal abstract class CStyleFinder : CodeFinderBase
    {
        /// <summary>
        /// The set of language-specific keywords that could be attributed to a scope (e.g. "class", "if", "using").
        /// Inherited classes should fill out this list.
        /// </summary>
        protected HashSet<string> Keywords;

        public CStyleFinder(string fileContents) : base(fileContents)
        {
        }

        /// <summary>
        /// Returns a list of scope identifiers parsed out of the given function signature, in order
        /// from largest to smallest scope.
        /// Derived classes must implement this method.
        /// </summary>
        /// <param name="functionSignature">A string representing the function signature, including
        /// namespace and class, delimited with either "::" or "." as per the actual language used.
        /// E.g. "Namespace::Class.Method", "Namespace::Class::Method", "Class::Method", "Method",
        /// "Namespace1.Namespace2.Class.Method".
        /// </param>
        /// <returns></returns>
        protected abstract List<ScopeIdentifier> ParseFunctionSignature(string functionSignature);

        /// <summary>
        /// Finds all matches for the given query.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public override List<MatchResult> FindMatches(MatchQuery query)
        {
            // Get the scope identifiers from the function signature.
            // Derived classes must implement this method.
            List<ScopeIdentifier> identifiers = ParseFunctionSignature(query.FunctionSignature);

            // If the query is for a function definition, make sure the function signature includes the function itself
            // so that it gets found when looking for scope spans.
            if (query.TypeHint == MatchQuery.MatchTypeHint.Function)
            {
                if (identifiers.Count() == 0 ||
                    identifiers.Last().Name != query.TextToFind)
                {
                    identifiers.Add(new ScopeIdentifier(query.TextToFind, ScopeType.Function));
                }
            }

            // If there are no scope identifiers then fall back on the base implementation.
            if (identifiers.Count == 0)
            {
                return base.FindMatches(query);
            }

            // Using the scope identifiers, refine the search scope.
            IEnumerable<FileSpan> searchSpans = FindScopeSpans(identifiers);

            // Find matches within the search scope(s).
            List<MatchResult> matches;
            if (query.TypeHint == MatchQuery.MatchTypeHint.Function)
            {
                matches = FindFunctionDefinition(query, searchSpans);
            }
            else
            {
                if (searchSpans.Count() > 0)
                {
                    matches = FindCode(query, searchSpans);
                }
                else
                {
                    // We were unable to identify any scopes for the given function signature.
                    // Fall back to the base implementation so that we may return some (albeit possibly inaccurate) results.
                    return base.FindMatches(query);
                }
            }

            return matches;
        }

        /// <summary>
        /// Finds instances of the code specified by <paramref name="query"/> in the given <paramref name="searchSpans"/>.
        /// </summary>
        /// <param name="query">Specifies the code to search for.</param>
        /// <param name="searchSpans">A list of <see cref="FileSpan"/> to be searched.</param>
        /// <returns>A list of <see cref="MatchResult"/> representing the instances found.</returns>
        internal List<MatchResult> FindCode(MatchQuery query, IEnumerable<FileSpan> searchSpans)
        {
            var matches = new List<MatchResult>();

            // Within each span, look for instances of the given line of code.
            foreach (FileSpan searchSpan in searchSpans)
            {
                int start = searchSpan.Start;
                do
                {
                    int length = (searchSpan.End - start) + 1;
                    int pos = IndexOf(query.TextToFind, start, length, searchStringLiterals: true);
                    if (pos != -1)
                    {
                        int lineNumber = GetLineNumber(pos);
                        int distanceFromLineHint = Math.Abs(lineNumber - query.LineNumberHint);
                        matches.Add(new MatchResult(query.Id, new FileSpan(pos, pos + query.TextToFind.Length - 1), lineNumber, distanceFromLineHint, true, 0));

                        // Keep searching for more instances.
                        start = pos + query.TextToFind.Length;
                    }
                    else
                    {
                        break;
                    }
                }
                while (start <= searchSpan.End);
            }

            return matches;
        }

        /// <summary>
        /// Finds the function definition specified by <paramref name="query"/> using the given <paramref name="searchSpans"/>.
        /// This only finds the function definition, not calls to the function.
        /// If there are multiple definitions of the function then multiple results will be returned.
        /// </summary>
        /// <param name="query">Specifies the function definition to search for.</param>
        /// <param name="searchSpans">A list of <see cref="FileSpan"/> to be searched.</param>
        /// <returns>A list of <see cref="MatchResult"/> representing the instances found.</returns>
        internal List<MatchResult> FindFunctionDefinition(MatchQuery query, IEnumerable<FileSpan> searchSpans)
        {
            var matches = new List<MatchResult>();

            // If it exists, the function definition should be specified by one or more of the given spans.
            foreach (FileSpan span in searchSpans)
            {
                // Look backwards from the start of the given span for the first instance of the function name.
                int pos = LastIndexOf(query.TextToFind, span.Start, matchWholeWord: true);
                if (pos != -1)
                {
                    int lineNumber = GetLineNumber(pos);
                    int distanceFromLineHint = Math.Abs(lineNumber - query.LineNumberHint);
                    matches.Add(new MatchResult(query.Id, new FileSpan(pos, pos + query.TextToFind.Length - 1), lineNumber, distanceFromLineHint, true, 0));
                }
            }

            return matches;
        }

        /// <summary>
        /// For the given query, returns all potential matches.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public override List<MatchResult> FindMatches2(MatchQuery query)
        {
            string textToFind = query.TextToFind;

            // Because curly braces define scopes, they present an interesting challenge to this
            // algorithm. We will find them but it may be difficult to tell which one is the "best" one,
            // especially if no calling function signature is provided. Thus, we should fall back to the
            // base implementation if no calling function signature is provided and the caller wants to
            // find curly brace matches.
            if (string.IsNullOrWhiteSpace(query.FunctionSignature) &&
                (textToFind == "{" || textToFind == "}"))
            {
                return base.FindMatches2(query);
            }

            var matches = new List<MatchResult>();

            // Get the scope identifiers from the function signature.
            // Derived classes must implement this method.
            // TODO: Modify this method to only return strings and return them in order of smallest to largest scope.
            //       For now, just extract the scope names and reverse the list.
            List<ScopeIdentifier> functionSignatureScopes = ParseFunctionSignature(query.FunctionSignature);
            List<string> scopesToFind = functionSignatureScopes.Select(e => e.Name).Reverse().ToList();

            // If we're supposed to find a function definition, check to see if the function is included in the
            // given function signature. If so, remove it so that we correctly match the scopes that contain
            // the function definition and make sure the text to find is just the function itself.
            if (query.TypeHint == MatchQuery.MatchTypeHint.Function &&
                scopesToFind.Count > 0 &&
                textToFind.EndsWith(scopesToFind[0]))
            {
                textToFind = scopesToFind[0];
                scopesToFind.RemoveAt(0);
            }

            var textToFindTokens = new List<string>();
            if (query.MatchWholeTokens)
            {
                textToFindTokens = Tokenize(textToFind);
            }

            int textPos = 0;
            while (textPos != -1)
            {
                // Find the next instance of the text.
                textPos = IndexOf(textToFind, textPos, searchStringLiterals: true);
                if (textPos != -1)
                {
                    bool validInstance = true;
                    var foundScopes = new List<string>();

                    if (query.MatchWholeTokens)
                    {
                        // We need to see if the found instance is a whole token match or not.
                        // A whole token match is a match where each token in textToFind is found
                        // exactly in the (also tokenized) found instance.

                        // First expand the found instance region to the nearest non-word characters (excluding them).
                        // This ensures we're matching on whole tokens.
                        // E.g. if textToFind = "DoSomething" and the line of the found instance is "MyClass->EnableDoSomethingEx()" then
                        // we would expand the found instance of "DoSomething" to "EnableDoSomethingEx" (which is not a whole token match).

                        int start = textPos;
                        while (start > 0 && IsWordCharacter(FileContents[start - 1]))
                        {
                            start--;
                        }

                        int end = textPos + textToFind.Length; // "end" will be at the first character after the found instance.
                        while (end < EndOfFile - 1 && IsWordCharacter(FileContents[end]))
                        {
                            end++;
                        }

                        // Now tokenize the found instance (that has been expanded to the nearest whole token).
                        List<string> foundTokens = Tokenize(FileContents.Substring(start, end - start));

                        // Finally, compare the tokens in the found instance to the textToFind tokens.
                        // A null value means there is at least one token that doesn't match.
                        // A negative value means there are fewer tokens in the found instance.
                        // Both cases indicate that this is not a whole token match.
                        int? tokenDiff = foundTokens.Compare(textToFindTokens);
                        if (tokenDiff == null || tokenDiff < 0)
                        {
                            validInstance = false;
                        }
                    }

                    if (validInstance && query.TypeHint == MatchQuery.MatchTypeHint.Function)
                    {
                        // We're looking for a function definition so verify this instance of the text
                        // is indeed a function.
                        List<string> ids = GetScopeIdentifiers(textPos, out bool isFunction);
                        if (isFunction &&
                            ids.Count > 0 &&
                            ids[0] == textToFind)
                        {
                            // Make sure we add any outer scope identifers chained to the function definition (like class name).
                            ids.RemoveAt(0);
                            foundScopes.AddRange(ids);
                        }
                        else
                        {
                            validInstance = false;
                        }
                    }

                    if (validInstance)
                    {
                        int scopeSearchPos = textPos;

                        // Get all meaningful scopes that enclose the text.
                        while (scopeSearchPos >= 0)
                        {
                            FileSpan containingScope = GetScopeSpan(scopeSearchPos);
                            if (containingScope != null)
                            {
                                List<string> ids = GetScopeIdentifiers(containingScope.Start, out _);
                                foundScopes.AddRange(ids);

                                // Examine the next outer scope.
                                scopeSearchPos = containingScope.Start - 1;
                            }
                            else
                            {
                                break;
                            }
                        }

                        // Compare the given scopes with the scopes we actually found.
                        // If it returns null that means the scopes don't match at all and
                        // therefore this isn't a legitimate match.
                        int? scopeMatchDiff = scopesToFind.Compare(foundScopes);

                        if (scopeMatchDiff != null)
                        {
                            int lineNumber = GetLineNumber(textPos);
                            int distanceFromLineHint = Math.Abs(lineNumber - query.LineNumberHint);
                            matches.Add(new MatchResult(query.Id,
                                                        new FileSpan(textPos, textPos + textToFind.Length - 1),
                                                        lineNumber,
                                                        distanceFromLineHint,
                                                        true,
                                                        scopeMatchDiff));
                        }
                    }

                    // Keep searching for more instances of the text.
                    textPos += textToFind.Length;
                }
            }

            return matches;
        }

        /// <summary>
        /// For the given chain of scope identifiers, finds the FileSpan(s) for the final scope in the chain.
        /// E.g. if identifiers contains "Foo", "Bar", "SomeMethod" then this method will first search for
        /// "Foo", then search for "Bar" within Foo's scope(s), then search for "SomeMethod" within Bar's scope(s).
        /// When you first call this method, you only need to supply the list of identifiers and the method
        /// will automatically start looking for the first identifier in the entire file.
        /// </summary>
        /// <param name="identifiers">The chain of scope identifiers, in order from largest to smallest.</param>
        /// <param name="depth">Optional. Indicates how deep into the identifiers chain to start. Omit when first calling this method.</param>
        /// <param name="searchSpan">Optional. Specifies the part of the file to search. Omit when first calling this method.</param>
        /// <returns>An IEnumerable containing 0 or more FileSpans.</returns>
        internal IEnumerable<FileSpan> FindScopeSpans(List<ScopeIdentifier> identifiers, int depth = 0, FileSpan searchSpan = null)
        {
            // If no search span was provided, default to the entire file.
            if (searchSpan == null)
            {
                searchSpan = new FileSpan(0, EndOfFile);
            }

            if (depth >= identifiers.Count())
            {
                // We've run out of identifiers to search for so just return whatever was passed to us.
                yield return searchSpan;
            }
            else
            {
                // Look for the identifier at this depth within the given search span.
                ScopeIdentifier identifier = identifiers[depth];
                int searchStart = searchSpan.Start;
                do
                {
                    // First look for a whole word instance of the identifier.
                    int length = (searchSpan.End - searchStart) + 1;
                    int pos = IndexOf(identifier.Name, searchStart, length, true);
                    if (pos != -1)
                    {
                        // Default to searching again directly after this instance of the identifier.
                        searchStart = pos + identifier.Name.Length;

                        // If an open curly brace occurs before the next semi-colon then this identifier precedes a scope.
                        // Whether or not the identifier has any part in the ownership of the scope is determined below.
                        int openCurly = IndexOf('{', pos);
                        int semiColon = IndexOf(';', pos);
                        if (openCurly != -1 && openCurly < semiColon)
                        {
                            // When we continue searching for this identifier, do it at the end of this scope.
                            // This ensures we don't return the same scope more than once if the identifier name
                            // appears more than once in the identifier chain (e.g. in a constructor definition like "Foo::Foo() { }").
                            FileSpan scopeSpan = GetScopeSpan(openCurly + 1);
                            if (scopeSpan != null)
                            {
                                searchStart = scopeSpan.End;

                                // Get the scope's type and identifiers.
                                ScopeType scopeType = GetScopeInfo(openCurly, out List<string> scopeIdentifiers);

                                // Walk the identifier chain to see if the identifier we found owns this scope or
                                // perhaps another identifier in the chain owns it.
                                ScopeIdentifier scopeOwner = null;
                                int depth2 = depth;
                                foreach (string scopeIdentifier in scopeIdentifiers)
                                {
                                    if (depth2 < identifiers.Count() &&
                                        identifiers[depth2].Name == scopeIdentifier)
                                    {
                                        scopeOwner = identifiers[depth2];
                                        depth2++;
                                    }
                                    else
                                    {
                                        scopeOwner = null;
                                        break;
                                    }
                                }

                                if (scopeOwner != null)
                                {
                                    // One of our identifiers owns this scope. Now verify the type is what we expect.
                                    if ((scopeType == scopeOwner.Type) ||
                                        (scopeOwner.Type == ScopeType.Unknown))
                                    {
                                        // Now that we verified this identifier owns this scope, we can say for certain it is explicit (e.g. not implicitly declared via "using namespace").
                                        identifier.Explicit = true;

                                        // Search within this scope for the next identifier. (If this is the last identifer then this call will just return the current scope.)
                                        IEnumerable<FileSpan> subScopes = FindScopeSpans(identifiers, depth2, scopeSpan);
                                        foreach (FileSpan subScope in subScopes)
                                        {
                                            yield return subScope;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // If this identifier's scope may be implicit (e.g. declared via "using namespace") then call this method again
                        // to look for the next identifier within the current scope.
                        if (identifier.Explicit == false)
                        {
                            IEnumerable<FileSpan> subScopes = FindScopeSpans(identifiers, depth + 1, searchSpan);
                            foreach (FileSpan subScope in subScopes)
                            {
                                yield return subScope;
                            }
                        }

                        // Break out since we're done searching for this particular identifier.
                        break;
                    }
                }
                while (searchStart <= searchSpan.End);
            }
        }

        /// <summary>
        /// Returns a span for the scope that contains the given starting index.
        /// If the starting index indicates an open or close curly brace then the span returned
        /// will be for the scope defined by that curly brace.
        /// The returned span includes the open and close curly braces that define the scope.
        /// </summary>
        /// <param name="startIndex"></param>
        /// <returns>A FileSpan representing the scope or null if no scope was identified.</returns>
        internal virtual FileSpan GetScopeSpan(int startIndex)
        {
            // Make sure the given index is sane.
            if (startIndex < 0 || startIndex > EndOfFile)
            {
                return null;
            }

            // If we start at a closing curly brace, back up one character to ensure the logic
            // below doesn't get confused by encountering an extra closing curly brace.
            if (FileContents[startIndex] == '}')
            {
                startIndex--;
            }

            // The given start index may be in an ignored span. If not, get the previous ignored span.
            FileSpan ignoredSpan = IgnoredSpans.GetContainingSpan(startIndex);
            if (ignoredSpan == null)
            {
                ignoredSpan = IgnoredSpans.GetPreviousSpan(startIndex);
            }

            // Search backwards for the first un-matched open curly brace.
            int start = -1;
            int level = 0;
            for (int i = startIndex; i >= 0 && start == -1; i--)
            {
                if (ignoredSpan != null && ignoredSpan.Contains(i))
                {
                    // We encountered an ignored span, jump to the start of it and keep searching.
                    i = ignoredSpan.Start;
                    ignoredSpan = IgnoredSpans.GetPreviousSpan(i);
                }
                else
                {
                    char c = FileContents[i];
                    if (c == '}')
                    {
                        //Console.WriteLine($"Level {level}: {{ on line {GetLineNumber(i)}"); // Useful for debugging when this method returns null.
                        level++;
                    }
                    else if (c == '{')
                    {
                        level--;
                        //Console.WriteLine($"Level {level}: }} on line {GetLineNumber(i)}"); // Useful for debugging when this method returns null.
                        if (level < 0)
                        {
                            start = i;
                        }
                    }
                }
            }

            // We didn't find an unmatched open curly brace so the given startIndex is not actually in an identifiable scope.
            if (start == -1)
            {
                return null;
            }

            ignoredSpan = IgnoredSpans.GetNextSpan(start);

            // Look for open and close curly braces until we find the close curly brace that pairs with the first one.
            level = 0;
            for (int i = start; i <= EndOfFile; i++)
            {
                if (ignoredSpan != null && ignoredSpan.Contains(i))
                {
                    // We've encoutered an ignored span, jump to the end of it and get the next one.
                    i = ignoredSpan.End;
                    ignoredSpan = IgnoredSpans.GetNextSpan(i);
                }
                else
                {
                    char c = FileContents[i];
                    if (c == '{')
                    {
                        //Console.WriteLine($"Level {level}: {{ on line {GetLineNumber(i)}"); // Useful for debugging when this method returns null.
                        level++;
                    }
                    else if (c == '}')
                    {
                        level--;
                        //Console.WriteLine($"Level {level}: }} on line {GetLineNumber(i)}"); // Useful for debugging when this method returns null.
                        if (level == 0)
                        {
                            return new FileSpan(start, i);
                        }

                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the scope span that contains the given (1-indexed) line.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        internal FileSpan GetScopeSpanAtLine(int line)
        {
            FileSpan span = GetFileSpanForLine(line);
            return GetScopeSpan(span.End);
        }

        /// <summary>
        /// Returns a list of identifiers for the scope immediately at or after the given <paramref name="startIndex"/>.
        /// Identifiers are in order of innermost to outermost (i.e. the identifier at index 0 is the innermost).
        /// Language keywords are not returned as identifiers (e.g. a for loop scope will return no identifiers).
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="isFunction">Returns true if the scope appears to be a function definition.</param>
        /// <returns></returns>
        internal List<string> GetScopeIdentifiers(int startIndex, out bool isFunction)
        {
            var identifiers = new List<string>();
            isFunction = false;

            // First, find the next open curly brace from startIndex. This marks the end of the string to parse.
            int openCurly = IndexOf('{', startIndex);
            if (openCurly <= 0)
            {
                return identifiers;
            }

            // From the open curly brace, look backwards for the previous semi-colon, close curly brace, or open curly brace.
            int prevSemiColon = LastIndexOf(';', openCurly - 1);
            int prevCloseCurly = LastIndexOf('}', openCurly - 1);
            int prevOpenCurly = LastIndexOf('{', openCurly - 1);

            // Determine which is closest to the scope's opening curly brace and then get the intermediate string.
            // This is the string that we'll try to parse to determine the type and identity of the scope.
            startIndex = Math.Max(Math.Max(prevSemiColon, prevCloseCurly), prevOpenCurly) + 1;
            string str = Substring(startIndex, openCurly - startIndex);

            // To make the string easier to tokenize, first sanitize it by:
            // Removing anything between parentheses, but keeping the parentheses as these are useful landmarks.
            // Removing anything related to templates or arrays, including the template/array characters.
            str = str.RemoveBetween('(', ')', false);
            str = StringExtensions.RemoveBetween(str, '<', '>');
            str = StringExtensions.RemoveBetween(str, '[', ']');

            // Tokenize the sanitized string to make it easier to parse.
            List<string> tokens = Tokenize(str);

            // Determine where within the list of tokens to start looking for identifiers.
            // In most cases it should start with the last token in the list. However, there
            // are other cases we need to check for.
            int lastIdentifierPos = tokens.Count - 1;

            int openParensPos = tokens.LastIndexOf("(");
            if (tokens.Contains("class"))
            {
                // If this is a class that inherits then there should be a single colon, e.g. "public class MyClass : BaseClass".
                // In this case the identifiers start immediately to the left of the colon.
                int singleColon = tokens.LastIndexOf(":");
                if (singleColon != -1)
                {
                    lastIdentifierPos = singleColon - 1;
                }
            }
            else if (tokens.Contains("namespace"))
            {
                // For namespaces, the identifiers will always be to the right of the "namespace" keyword so reduce the set of tokens
                // to just those.
                int namespacePos = tokens.LastIndexOf("namespace");
                tokens.RemoveRange(0, namespacePos + 1);
                lastIdentifierPos = tokens.Count - 1;
            }
            else if (openParensPos != -1)
            {
                // This has parentheses so it's probably a function. We may invalidate this later.
                isFunction = true;

                // The identifiers will be immediately to the left of the parentheses. E.g. "void Foo()" -> "Foo" or "MyClass::Foo()" -> "MyClass::Foo".
                // We look for the parentheses closest to the scope's open curly brace (using LastIndexOf) to avoid macros that may precede the actual function name.
                // Those macros may take parameters and contain parentheses (like some SAL annotations) that would otherwise cause us to misidentify the scope.
                lastIdentifierPos = openParensPos - 1;

                // If this is a constructor then there may be a single colon in the token list, e.g. "MyClass::MyClass() : base()".
                // In this case the identifiers will be immediately to the left of the parentheses that occur *before* the colon.
                // However, we also need to look out for (and ignore) the case where the colon appears from something like "public:" or "private:".
                // In that case, the colon should not be preceded by parentheses.
                int colonPos = tokens.LastIndexOf(":");
                if (colonPos != -1)
                {
                    openParensPos = tokens.LastIndexOf("(", colonPos);
                    if (openParensPos != -1)
                    {
                        lastIdentifierPos = openParensPos - 1;
                    }
                }
            }

            // Finally, starting from where we think the last identifier is, walk back the list of tokens, collecting identifiers.
            // Multiple identifiers may be "chained" together with "::" or ".".
            if (lastIdentifierPos >= 0)
            {
                bool chained = false;
                for (int i = lastIdentifierPos; i >= 0; i--)
                {
                    string token = tokens[i];
                    if (IsWord(token))
                    {
                        // Accept this token as an identifier if:
                        // * We haven't found any other identifiers and it's not a language keyword; or
                        // * It's chained to the previous identifier.
                        if ((identifiers.Count == 0 && Keywords.Contains(token) == false) || chained == true)
                        {
                            identifiers.Add(token);
                        }
                        else if (identifiers.Count > 0 && chained == false)
                        {
                            // If we already have at least one identifier and this isn't a chain, then we're done.
                            break;
                        }

                        // This token breaks the chain. A subsqeuent chaining character may continue the chain.
                        chained = false;
                    }
                    else if (token == "::" || token == ".")
                    {
                        // Identifiers are only chained by "::" or ".".
                        chained = true;
                    }
                    else
                    {
                        // If this token is not a word and not a chaining token then we're done.
                        break;
                    }
                }
            }

            // If we couldn't identify this scope then it definitely isn't a function.
            if (identifiers.Count == 0)
            {
                isFunction = false;
            }

            return identifiers;
        }

        /// <summary>
        /// Returns information about the scope that occurs immediately after the given <paramref name="startIndex"/>.
        /// </summary>
        /// <param name="startIndex">The starting index that precedes the desired scope. Typically this is the index of the scope's opening curly brace.</param>
        /// <param name="identifiers">The list of identifiers for the scope.</param>
        /// <returns>The type of the scope.</returns>
        internal ScopeType GetScopeInfo(int startIndex, out List<string> identifiers)
        {
            ScopeType scopeType;
            identifiers = new List<string>();

            // First, find the next open curly brace from startIndex. This marks the end of the string to parse.
            int openCurly = IndexOf('{', startIndex);
            if (openCurly <= 0)
            {
                return ScopeType.None;
            }

            // From the open curly brace, look backwards for the previous semi-colon, close curly brace, or open curly brace.
            int prevSemicolon = LastIndexOf(';', openCurly - 1);
            int prevCloseCurly = LastIndexOf('}', openCurly - 1);
            int prevOpenCurly = LastIndexOf('{', openCurly - 1);

            // Determine which is closest to the scope's opening curly brace and then get the intermediate string.
            // This is the string that we'll try to parse to determine the type and identity of the scope.
            startIndex = Math.Max(prevSemicolon, Math.Max(prevCloseCurly, prevOpenCurly)) + 1;
            string str = Substring(startIndex, openCurly - startIndex);

            // To make the string easier to tokenize, first sanitize it by:
            // Removing anything between parentheses, but keeping the parentheses as these are useful landmarks.
            // Removing anything related to templates or arrays, including the template/array characters.
            str = str.RemoveBetween('(', ')', false);
            str = StringExtensions.RemoveBetween(str, '<', '>');
            str = StringExtensions.RemoveBetween(str, '[', ']');

            // Tokenize the sanitized string to make it easier to parse.
            List<string> tokens = Tokenize(str);
            if (tokens.Count == 0)
            {
                // No tokens means this scope doesn't have any identifiers.
                return ScopeType.None;
            }

            // Try to classify this scope and figure out where the identifiers are in the token list.
            // Note that if the open parens is first (openParensPos == 0) then this is probably a lambda and there are no identifiers.
            int lastIdentifierPos = -1;
            int openParensPos = tokens.LastIndexOf("(");
            if (openParensPos > 0)
            {
                // The identifiers will be immediately to the left of the parentheses. E.g. "void Foo()" -> "Foo" or "MyClass::Foo()" -> "MyClass::Foo".
                // We look for the parentheses closest to the scope's open curly brace (using LastIndexOf) to avoid macros that may precede the actual function name.
                // Those macros may take parameters and contain parentheses (like some SAL annotations) that would otherwise cause us to misidentify the scope.
                lastIdentifierPos = openParensPos - 1;

                // If this is a constructor then there may be a single colon in the token list, e.g. "MyClass::MyClass() : base()".
                // In this case the identifiers will be immediately to the left of the parentheses that occur *before* the colon.
                // However, we also need to look out for (and ignore) the case where the colon appears from something like "public:" or "private:".
                // In that case, the colon should not be preceded by parentheses.
                int colonPos = tokens.LastIndexOf(":");
                if (colonPos != -1)
                {
                    openParensPos = tokens.LastIndexOf("(", colonPos);
                    if (openParensPos != -1)
                    {
                        lastIdentifierPos = openParensPos - 1;
                    }
                }

                // This may be a function or it may be a some sort of control block (e.g. if, for, catch, etc.).
                if (Keywords.Contains(tokens[lastIdentifierPos]))
                {
                    lastIdentifierPos = -1;
                    scopeType = ScopeType.Control;
                }
                else
                {
                    scopeType = ScopeType.Function;
                }
            }
            else
            {
                // There are no parentheses so this should be a namespace, class, or struct.
                // Figure out which it is and where the identifiers are in the token list.
                if (tokens.Contains("namespace"))
                {
                    scopeType = ScopeType.Namespace;
                    lastIdentifierPos = tokens.Count - 1;
                }
                else if (tokens.Contains("class"))
                {
                    scopeType = ScopeType.Class;
                    lastIdentifierPos = tokens.Count - 1;

                    // If this class inherits then there should be a single colon, e.g. "public class MyClass : BaseClass".
                    // In this case the identifiers start immediately to the left of the colon.
                    int singleColon = tokens.LastIndexOf(":");
                    if (singleColon != -1)
                    {
                        lastIdentifierPos = singleColon - 1;
                    }
                }
                else if (tokens.Contains("struct"))
                {
                    scopeType = ScopeType.Struct;
                    lastIdentifierPos = tokens.Count - 1;
                }
                else if (Keywords.Contains(tokens[tokens.Count - 1]))
                {
                    // If the last token is a keyword then this is a control block.
                    scopeType = ScopeType.Control;
                }
                else
                {
                    // Whatever this is, we don't currently identify it.
                    scopeType = ScopeType.Unknown;
                }
            }

            // Finally, starting from where we think the last identifier is, walk back the list of tokens, collecting identifiers.
            // Multiple identifiers may be "chained" together with "::" or ".".
            if (lastIdentifierPos >= 0)
            {
                bool chained = false;
                for (int i = lastIdentifierPos; i >= 0; i--)
                {
                    string token = tokens[i];
                    if (IsWord(token))
                    {
                        // This is only an identifier if it's the first word or if it was prceded by a chaining character.
                        if (identifiers.Count == 0 || chained == true)
                        {
                            identifiers.Insert(0, token);
                        }
                        else
                        {
                            // If we encounter another word that isn't part of a chain then we're done.
                            break;
                        }

                        // This word breaks the chain. A subsqeuent chaining character may
                        chained = false;
                    }
                    else if (token == "::" || token == ".")
                    {
                        // Identifiers are only chained by "::" or ".".
                        chained = true;
                    }
                    else
                    {
                        // If this token is not a word and not a chaining token then we're done.
                        break;
                    }
                }
            }

            return scopeType;
        }

        /// <summary>
        /// Breaks the given string up into tokens and returns the list of tokens, in order.
        /// A token can be a whole word (like "Foo" or "namespace") or a single character (like ".", ":", or "*").
        /// For convenience, a double-colon ("::") is considered a single token.
        /// Whitespace characters are ignored.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        protected virtual List<string> Tokenize(string str)
        {
            var tokens = new List<string>();
            var token = new StringBuilder();

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];

                if (IsWordCharacter(c, token.Length == 0))
                {
                    // This is a word character, append it to what we've collected so far.
                    token.Append(c);
                }
                else
                {
                    // This is not a word character.

                    if (token.Length > 0)
                    {
                        // We've collected at least one other character so this signifies the end of the current token.
                        tokens.Add(token.ToString());
                        token.Clear();
                    }

                    // If this isn't a whitespace character then it should be added to the list of tokens.
                    if (char.IsWhiteSpace(c) == false)
                    {
                        if (c == ':' && tokens.Count > 0 && tokens.Last() == ":")
                        {
                            // If this is a colon and the last token was a colon, then coalesce it into a single double-colon token ("::").
                            tokens[tokens.Count - 1] = "::";
                        }
                        else
                        {
                            // Add this non-word, non-whitespace character to the list of tokens.
                            tokens.Add(c.ToString());
                        }
                    }
                }
            }

            // If we reached the end of the string and there's a token that hasn't been committed yet, do it now.
            if (token.Length > 0)
            {
                tokens.Add(token.ToString());
            }

            return tokens;
        }

        /// <summary>
        /// Returns true if the given character is a valid "word" character.
        /// A "word" can be an identifier (e.g. function or variable name) or a language-reserved keyword
        /// (e.g. "namespace" or "if").
        /// A "word" may not start with a number and it may contain one or more underscores.
        /// A "word" may contain a tilde, but only as the first character (e.g. to indicate a destructor in C++).
        /// </summary>
        /// <param name="c"></param>
        /// <param name="first">Optional. Indicates that the character is the first character in the word.</param>
        /// <returns></returns>
        protected static bool IsWordCharacter(char c, bool first = false)
        {
            // The first character of a word cannot be a number.
            if (first && char.IsDigit(c))
            {
                return false;
            }

            // A tilde is only valid as the first character.
            if (first && c == '~')
            {
                return true;
            }

            // Words can be made up of any alphanumeric character plus underscores.
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the given string is a C-style "word".
        /// A word is defined as a sequence of one or more alphanumeric characters, plus underscores.
        /// A word may not start with a number. A word may start with a tilde (i.e. for destructors).
        /// </summary>
        /// <param name="word">The string to check.</param>
        /// <param name="fastCheck">Optional, defaults to true. When set to true, this method only checks
        /// the first character of <see cref="word"/>. When set to false, every character is checked.</param>
        /// <returns></returns>
        protected static bool IsWord(string word, bool fastCheck = true)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return false;
            }

            if (IsWordCharacter(word[0], true) == false)
            {
                return false;
            }

            if (fastCheck == false)
            {
                for (int i = 1; i < word.Length; i++)
                {
                    if (IsWordCharacter(word[i]) == false)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Returns spans that should be ignored (comments and string/character literals).
        /// </summary>
        /// <returns></returns>
        protected override FileSpanCollection GetIgnoredSpans()
        {
            var spans = new List<FileSpan>();

            int blockCommentStart = -1;
            int lineCommentStart = -1;
            int stringStart = -1;
            int charStart = -1;

            // Start at the beginning of the file, looking for tokens that indicate the start of a block comment, line comment, string literal, or character literal.
            // Once inside a block comment, line comment, string literal, or character literal, look for the terminating token.
            for (int i = 0; i <= EndOfFile; i++)
            {
                char prevPrevChar = i > 1 ? FileContents[i - 2] : (char)0;
                char prevChar = i > 0 ? FileContents[i - 1] : (char)0;
                char curChar = FileContents[i];
                string next2 = FileContents.Substring(i, i < EndOfFile ? 2 : 1);

                if (blockCommentStart == -1 &&
                    lineCommentStart == -1 &&
                    stringStart == -1 &&
                    charStart == -1)
                {
                    // If we're not in any comment or string, look for the start of one.

                    if (next2 == "/*")
                    {
                        blockCommentStart = i;
                    }
                    else if (next2 == "//")
                    {
                        lineCommentStart = i;
                    }
                    else if (curChar == '"')
                    {
                        stringStart = i;
                    }
                    else if (curChar == '\'')
                    {
                        charStart = i;
                    }
                }
                else if (blockCommentStart != -1 && next2 == "*/")
                {
                    // We're in a block comment and we found the end of it.
                    spans.Add(new FileSpan(blockCommentStart, i + 1, FileSpan.FileSpanTag.Comment));
                    blockCommentStart = -1;
                }
                else if (lineCommentStart != -1 && curChar == '\n')
                {
                    // We're in a line comment and we found the end of the line.
                    spans.Add(new FileSpan(lineCommentStart, i, FileSpan.FileSpanTag.Comment));
                    lineCommentStart = -1;
                }
                else if (stringStart != -1 && curChar == '"')
                {
                    // We're in a string literal and we may have found the end of it.

                    // Make sure this instance of the double-quote wasn't escaped.
                    // If it was esceped, make sure the escape character itself wasn't escaped.
                    // That is, \" is not the end of the string, but \\" is.
                    if (prevChar != '\\' ||
                        (prevChar == '\\' && prevPrevChar == '\\'))
                    {
                        spans.Add(new FileSpan(stringStart, i, FileSpan.FileSpanTag.StringLiteral));
                        stringStart = -1;
                    }
                }
                else if (charStart != -1 && curChar == '\'')
                {
                    // We're in a character literal and we may have found the end of it.

                    // Make sure this instance of the single-quote wasn't escaped.
                    // If it was esceped, make sure the escape character itself wasn't escaped.
                    // That is, \' is not the end of the string, but \\' is.
                    if (prevChar != '\\' ||
                        (prevChar == '\\' && prevPrevChar == '\\'))
                    {
                        spans.Add(new FileSpan(charStart, i, FileSpan.FileSpanTag.CharLiteral));
                        charStart = -1;
                    }
                }
            }

            return new FileSpanCollection(spans);
        }
    }
}

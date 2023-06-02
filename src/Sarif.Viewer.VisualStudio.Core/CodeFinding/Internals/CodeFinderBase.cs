// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Sarif.Viewer.CodeFinding.Internal
{
    /// <summary>
    /// Abstract class that all code finders should inherit from.
    /// Also provides some basic, common functionality that most finders will likely need.
    /// </summary>
    internal abstract class CodeFinderBase
    {
        /// <summary>
        /// The complete contents of the file.
        /// </summary>
        protected string fileContents;

        /// <summary>
        /// The index of the very last character of the file. Character positions are 0-indexed.
        /// </summary>
        protected int endOfFile;

        /// <summary>
        /// The number of lines in the file. Lines are 1-indexed.
        /// </summary>
        protected int lineCount;

        /// <summary>
        /// Gets the collection of portions (spans) of the file that should be ignored when searching for matches.
        /// Cached result of <see cref="GetIgnoredSpans()"/>. Can only be read by derived classes.
        /// </summary>
        protected FileSpanCollection IgnoredSpans { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeFinderBase"/> class.
        /// Creates a line matcher based on the given file contents.
        /// </summary>
        /// <param name="fileContents">The string content of a file.</param>
        public CodeFinderBase(string fileContents)
        {
            this.fileContents = fileContents;
            this.endOfFile = this.fileContents.Length - 1;

            lineCount = GetLineNumber(endOfFile);

            IgnoredSpans = GetIgnoredSpans();
        }

        /// <summary>
        /// Derived classes that handle files that may have regions that should be ignored (e.g. code comments)
        /// should implement this method.
        /// </summary>
        /// <returns>A list of ignored filespans.</returns>
        protected virtual FileSpanCollection GetIgnoredSpans()
        {
            return new FileSpanCollection(null);
        }

        /// <summary>
        /// Finds all matches for the given query.
        /// This is a basic algorithm that ignores the query's function signature and just looks for instances
        /// of the given text throughout the entire file.
        /// Derived classes should override this and provide their own implementation, falling back
        /// to the base implementation if necessary.
        /// The confidence score for each match takes into account:
        ///  * How close the match is to the given line hint.
        ///  * How many other instances of the match there are.
        /// </summary>
        /// <param name="query">The query being searched for.</param>
        /// <returns>List of possible matches.</returns>
        public virtual List<MatchResult> FindMatches(MatchQuery query)
        {
            return FindMatchesBasic(query);
        }

        /// <summary>
        /// Finds all matches for the given query.
        /// For the base class, this is identical to <see cref="FindMatches(MatchQuery)"/>.
        /// Derived classes should override this and provide their own implementation, falling back
        /// to the base implementation if necessary.
        /// </summary>
        /// <param name="query">The query to find matches for.</param>
        /// <returns>The list of possible matches.</returns>
        public virtual List<MatchResult> FindMatchesWithFunction(MatchQuery query)
        {
            return FindMatchesBasic(query);
        }

        /// <summary>
        /// Implements a very basic algorithm that just looks for instances of the given text.
        /// </summary>
        /// <param name="query">The query to find matches for.</param>
        /// <returns>The list of possible matches.</returns>
        private List<MatchResult> FindMatchesBasic(MatchQuery query)
        {
            var matches = new List<MatchResult>();

            // Find all instances of the given text.
            int start = 0;
            do
            {
                int length = (endOfFile - start) + 1;
                int pos = IndexOf(query.TextToFind, start, length, searchStringLiterals: true);
                if (pos != -1)
                {
                    int lineNumber = GetLineNumber(pos);
                    int distanceFromLineHint = Math.Abs(lineNumber - query.LineNumberHint);
                    matches.Add(new MatchResult(query.Id, new FileSpan(pos, pos + query.TextToFind.Length - 1), lineNumber, distanceFromLineHint, stringLiteral: IsStringLiteral(pos)));

                    // Keep searching for more instances.
                    start = pos + query.TextToFind.Length;
                }
                else
                {
                    break;
                }
            }
            while (start <= endOfFile);

            return matches;
        }

        /// <summary>
        /// Returns the (1-indexed) line number for the given (0-indexed) file position.
        /// </summary>
        /// <param name="filePosition">The file position we are trying to get a line number for.</param>
        /// <returns>The line number.</returns>
        protected virtual int GetLineNumber(int filePosition)
        {
            if (filePosition >= fileContents.Length)
            {
                return -1;
            }

            int line = 0;
            int pos = 0;
            while (pos <= filePosition && pos != -1)
            {
                pos = fileContents.IndexOf('\n', pos);
                if (pos != -1)
                {
                    pos++;
                    line++;
                }
            }

            return line;
        }

        /// <summary>
        /// Returns a FileSpan representing the given (1-indexed) line, including the ending newline character.
        /// </summary>
        /// <param name="line">The line number.</param>
        /// <returns>File span representing the line.</returns>
        protected virtual FileSpan GetFileSpanForLine(int line)
        {
            int currentLine = 0;
            int pos = 0;
            int lineStart = 0;
            while (pos < fileContents.Length && pos != -1)
            {
                pos = fileContents.IndexOf('\n', pos);
                if (pos != -1)
                {
                    currentLine++;
                    if (currentLine == line)
                    {
                        return new FileSpan(lineStart, pos);
                    }

                    pos++;
                    lineStart = pos;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the file position (0-indexed) at the start of the given (1-indexed) line number.
        /// </summary>
        /// <param name="lineNumber">The line number the start index is based on.</param>
        /// <returns>The file position at the start of a specific line number.</returns>
        protected virtual int GetStartIndex(int lineNumber)
        {
            int line = 1;
            int pos = 0;
            while (pos <= endOfFile && pos != -1)
            {
                if (line == lineNumber)
                {
                    return pos;
                }
                else
                {
                    pos = fileContents.IndexOf('\n', pos);
                    if (pos != -1)
                    {
                        pos++;
                        line++;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// A version of IndexOf that returns the index of the given character or string within the file,
        /// but only if that character or string does not reside within an ignored span (e.g. comment or string literal).
        /// </summary>
        /// <param name="value">The character or string to search for. Must be of type "char" or "string".</param>
        /// <param name="startIndex">The position to start searching at. Omit to start at the beginning of the file.</param>
        /// <param name="count">How many characters to examine. Omit to examine to the end of the file.</param>
        /// <param name="matchWholeWord">Set to true if you want to match on a whole word (only applicable for string values).</param>
        /// <param name="searchStringLiterals">Set to true if you want to include string literals (which are ignored by default) in the search.</param>
        /// <returns>The starting position of the first occurrence of the string in the file, if found. Otherwise, -1.</returns>
        protected virtual int IndexOf(object value, int startIndex = 0, int count = -1, bool matchWholeWord = false, bool searchStringLiterals = false)
        {
            // If we should search within string literals then make sure we ignore any ignored spans that are a string literal.
            List<FileSpan.FileSpanTag> excludeTags = null;
            if (searchStringLiterals)
            {
                excludeTags = new List<FileSpan.FileSpanTag> { FileSpan.FileSpanTag.StringLiteral };
            }

            int valueLength = 1;
            char? c = null;
            string s = string.Empty;

            // Determine if the given value is a char or a string.
            // string.IndexOf(char) is faster than string.IndexOf(string) so we should try to use it whenever possible,
            // e.g. when the caller is looking for '{' or '\n' vs. "namespace".
            if (value is char @char)
            {
                c = @char;
            }
            else if (value is string @string)
            {
                s = @string;
                valueLength = s.Length;

                // If it's a single-character string, optimize by making it a char.
                if (s.Length == 1)
                {
                    c = s[0];
                }
            }
            else
            {
                throw new ArgumentException($"value is a {value.GetType()}, it must be a char or a string");
            }

            // Make sure the given count doesn't exceed the end of the file.
            if (count == -1 || (startIndex + count) - 1 > endOfFile)
            {
                count = (endOfFile - startIndex) + 1;
            }

            int start = startIndex;
            int end = (start + count) - 1;
            do
            {
                int pos;
                count = (end - start) + 1;

                // Call the appropriate flavor of string.IndexOf().
                if (c != null)
                {
                    pos = fileContents.IndexOf(c.Value, start, count);
                }
                else
                {
                    pos = fileContents.IndexOf(s, start, count);
                }

                if (pos != -1)
                {
                    // We found an instance of it.
                    // Make sure it's not in an ignored span and return it. Otherwise, keep searching for another instance.
                    if (IgnoredSpans.Contains(pos, valueLength, excludeTags) == false)
                    {
                        if (c == null && matchWholeWord)
                        {
                            // If we're looking for a string and want to match the whole word, verify that neither the preceding
                            // or succeeding characters are alphanumeric.
                            char prevChar = pos > 0 ? fileContents[pos - 1] : ' ';
                            char nextChar = (pos + valueLength <= endOfFile) ? fileContents[pos + valueLength] : ' ';
                            if (char.IsLetterOrDigit(prevChar) == false && char.IsLetterOrDigit(nextChar) == false)
                            {
                                return pos;
                            }
                        }
                        else
                        {
                            return pos;
                        }
                    }

                    start = pos + valueLength;
                }
                else
                {
                    break;
                }
            }
            while (start <= end);

            return -1;
        }

        /// <summary>
        /// A version of LastIndexOf that returns the last index of the given character or string within the file,
        /// but only if that character or string does not reside within an ignored span (e.g. comment or string literal).
        /// </summary>
        /// <param name="value">The character or string to search for. Must be of type "char" or "string".</param>
        /// <param name="startIndex">The position to start searching at. Omit to start at the end of the file.</param>
        /// <param name="count">How many characters to examine, backwards from startIndex. Omit to examine to the start of the file.</param>
        /// <param name="matchWholeWord">Set to true if you want to match on a whole word (only applicable for string values).</param>
        /// <param name="searchStringLiterals">Set to true if you want to include string literals (which are ignored by default) in the search.</param>
        /// <returns>The starting position of the last occurrence of the string in the file, if found. Otherwise, -1.</returns>
        protected virtual int LastIndexOf(object value, int startIndex = -1, int count = -1, bool matchWholeWord = false, bool searchStringLiterals = false)
        {
            int valueLength = 1;
            char? c = null;
            string s = string.Empty;

            // Determine if the given value is a char or a string.
            // string.LastIndexOf(char) is faster than string.LastIndexOf(string) so we should try to use it whenever possible.
            if (value is char @char)
            {
                c = @char;
            }
            else if (value is string @string)
            {
                s = @string;
                valueLength = s.Length;

                // If it's a single-character string, optimize by making it a char.
                if (s.Length == 1)
                {
                    c = s[0];
                }
            }
            else
            {
                throw new ArgumentException($"value is a {value.GetType()}, it must be a char or a string");
            }

            // EndOfFile is determined at runtime so we can't specify it as a default value of startIndex.
            if (startIndex == -1)
            {
                startIndex = endOfFile;
            }

            // Make sure the given count doesn't go past the start of the file.
            if (count == -1 || (startIndex - count) + 1 < 0)
            {
                count = startIndex + 1;
            }

            // Because we're searching *backwards*, end is *before* start.
            int start = startIndex;
            int end = (start - count) + 1;
            do
            {
                int pos;
                count = (start - end) + 1;

                // Call the appropriate flavor of string.LastIndexOf().
                if (c != null)
                {
                    pos = fileContents.LastIndexOf(c.Value, start, count);
                }
                else
                {
                    pos = fileContents.LastIndexOf(s, start, count);
                }

                if (pos != -1)
                {
                    // We found an instance of it.

                    // If we should search within string literals then make sure we ignore any ignored spans that are a string literal.
                    List<FileSpan.FileSpanTag> excludeTags = null;
                    if (searchStringLiterals)
                    {
                        excludeTags = new List<FileSpan.FileSpanTag> { FileSpan.FileSpanTag.StringLiteral };
                    }

                    // Make sure it's not in an ignored span and return it. Otherwise, keep searching for another instance.
                    if (IgnoredSpans.Contains(pos, valueLength, excludeTags) == false)
                    {
                        if (c == null && matchWholeWord)
                        {
                            // If we're looking for a string and want to match the whole word, verify that neither the preceding
                            // or succeeding characters are alphanumeric.
                            char beforeChar = pos > 0 ? fileContents[pos - 1] : ' ';
                            char afterChar = (pos + valueLength <= endOfFile) ? fileContents[pos + valueLength] : ' ';
                            if (char.IsLetterOrDigit(beforeChar) == false && char.IsLetterOrDigit(afterChar) == false)
                            {
                                return pos;
                            }
                        }
                        else
                        {
                            return pos;
                        }
                    }

                    // Start searching again, from just before this instance.
                    start = pos - 1;
                }
                else
                {
                    break;
                }
            }
            while (start >= end);

            return -1;
        }

        /// <summary>
        /// Returns a substring from the file, excluding ignored regions.
        /// E.g. Asking for the substring "var foo = /* 42 */ GetFooNumber();" will return "var foo =  GetFooNumber();"
        /// The substring will be taken according to <paramref name="startIndex"/> and <paramref name="length"/>, but the actual
        /// length of the returned string may be shorter if it overlaps with any ignored regions.
        /// </summary>
        /// <param name="startIndex">The starting character position of the desired substring.</param>
        /// <param name="length">The length of the substring in characters. Optional. If omitted, the substring will go to the end of the file.</param>
        /// <returns>The substring of a file's content.</returns>
        internal virtual string Substring(int startIndex, int length = -1)
        {
            // Make sure the given length doesn't exceed the end of the file.
            if (length == -1 || (startIndex + length) - 1 > endOfFile)
            {
                length = (endOfFile - startIndex) + 1;
            }

            int endIndex = startIndex + length - 1;

            // See if the starting index is contained within an ignored span.
            // If so, adjust it to start after the ignored span.
            FileSpan ignoredSpan = IgnoredSpans.GetContainingSpan(startIndex);
            if (ignoredSpan != null)
            {
                startIndex = ignoredSpan.End + 1;
                length = (endIndex - startIndex) + 1;

                // If the start index is now past the end index (length is not positive)
                // then the entire desired substring is contained within an ignored region
                // and we should just return an empty string.
                if (length <= 0)
                {
                    return string.Empty;
                }
            }

            // Get the next ignored span after the starting index.
            ignoredSpan = IgnoredSpans.GetNextSpan(startIndex);

            // If there is no ignored span after the starting index or
            // if the ignored span is after the end index we can simply
            // return the substring as requested.
            if (ignoredSpan == null || ignoredSpan.Start > endIndex)
            {
                return fileContents.Substring(startIndex, length);
            }

            // At this point we know we have to work around an ignored span.
            // Copy over characters that aren't in an ignored span.

            var str = new StringBuilder();

            for (int i = startIndex; i <= endIndex; i++)
            {
                if (ignoredSpan == null || ignoredSpan.Contains(i) == false)
                {
                    // If this character isn't in the ignored span, append it.
                    str.Append(fileContents[i]);
                }
                else if (i == ignoredSpan.End)
                {
                    // If we've reached the end of the current ignored span, get the next one.
                    ignoredSpan = IgnoredSpans.GetNextSpan(i + 1);
                }
            }

            return str.ToString();
        }

        /// <summary>
        /// Returns true if the given position is at the location of a string literal.
        /// </summary>
        /// <param name="pos">The position beeing seeked.</param>
        /// <returns>True if the position is at a string literal.</returns>
        public bool IsStringLiteral(int pos)
        {
            FileSpan span = IgnoredSpans.GetContainingSpan(pos);
            if (span != null && span.Tag == FileSpan.FileSpanTag.StringLiteral)
            {
                return true;
            }

            return false;
        }
    }
}

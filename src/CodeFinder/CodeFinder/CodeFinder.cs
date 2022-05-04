// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeFinder.Finders;
using Microsoft.CodeFinder.Finders.CStyle;

namespace Microsoft.CodeFinder
{
    public enum Language
    {
        Unknown,
        C,
        Cpp,
        CSharp
    }

    /// <summary>
    /// This class finds text within a code file.
    /// See <see cref="FindMatches(MatchQuery)"/>, <see cref="MatchQuery"/>, and <see cref="MatchResult"/> for more information.
    /// </summary>
    public class CodeFinder
    {
        private CodeFinderBase finder;

        /// <summary>
        /// Constructs a CodeFinder object for the given file (and optionally-provided file contents).
        /// </summary>
        /// <param name="filePath">The full path to the code file to be searched.</param>
        /// <param name="fileContents">Optional. If the caller already has a buffer of the file contents they should provide it here.
        /// Otherwise, we'll use the filePath to load the contents.</param>
        public CodeFinder(string filePath, string fileContents = "")
        {
            if (string.IsNullOrWhiteSpace(fileContents))
            {
                fileContents = File.ReadAllText(filePath);
            }

            Initialize(filePath, fileContents);
        }

        /// <summary>
        /// Returns the language associated with the given file path or file extension.
        /// </summary>
        /// <param name="filePathOrExtension">Either the path to specific file or a file extension, starting with a "."</param>
        /// <returns></returns>
        private static Language GetLanguage(string filePathOrExtension)
        {
            string ext = filePathOrExtension;
            if (string.IsNullOrEmpty(ext) == false)
            {
                if (filePathOrExtension.StartsWith(".") == false)
                {
                    ext = Path.GetExtension(ext);
                }
            }

            switch (ext)
            {
                case ".cs":
                    return Language.CSharp;

                case ".c":
                case ".h":
                    return Language.C;

                case ".cpp":
                case ".cxx":
                case ".cc":
                case ".hpp":
                case ".hxx":
                    return Language.Cpp;

                default:
                    return Language.Unknown;
            }
        }

        /// <summary>
        /// Initializes this object by instantiating the appropriate "finder" based on the given file type.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileContents"></param>
        private void Initialize(string filePath, string fileContents)
        {
            Language language = GetLanguage(filePath);
            switch (language)
            {
                case Language.CSharp:
                    finder = new CSharpFinder(fileContents);
                    break;

                case Language.C:
                case Language.Cpp:
                    finder = new CppFinder(fileContents);
                    break;

                // All other languages use the default finder.
                default:
                    finder = new DefaultFinder(fileContents);
                    break;
            }
        }

        /// <summary>
        /// Finds matches for all the given queries.
        /// Callers can use <see cref="MatchResult.Id"/> and <see cref="MatchQuery.Id"/> to correlate matches with queries.
        /// </summary>
        /// <param name="queries"></param>
        /// <returns></returns>
        public List<MatchResult> FindMatches(List<MatchQuery> queries)
        {
            var matches = new List<MatchResult>();
            foreach (MatchQuery query in queries)
            {
                matches.AddRange(FindMatches(query));
            }

            // Order matches by ID, then line number.
            matches = matches.OrderBy(m => m.Id).ThenBy(m => m.LineNumber).ToList();

            return matches;
        }

        /// <summary>
        /// Finds matches for the given query.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public List<MatchResult> FindMatches(MatchQuery query)
        {
            List<MatchResult> matches = finder.FindMatches(query);

            // Order matches by line number.
            matches = matches.OrderBy(m => m.LineNumber).ToList();

            return matches;
        }

        /// <summary>
        /// Finds matches for the given query.
        /// When a function signature is provided, this uses a different strategy from <see cref="FindMatches(List{MatchQuery})"/>
        /// that is more likely to return a match in certain cases.
        /// </summary>
        /// <param name="queries"></param>
        /// <returns></returns>
        public List<MatchResult> FindMatches2(List<MatchQuery> queries)
        {
            var matches = new List<MatchResult>();
            foreach (MatchQuery query in queries)
            {
                matches.AddRange(FindMatches2(query));
            }

            // Order matches by ID, then line number.
            matches = matches.OrderBy(m => m.Id).ThenBy(m => m.LineNumber).ToList();

            return matches;
        }

        /// <summary>
        /// Finds matches for the given query.
        /// When a function signature is provided, this uses a different strategy from <see cref="FindMatches(MatchQuery)""/>
        /// that is more likely to return a match in certain cases.
        /// </summary>
        /// <param name="queries"></param>
        /// <returns></returns>
        public List<MatchResult> FindMatches2(MatchQuery query)
        {
            List<MatchResult> matches = finder.FindMatches2(query);

            // Order matches by line number.
            matches = matches.OrderBy(m => m.LineNumber).ToList();

            return matches;
        }
    }
}

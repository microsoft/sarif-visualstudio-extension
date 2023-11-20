// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;

namespace Microsoft.Sarif.Viewer.CodeFinding
{
    /// <summary>
    /// Represents a query to find a given piece of text.
    /// </summary>
    public class MatchQuery
    {
        /// <summary>
        /// Gets a user-provided ID to uniquely identify the query. This can be used to correlate the query with any results.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets or sets the text to search for.
        /// </summary>
        public string TextToFind { get; set; }

        /// <summary>
        /// Gets optional function signature.
        /// If provided, the search for the given text will be limited to the function's scope.
        /// This can be as simple as the name of the function, but, depending on the language
        /// of the code file, it may also include namespace, class, etc.
        /// E.g. A C++ function signature may be: "Namespace::Class::Method"
        /// C# function signature may be: "Namespace::Class.Method".
        /// </summary>
        public string FunctionSignature { get; }

        /// <summary>
        /// Gets a hint as to the (1-indexed) line number where the given text may be found.
        /// This may be ignored if a function signature is provided.
        /// This may be used when calculating the confidence score. I.e. the further away the
        /// text is from this line the less confident the match result.
        /// </summary>
        public int LineNumberHint { get; }

        /// <summary>
        /// This is a hint that indicates the expected characterization of <see cref="TextToFind"/>.
        /// In most cases, the caller of <see cref="MatchQuery"/> will want to specify <see cref="Code"/> (which is the default).
        /// However, if the caller knows that <see cref="TextToFind"/> is a function definition (or they want to find the function definition)
        /// then they should specify <see cref="Function"/>.
        /// </summary>
        public enum MatchTypeHint
        {
            /// <summary>
            /// Indicates that what <see cref="TextToFind"/> represents is unknown.
            /// </summary>
            Unknown,

            /// <summary>
            /// Indicates that <see cref="TextToFind"/> represents code that resides within a function.
            /// </summary>
            Code,

            /// <summary>
            /// Indicates that <see cref="TextToFind"/> represents a function definition.
            /// </summary>
            Function,

            /// <summary>
            /// Indicates that <see cref="TextToFind"/> represents a class definition.
            /// </summary>
            Class,
        }

        /// <summary>
        /// Gets a hint for the type of code that <see cref="TextToFind"/> represents.
        /// </summary>
        public MatchTypeHint TypeHint { get; }

        /// <summary>
        /// Gets a value indicating whether only whole tokens will be matched. Not supported by <see cref="CodeFinder.FindMatches(MatchQuery)"/>.
        /// </summary>
        public bool MatchWholeTokens { get; }

        /// <summary>
        /// Gets the number of lines we are searching for with this query.
        /// </summary>
        public int LineNumbers
        {
            get
            {
                if (this.TextToFind.Contains("\r\n"))
                {
                    return this.TextToFind.Split(new string[] { "\r\n" }, System.StringSplitOptions.None).Count();
                }
                else if (this.TextToFind.Contains("\r"))
                {
                    return this.TextToFind.Split('\r').Count();
                }
                else
                {
                    return this.TextToFind.Split('\n').Count();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MatchQuery"/> class.
        /// Creates a MatchQuery object.
        /// </summary>
        /// <param name="textToFind">Required. The string to search for within the code provided to CodeFinder.</param>
        /// <param name="lineNumberHint">Optional. A hint at the (1-indexed) line at which the text may likely be found.</param>
        /// <param name="callingSignature">Optional. The signature of the function/scope that contains the text to find.</param>
        /// <param name="id">Optional. A caller-defined ID that the caller can use to correlate a query with a result.</param>
        /// <param name="typeHint">Optional. A hint characterizing the text to find.</param>
        /// <param name="matchWholeTokens">Optional, defaults to true. If true, ensures any matches of <paramref name="textToFind"/> match on whole tokens.
        /// E.g. "MyClass" will match within "MyClass->DoThing()" but not "MyClassExtended->DoThing()".</param>
        public MatchQuery(string textToFind, int lineNumberHint = 1, string callingSignature = "", string id = "", MatchTypeHint typeHint = MatchTypeHint.Code, bool matchWholeTokens = true)
        {
            Id = id;
            TextToFind = textToFind;
            FunctionSignature = callingSignature;
            LineNumberHint = lineNumberHint;
            TypeHint = typeHint;
            MatchWholeTokens = matchWholeTokens;
        }

        /// <summary>
        /// Changes the line endings in the text to find to make matching simpler. Automatically tries to detect the line endings being used in the text.
        /// </summary>
        /// <param name="newLineEndings">The new line endings that we will use.</param>
        public void ChangeLineEndings(string newLineEndings)
        {
            if (this.TextToFind.Contains("\r\n"))
            {
                this.TextToFind = this.TextToFind.Replace("\r\n", newLineEndings);
            }
            else if (this.TextToFind.Contains("\r"))
            {
                this.TextToFind = this.TextToFind.Replace("\r", newLineEndings);
            }
            else
            {
                this.TextToFind = this.TextToFind.Replace("\n", newLineEndings);
            }
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Text;

namespace Microsoft.Sarif.Viewer.CodeFinder.Internal
{
    /// <summary>
    /// This static class contains extension methods for the "string" class.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Truncates the given string <paramref name="str"/>. If <paramref name="maxLength"/> is greater than <paramref name="str"/>'s length then it is returned as-is.
        /// </summary>
        /// <param name="str">The string to truncate.</param>
        /// <param name="maxLength">The maximum number of characters to return.</param>
        /// <returns>The string, truncated up to <paramref name="maxLength"/> characters.</returns>
        public static string Truncate(this string str, int maxLength)
        {
            if (string.IsNullOrEmpty(str) == false && str.Length > maxLength)
            {
                return str.Substring(0, maxLength);
            }

            return str;
        }

        /// <summary>
        /// For the given string, removes everything between matching pairs of <paramref name="startCharacter"/> and <paramref name="endCharacter"/>.
        /// If <paramref name="inclusive"/> is true, <paramref name="startCharacter"/> and <paramref name="endCharacter"/> are removed.
        /// Any nested pairs of <paramref name="startCharacter"/> and <paramref name="endCharacter"/> are removed, irrespective of the value of <paramref name="inclusive"/>.
        /// If <paramref name="startCharacter"/> is present in the string but <paramref name="endCharacter"/> is not, and vice versa, nothing will be removed.
        /// </summary>
        /// <param name="str">Target string.</param>
        /// <param name="startCharacter">Start character to match for removal range.</param>
        /// <param name="endCharacter">End character to match for removal range.</param>
        /// <param name="inclusive">Optional, defaults to true. If false, <paramref name="startCharacter"/> and <paramref name="endCharacter"/> will not be removed.</param>
        /// <returns>New string that has content removed with.</returns>
        public static string RemoveBetween(this string str, char startCharacter, char endCharacter, bool inclusive = true)
        {
            // If the given string doesn't contain any instances of either the start or end character then just return the original string.
            if (str.Contains(startCharacter) == false || str.Contains(endCharacter) == false)
            {
                return str;
            }

            var newStr = new StringBuilder();

            int level = 0;
            for (int i = 0; i < str.Length; i++)
            {
                int c = str[i];
                bool append = true;

                if (level == 0)
                {
                    if (c == startCharacter)
                    {
                        level++;

                        // If the removal is not inclusive append this instance of the start character to the new string.
                        append = !inclusive;
                    }
                }
                else
                {
                    // We're in a region of the string to be removed so by default don't append any characters to the new string.
                    append = false;

                    if (c == startCharacter)
                    {
                        level++;
                    }
                    else if (c == endCharacter)
                    {
                        level--;

                        // If this is the final matching end character and removal is not inclusive, append this instance of the end character to the new string.
                        if (level == 0 && inclusive == false)
                        {
                            append = true;
                        }
                    }
                }

                if (append)
                {
                    newStr.Append(c);
                }
            }

            return newStr.ToString();
        }

        /// <summary>
        /// Version of Contains that allows for ignoring case.
        /// </summary>
        /// <param name="source">The source string.</param>
        /// <param name="value">The value to look for in the source string.</param>
        /// <param name="comparisonType">One of the <see cref="StringComparison"/> values that specifies the rules for the search.</param>
        /// <returns>True if <paramref name="value"/> is found within <paramref name="source"/>.</returns>
        public static bool Contains(this string source, string value, StringComparison comparisonType)
        {
            return source?.IndexOf(value, comparisonType) >= 0;
        }
    }
}

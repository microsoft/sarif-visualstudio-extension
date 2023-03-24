// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Sarif.Viewer.CodeFinder.Internal
{
    /// <summary>
    /// Provides extension methods for <see cref="List{T}"/>.
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// Compares two lists (using the given type's Equals() method) and returns a value indicating how they match.
        /// </summary>
        /// <typeparam name="T">The type that the lists hold.</typeparam>
        /// <param name="list1">The first lit to compare.</param>
        /// <param name="list2">The second list to compare.</param>
        /// <returns>
        /// 0 indicates an exact match (both lists are the same length and all elements match).
        /// A positive value indicates all elements in the second list match the first but the first has more elements than the second.
        /// A negative value indicates all elements in the first list match the second but the first has fewer elements than the second.
        /// A null value indicates at least one element (at the same valid index in both lists) does not match.
        /// </returns>
        public static int? Compare<T>(this List<T> list1, List<T> list2)
        {
            int result = 0;

            int length = Math.Max(list1.Count, list2.Count);

            for (int i = 0; i < length; i++)
            {
                if (i < list1.Count && i < list2.Count)
                {
                    if (list1[i].Equals(list2[i]) == false)
                    {
                        // If at any point the elements with the same index don't match, then this is not a match.
                        return null;
                    }
                }
                else if (i < list1.Count && i >= list2.Count)
                {
                    // Increment the return value to indicate the first list contains more elements than the second list.
                    result++;
                }
                else if (i >= list1.Count && i < list2.Count)
                {
                    // Decrement the return value to indicate the first list contains fewer elements than the second list.
                    result--;
                }
            }

            return result;
        }
    }
}

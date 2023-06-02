// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Sarif.Viewer.CodeFinding
{
    /// <summary>
    /// Represents a section of a file, defined as [Start, End] character indices.
    /// It can define a portion of a line, multiple lines, or even the entirety of a file.
    /// This class is immutable.
    /// </summary>
    public class FileSpan
    {
        /// <summary>
        /// Gets the starting position of the span.
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// Gets the ending position of the span.
        /// </summary>
        public int End { get; }

        /// <summary>
        /// Gets the length, in characters, of the span.
        /// </summary>
        public int Length { get => (End - Start) + 1; }

        /// <summary>
        /// Gets unique identifier for this span.
        /// </summary>
        public string Id { get => $"{Start}_{End}"; }

        public enum FileSpanTag
        {
            /// <summary>
            /// Indicates this FileSpan has no particular significance.
            /// </summary>
            None = 0,

            /// <summary>
            /// Indicates this FileSpan represents a code comment.
            /// </summary>
            Comment,

            /// <summary>
            /// Indicates this FileSpan represents a string literal, e.g. "this is a string".
            /// </summary>
            StringLiteral,

            /// <summary>
            /// Indicates this FileSpan represents a character literal, e.g. 'c'.
            /// </summary>
            CharLiteral,
        }

        /// <summary>
        /// Gets characteristics of the file contents defined by this span.
        /// </summary>
        public FileSpanTag Tag { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSpan"/> class.
        /// Constructs a new span of the range [start, end].
        /// </summary>
        /// <param name="start">The starting position of the span.</param>
        /// <param name="end">The ending position of the span, inclusive.</param>
        /// <param name="tag">An optional tag for the span.</param>
        public FileSpan(int start, int end, FileSpanTag tag = FileSpanTag.None)
        {
            if (start > end)
            {
                throw new ArgumentException("start must be less than or equal to end");
            }
            else if (start < 0 || end < 0)
            {
                throw new ArgumentException("start and end must be non-negative integers");
            }

            Start = start;
            End = end;
            Tag = tag;
        }

        /// <summary>
        /// Returns true if this span is equivalent to the given span.
        /// </summary>
        /// <param name="span">The span being compared to.</param>
        /// <returns>True if the FileSpans are equivalent.</returns>
        public bool Equals(FileSpan span)
        {
            if (Start == span.Start && End == span.End)
            {
                return true;
            }

            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj is FileSpan span)
            {
                return Equals(span);
            }

            return false;
        }

        /// <summary>
        /// Returns true if this FileSpan intersects, in any way, with the given FileSpan.
        /// </summary>
        /// <param name="otherSpan">The other <see cref="FileSpan"/> being compared with.</param>
        /// <returns>Whether it intersects with the other file span.</returns>
        public bool Intersects(FileSpan otherSpan)
        {
            if (otherSpan.Start <= Start && Start <= otherSpan.End)
            {
                // The start of this span is within the other span.
                return true;
            }
            else if (otherSpan.Start <= End && End <= otherSpan.End)
            {
                // The end of this span is within the other span.
                return true;
            }
            else if (Contains(otherSpan))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if this FileSpan completely contains the given FileSpan.
        /// Note that if both spans are equal then this will also return true.
        /// </summary>
        /// <param name="otherSpan">The FileSpan we are seeing if we are a superset of.</param>
        /// <returns>Whether we contain the other FileSpan.</returns>
        public bool Contains(FileSpan otherSpan)
        {
            if (Start <= otherSpan.Start && otherSpan.End <= End)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the given position falls within this FileSpan.
        /// </summary>
        /// <param name="position">The line number being checked.</param>
        /// <returns>True if the provided line number was contained within this FileSpan.</returns>
        public bool Contains(int position)
        {
            if (Start <= position && position <= End)
            {
                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            int hashCode = -509415362;
            int hashFactor = -1521134295;

            hashCode = (hashCode * hashFactor) + Start.GetHashCode();
            hashCode = (hashCode * hashFactor) + End.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    /// Comparer for FileSpan objects. Objects are sorted according to their Start member, in ascending order.
    /// </summary>
    internal class FileSpanComparer : IComparer<FileSpan>
    {
        public int Compare(FileSpan s1, FileSpan s2)
        {
            if (s1.Start > s2.Start)
            {
                return 1;
            }
            else if (s1.Start < s2.Start)
            {
                return -1;
            }

            return 0;
        }
    }

    /// <summary>
    /// Comparer for FileSpan objects, that sorts them according to their End member, in *descending* order.
    /// </summary>
    internal class FileSpanReverseEndComparer : IComparer<FileSpan>
    {
        public int Compare(FileSpan s1, FileSpan s2)
        {
            if (s1.End > s2.End)
            {
                return -1;
            }
            else if (s1.End < s2.End)
            {
                return 1;
            }

            return 0;
        }
    }
}

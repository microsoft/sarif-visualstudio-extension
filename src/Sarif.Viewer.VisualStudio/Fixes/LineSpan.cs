// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Sarif.Viewer.Fixes
{
    using System;

    // This code was taken from roslyn and is used to create diff views for lightbulb previews.
    internal struct LineSpan : IEquatable<LineSpan>
    {
        public int Start { get; }

        public int End { get; }

        public LineSpan(int start, int end)
        {
            this.Start = start;
            this.End = end;
        }

        public override bool Equals(object obj)
        {
            return obj is LineSpan span && this.Equals(span);
        }

        public bool Equals(LineSpan other)
        {
            return this.Start == other.Start &&
                   this.End == other.End;
        }

        public override int GetHashCode()
        {
            var hashCode = -1676728671;
            hashCode = (hashCode * -1521134295) + this.Start.GetHashCode();
            hashCode = (hashCode * -1521134295) + this.End.GetHashCode();
            return hashCode;
        }
    }
}

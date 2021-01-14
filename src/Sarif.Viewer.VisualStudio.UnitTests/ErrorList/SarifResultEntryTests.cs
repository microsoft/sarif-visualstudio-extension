// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using FluentAssertions;

using Microsoft.Sarif.Viewer.ErrorList;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class SarifResultEntryTests
    {
        [Fact]
        public void SarifSnapshot_TryGetValue_LineNumber()
        {
            const int lineNumber = 10;

            var errorItem = new SarifErrorListItem
            {
                LineNumber = lineNumber,
            };

            var tableEntry = new SarifResultTableEntry(errorItem);

            object value;
            tableEntry.TryGetValue("line", out value).Should().Be(true);
            value.Should().Be(lineNumber - 1);
        }

        [Fact]
        public void SarifSnapshot_TryGetValue_NoLineNumber()
        {
            var errorItem = new SarifErrorListItem();

            var tableEntry = new SarifResultTableEntry(errorItem);

            object value;
            tableEntry.TryGetValue("line", out value).Should().Be(true);
            value.Should().Be(-1);
        }

        [Fact]
        public void SarifSnapshot_TryGetValue_NegativeLineNumber()
        {
            const int lineNumber = -10;

            var errorItem = new SarifErrorListItem
            {
                LineNumber = lineNumber,
            };

            var tableEntry = new SarifResultTableEntry(errorItem);

            object value;
            tableEntry.TryGetValue("line", out value).Should().Be(true);
            value.Should().Be(lineNumber - 1);
        }
    }
}

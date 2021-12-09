// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Windows.Documents;

using FluentAssertions;

using Microsoft.Sarif.Viewer.ErrorList;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class SarifResultEntryTests
    {
        public SarifResultEntryTests()
        {
            SarifViewerPackage.IsUnitTesting = true;
        }

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

        [Fact]
        public void SarifResultEntry_TryGetValue_TextWithMarkdownHyperlinks()
        {
            string messageText = "Sample [text](0) with hyperlink to [github](https://github.com).";
            var errorItem = new SarifErrorListItem
            {
                RawMessage = messageText,
                Message = messageText,
                ShortMessage = messageText
            };

            var tableEntry = new SarifResultTableEntry(errorItem);

            tableEntry.TryGetValue("textinlines", out object inlines).Should().Be(true);
            List<Inline> inlineList = inlines as List<Inline>;
            inlineList.Should().NotBeNull();
            inlineList.Count.Should().Be(5);
            inlineList[1].GetType().Should().Be(typeof(Hyperlink));
            inlineList[3].GetType().Should().Be(typeof(Hyperlink));

            tableEntry.TryGetValue("text", out object text).Should().Be(true);
            ((string)text).Should().BeEquivalentTo(messageText);

            tableEntry.TryGetValue("fullText", out object fullText).Should().Be(true);
            ((string)fullText).Should().BeNull();
        }

        [Fact]
        public void SarifResultEntry_TryGetValue_PlainText()
        {
            string messageText = "Plain text result message.";
            var errorItem = new SarifErrorListItem
            {
                RawMessage = messageText,
                Message = messageText,
                ShortMessage = messageText
            };

            var tableEntry = new SarifResultTableEntry(errorItem);

            tableEntry.TryGetValue("textinlines", out object inlines).Should().Be(true);
            inlines.Should().BeNull();

            tableEntry.TryGetValue("text", out object text).Should().Be(true);
            ((string)text).Should().BeEquivalentTo(messageText);

            tableEntry.TryGetValue("fullText", out object fullText).Should().Be(true);
            fullText.Should().BeNull();
        }
    }
}

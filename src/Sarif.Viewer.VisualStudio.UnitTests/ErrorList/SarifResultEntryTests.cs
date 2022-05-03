// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Web.ModelBinding;
using System.Windows.Documents;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TextManager.Interop;

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
            string messageText = "Sample [text](0) with hyperlink to [example web site](https://example.com).";
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

        [Fact]
        public void SarifResultEntry_MessagesTests()
        {
            string ellipsis = "\u2026";
            var testCases = new[]
            {
                new
                {
                    MaxTextLength = 40,
                    RawMessage = "",
                    ShortMessage = "",
                    FullMessage = "",
                    ContainsHyperlink = false,
                    ShortPlainText = (string)null,
                    FullPlainText = (string)null,
                },
                new
                {
                    MaxTextLength = 40,
                    RawMessage = "   ",
                    ShortMessage = "",
                    FullMessage = "",
                    ContainsHyperlink = false,
                    ShortPlainText = (string)null,
                    FullPlainText = (string)null,
                },
                new
                {
                    MaxTextLength = 40,
                    RawMessage = "A short sentence",
                    ShortMessage = "A short sentence.",
                    FullMessage = "A short sentence.",
                    ContainsHyperlink = false,
                    ShortPlainText = (string)null,
                    FullPlainText = (string)null,
                },
                new
                {
                    MaxTextLength = 40,
                    RawMessage = "A short sentence.",
                    ShortMessage = "A short sentence.",
                    FullMessage = "A short sentence.",
                    ContainsHyperlink = false,
                    ShortPlainText = (string)null,
                    FullPlainText = (string)null,
                },
                new
                {
                    MaxTextLength = 40,
                    RawMessage = "A short sentence!",
                    ShortMessage = "A short sentence!",
                    FullMessage = "A short sentence!",
                    ContainsHyperlink = false,
                    ShortPlainText = (string)null,
                    FullPlainText = (string)null,
                },
                new
                {
                    MaxTextLength = 15,
                    RawMessage = "A short sentence",
                    ShortMessage = "A short sentenc" + ellipsis,
                    FullMessage = "A short sentence.",
                    ContainsHyperlink = false,
                    ShortPlainText = (string)null,
                    FullPlainText = (string)null,
                },
                new
                {
                    MaxTextLength = 16,
                    RawMessage = "A short sentence",
                    ShortMessage = "A short sentence" + ellipsis,
                    FullMessage = "A short sentence.",
                    ContainsHyperlink = false,
                    ShortPlainText = (string)null,
                    FullPlainText = (string)null,
                },
                new
                {
                    MaxTextLength = 17,
                    RawMessage = "A short sentence",
                    ShortMessage = "A short sentence.",
                    FullMessage = "A short sentence.",
                    ContainsHyperlink = false,
                    ShortPlainText = (string)null,
                    FullPlainText = (string)null,
                },
                new
                {
                    MaxTextLength = 100,
                    RawMessage = "A short sentence? The second sentence",
                    ShortMessage = "A short sentence?",
                    FullMessage = "A short sentence? The second sentence.",
                    ContainsHyperlink = false,
                    ShortPlainText = (string)null,
                    FullPlainText = (string)null,
                },
                new
                {
                    MaxTextLength = 100,
                    RawMessage = "A short sentence\r\n The second sentence",
                    ShortMessage = "A short sentence.",
                    FullMessage = "A short sentence\r\n The second sentence.",
                    ContainsHyperlink = false,
                    ShortPlainText = (string)null,
                    FullPlainText = (string)null,
                },
                new
                {
                    MaxTextLength = 18,
                    RawMessage = "The [quick brown fox](https://www.quickfox.com) jumps over the [lazy dog](http://lazy.dog.com).",
                    ShortMessage = "The quick brown fo" + ellipsis,
                    FullMessage = "The [quick brown fox](https://www.quickfox.com) jumps over the [lazy dog](http://lazy.dog.com).",
                    ContainsHyperlink = true,
                    ShortPlainText = (string)null,
                    FullPlainText = "The quick brown fox jumps over the lazy dog.",
                },
                new
                {
                    MaxTextLength = 19,
                    RawMessage = "The [quick brown fox](https://www.quickfox.com) jumps over the [lazy dog](http://lazy.dog.com).",
                    ShortMessage = "The [quick brown fox](https://www.quickfox.com)" + ellipsis,
                    FullMessage = "The [quick brown fox](https://www.quickfox.com) jumps over the [lazy dog](http://lazy.dog.com).",
                    ContainsHyperlink = true,
                    ShortPlainText = "The quick brown fox" + ellipsis,
                    FullPlainText = "The quick brown fox jumps over the lazy dog.",
                },
                new
                {
                    MaxTextLength = 20,
                    RawMessage = "The [quick brown fox](https://www.quickfox.com) jumps over the [lazy dog](http://lazy.dog.com).",
                    ShortMessage = "The [quick brown fox](https://www.quickfox.com) " + ellipsis,
                    FullMessage = "The [quick brown fox](https://www.quickfox.com) jumps over the [lazy dog](http://lazy.dog.com).",
                    ContainsHyperlink = true,
                    ShortPlainText = "The quick brown fox " + ellipsis,
                    FullPlainText = "The quick brown fox jumps over the lazy dog.",
                },
                new
                {
                    MaxTextLength = 21,
                    RawMessage = "The [quick brown fox](https://www.quickfox.com) jumps over the [lazy dog](http://lazy.dog.com).",
                    ShortMessage = "The [quick brown fox](https://www.quickfox.com) j" + ellipsis,
                    FullMessage = "The [quick brown fox](https://www.quickfox.com) jumps over the [lazy dog](http://lazy.dog.com).",
                    ContainsHyperlink = true,
                    ShortPlainText = "The quick brown fox j" + ellipsis,
                    FullPlainText = "The quick brown fox jumps over the lazy dog.",
                },
                new
                {
                    MaxTextLength = 42,
                    RawMessage = "The [quick brown fox](https://www.quickfox.com) jumps over the [lazy dog](http://lazy.dog.com).",
                    ShortMessage = "The [quick brown fox](https://www.quickfox.com) jumps over the lazy do" + ellipsis,
                    FullMessage = "The [quick brown fox](https://www.quickfox.com) jumps over the [lazy dog](http://lazy.dog.com).",
                    ContainsHyperlink = true,
                    ShortPlainText = "The quick brown fox jumps over the lazy do" + ellipsis,
                    FullPlainText = "The quick brown fox jumps over the lazy dog.",
                },
                new
                {
                    MaxTextLength = 43,
                    RawMessage = "The [quick brown fox](https://www.quickfox.com) jumps over the [lazy dog](http://lazy.dog.com).",
                    ShortMessage = "The [quick brown fox](https://www.quickfox.com) jumps over the [lazy dog](http://lazy.dog.com)" + ellipsis,
                    FullMessage = "The [quick brown fox](https://www.quickfox.com) jumps over the [lazy dog](http://lazy.dog.com).",
                    ContainsHyperlink = true,
                    ShortPlainText = "The quick brown fox jumps over the lazy dog" + ellipsis,
                    FullPlainText = "The quick brown fox jumps over the lazy dog.",
                },
                new
                {
                    MaxTextLength = 44,
                    RawMessage = "The [quick brown fox](https://www.quickfox.com) jumps over the [lazy dog](http://lazy.dog.com).",
                    ShortMessage = "The [quick brown fox](https://www.quickfox.com) jumps over the [lazy dog](http://lazy.dog.com).",
                    FullMessage = "The [quick brown fox](https://www.quickfox.com) jumps over the [lazy dog](http://lazy.dog.com).",
                    ContainsHyperlink = true,
                    ShortPlainText = "The quick brown fox jumps over the lazy dog.",
                    FullPlainText = "The quick brown fox jumps over the lazy dog.",
                },
                new
                {
                    MaxTextLength = 160,
                    RawMessage = "The [quick brown fox](https://www.quickfox.com) jumps over the [lazy dog](http://lazy.dog.com).",
                    ShortMessage = "The [quick brown fox](https://www.quickfox.com) jumps over the [lazy dog](http://lazy.dog.com).",
                    FullMessage = "The [quick brown fox](https://www.quickfox.com) jumps over the [lazy dog](http://lazy.dog.com).",
                    ContainsHyperlink = true,
                    ShortPlainText = "The quick brown fox jumps over the lazy dog.",
                    FullPlainText = "The quick brown fox jumps over the lazy dog.",
                }

            };

            int originalMax = SarifErrorListItem.MaxConcisedTextLength;
            foreach (var test in testCases)
            {
                SarifErrorListItem.MaxConcisedTextLength = test.MaxTextLength;
                var result = new Result
                {
                    Message = new Message()
                    {
                        Text = test.RawMessage,
                    },
                };
                SarifErrorListItem error = TestUtilities.MakeErrorListItem(result);
                SarifResultTableEntry entry = new SarifResultTableEntry(error);

                VerifyErrorListItemEntry(error, entry, test);
            }
            SarifErrorListItem.MaxConcisedTextLength = originalMax;
        }

        private static void VerifyErrorListItemEntry(SarifErrorListItem error, SarifResultTableEntry entry, dynamic test)
        {
            error.ShortMessage.Should().Be(test.ShortMessage);
            error.Message.Should().Be(test.FullMessage);
            error.RawMessage.Should().Be(test.RawMessage.Trim());
            error.HasEmbeddedLinks.Should().Be(test.ContainsHyperlink);

            entry.TryGetValue(StandardTableKeyNames.Text, out object textColumn).Should().BeTrue();
            entry.TryGetValue(StandardTableKeyNames.FullText, out object fullTextColumn).Should().BeTrue();
            entry.TryGetValue(StandardTableKeyNames2.TextInlines, out object textInlinesColumn).Should().BeTrue();
            entry.TryGetValue(SarifResultTableEntry.FullTextInlinesColumnName, out object fullTextInlinesColumn).Should().BeTrue();

            ((string)textColumn).Should().Be(test.ShortMessage);
            ((string)fullTextColumn).Should().Be(error.HasDetailsContent ? test.FullMessage : null);

            if (test.ContainsHyperlink)
            {
                var textInlines = textInlinesColumn as List<Inline>;
                textInlines.Should().NotBeNull();
                SdkUIUtilities.GetPlainText(textInlines).Should().Be(test.ShortPlainText);

                var fullTextInlines = fullTextInlinesColumn as List<Inline>;
                fullTextInlines.Should().NotBeNull();
                SdkUIUtilities.GetPlainText(fullTextInlines).Should().Be(test.FullPlainText);
            }
            else
            {
                textInlinesColumn.Should().BeNull();
                fullTextInlinesColumn.Should().BeNull();
            }
        }
    }
}

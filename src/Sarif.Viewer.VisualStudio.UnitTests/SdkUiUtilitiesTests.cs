// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using FluentAssertions;
using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class SdkUIUtilitiesTests
    {
        private void Hyperlink_Click(object sender, RoutedEventArgs e) { }

        [Fact]
        public void GetInlinesForErrorMessage_DoesNotCreateLinks()
        {
            string message = @"The quick [brown fox](2) jumps over the lazy dog.";

            var expected = new List<Inline>
            {
                new Run("The quick "),
                new Run("brown fox"),
                new Run(" jumps over the lazy dog.")
            };

            var actual = SdkUIUtilities.GetInlinesForErrorMessage(message);

            actual.Count.Should().Be(expected.Count);

            for (int i = 0; i < actual.Count; i++)
            {
                VerifyTextRun(expected[i], actual[i]);
            }
        }

        [Fact]
        public void GetInlinesForErrorMessage_IgnoresInvalidLink()
        {
            // That is, the fact that the link destination doesn't look like a URL
            // doesn't bother it.
            string message = @"The quick [brown fox](some text) jumps over the lazy dog.";

            var expected = new List<Inline>
            {
                new Run("The quick "),
                new Run("brown fox"),
                new Run(" jumps over the lazy dog.")
            };

            var actual = SdkUIUtilities.GetInlinesForErrorMessage(message);

            actual.Count.Should().Be(expected.Count);

            for (int i = 0; i < actual.Count; i++)
            {
                VerifyTextRun(expected[i], actual[i]);
            }
        }

        [Fact]
        public void GetMessageInlines_DoesNotGenerateLinkForEscapedBrackets()
        {
            string message = @"The quick \[brown fox\] jumps over the lazy dog.";

            // Because there are no embedded links, we shouldn't get anything back
            var actual = SdkUIUtilities.GetMessageInlines(message, index: 1, clickHandler: Hyperlink_Click);

            actual.Count.Should().Be(0);
        }

        [Fact]
        public void GetMessageInlines_RendersOneLink()
        {
            string message = @"The quick [brown fox](1) jumps over the lazy dog.";

            var link = new Hyperlink();
            link.Tag = new Tuple<int, object>(1, 1);
            link.Inlines.Add(new Run("brown fox"));

            var expected = new List<Inline>
            {
                new Run("The quick "),
                link,
                new Run(" jumps over the lazy dog.")
            };

            var actual = SdkUIUtilities.GetMessageInlines(message, index: 1, clickHandler: Hyperlink_Click);

            actual.Count.Should().Be(expected.Count);

            VerifyTextRun(expected[0], actual[0]);
            VerifyHyperlink(expected[1], actual[1]);
            VerifyTextRun(expected[2], actual[2]);
        }

        [Fact]
        public void GetMessageInlines_RendersTwoLinksAndHandlesLinkAtTheEnd()
        {
            string message = @"The quick [brown fox](1) jumps over the [lazy dog](2)";

            var link1 = new Hyperlink();
            link1.Tag = new Tuple<int, object>(1, 1);
            link1.Inlines.Add(new Run("brown fox"));

            var link2 = new Hyperlink();
            link2.Tag = new Tuple<int, int>(1, 2);
            link2.Inlines.Add(new Run("lazy dog"));

            var expected = new List<Inline>
            {
                new Run("The quick "),
                link1,
                new Run(" jumps over the "),
                link2
            };

            var actual = SdkUIUtilities.GetMessageInlines(message, index: 1, clickHandler: Hyperlink_Click);

            actual.Count.Should().Be(expected.Count);

            VerifyTextRun(expected[0], actual[0]);
            VerifyHyperlink(expected[1], actual[1]);
            VerifyTextRun(expected[2], actual[2]);
            VerifyHyperlink(expected[3], actual[3]);
        }

        [Fact]
        public void GetMessageInlines_RendersOneLinkPlusLiteralBrackets()
        {
            string message = @"The quick [brown fox](1) jumps over the \[lazy dog\].";

            var link = new Hyperlink();
            link.Tag = new Tuple<int, object>(1, 1);
            link.Inlines.Add(new Run("brown fox"));

            var expected = new List<Inline>
            {
                new Run("The quick "),
                link,
                new Run(" jumps over the [lazy dog].")
            };

            var actual = SdkUIUtilities.GetMessageInlines(message, index: 1, clickHandler: Hyperlink_Click);

            actual.Count.Should().Be(expected.Count);

            VerifyTextRun(expected[0], actual[0]);
            VerifyHyperlink(expected[1], actual[1]);
            VerifyTextRun(expected[2], actual[2]);
        }

        [Fact]
        public void GetMessageInlines_RendersWebLink()
        {
            string url = "http://example.com";
            string message = $"The quick [brown fox]({url}) jumps over the lazy dog.";

            var link = new Hyperlink();
            link.Tag = new Tuple<int, object>(1, new Uri(url, UriKind.Absolute));
            link.Inlines.Add(new Run("brown fox"));

            var expected = new List<Inline>
            {
                new Run("The quick "),
                link,
                new Run(" jumps over the lazy dog.")
            };

            var actual = SdkUIUtilities.GetMessageInlines(message, index: 1, clickHandler: Hyperlink_Click);

            actual.Count.Should().Be(expected.Count);

            VerifyTextRun(expected[0], actual[0]);
            VerifyHyperlink(expected[1], actual[1]);
            VerifyTextRun(expected[2], actual[2]);
        }

        [Fact]
        public void GetMessageInlines_RendersWebLinkWithBackslashesInLinkText()
        {
            string url = "http://example.com";
            string message = $@"The file [..\directory\file.cpp]({url}) has a problem.";

            var link = new Hyperlink();
            link.Tag = new Tuple<int, object>(1, new Uri(url, UriKind.Absolute));
            link.Inlines.Add(new Run(@"..\directory\file.cpp"));

            var expected = new List<Inline>
            {
                new Run("The file "),
                link,
                new Run(" has a problem.")
            };

            var actual = SdkUIUtilities.GetMessageInlines(message, index: 1, clickHandler: Hyperlink_Click);

            actual.Count.Should().Be(expected.Count);

            VerifyTextRun(expected[0], actual[0]);
            VerifyHyperlink(expected[1], actual[1]);
            VerifyTextRun(expected[2], actual[2]);
        }

        private static void VerifyTextRun(Inline expected, Inline actual)
        {
            actual.Should().BeOfType(expected.GetType());
            (actual as Run).Text.Should().Be((expected as Run).Text);
        }

        private static void VerifyHyperlink(Inline expected, Inline actual)
        {
            actual.Should().BeOfType(expected.GetType());

            Hyperlink expectedLink = expected as Hyperlink;
            Hyperlink actualLink = actual as Hyperlink;

            actualLink.Inlines.Count.Should().Be(expectedLink.Inlines.Count);
            (actualLink.Inlines.FirstInline as Run).Text.Should().Be((expectedLink.Inlines.FirstInline as Run).Text);
            Tuple<int, object> tagActual = actualLink.Tag as Tuple<int, object>;
            Tuple<int, object> tagExpected = actualLink.Tag as Tuple<int, object>;
            tagActual.Item1.Should().Be(tagExpected.Item1);
            tagActual.Item2.Should().Be(tagExpected.Item2);
        }
    }
}

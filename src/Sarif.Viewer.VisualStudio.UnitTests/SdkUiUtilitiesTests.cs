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
    public class SdkUIUtilitiesTests : SarifViewerPackageUnitTests
    {
        private void Hyperlink_Click(object sender, RoutedEventArgs e) { }

        [Fact]
        public void GetInlinesForErrorMessage_DoesNotCreateLinks()
        {
            const string message = @"The quick [brown fox](2) jumps over the lazy dog.";

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
            const string message = @"The quick [brown fox](some text) jumps over the lazy dog.";

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
            const string message = @"The quick \[brown fox\] jumps over the lazy dog.";

            // Because there are no embedded links, we shouldn't get anything back
            var actual = SdkUIUtilities.GetMessageInlines(message, clickHandler: Hyperlink_Click);

            actual.Count.Should().Be(0);
        }

        [Fact]
        public void GetMessageInlines_RendersOneLink()
        {
            const string message = @"The quick [brown fox](1) jumps over the lazy dog.";

            var link = new Hyperlink();
            link.Tag = 1;
            link.Inlines.Add(new Run("brown fox"));

            var expected = new List<Inline>
            {
                new Run("The quick "),
                link,
                new Run(" jumps over the lazy dog.")
            };

            var actual = SdkUIUtilities.GetMessageInlines(message, clickHandler: Hyperlink_Click);

            actual.Count.Should().Be(expected.Count);

            VerifyTextRun(expected[0], actual[0]);
            VerifyHyperlink(expected[1], actual[1]);
            VerifyTextRun(expected[2], actual[2]);
        }

        [Fact]
        public void GetMessageInlines_RendersTwoLinksAndHandlesLinkAtTheEnd()
        {
            const string message = @"The quick [brown fox](1) jumps over the [lazy dog](2)";

            var link1 = new Hyperlink();
            link1.Tag = 1;
            link1.Inlines.Add(new Run("brown fox"));

            var link2 = new Hyperlink();
            link2.Tag = 2;
            link2.Inlines.Add(new Run("lazy dog"));

            var expected = new List<Inline>
            {
                new Run("The quick "),
                link1,
                new Run(" jumps over the "),
                link2
            };

            var actual = SdkUIUtilities.GetMessageInlines(message, clickHandler: Hyperlink_Click);

            actual.Count.Should().Be(expected.Count);

            VerifyTextRun(expected[0], actual[0]);
            VerifyHyperlink(expected[1], actual[1]);
            VerifyTextRun(expected[2], actual[2]);
            VerifyHyperlink(expected[3], actual[3]);
        }

        [Fact]
        public void GetMessageInlines_RendersOneLinkPlusLiteralBrackets()
        {
            const string message = @"The quick [brown fox](1) jumps over the \[lazy dog\].";

            var link = new Hyperlink();
            link.Tag = 1;
            link.Inlines.Add(new Run("brown fox"));

            var expected = new List<Inline>
            {
                new Run("The quick "),
                link,
                new Run(" jumps over the [lazy dog].")
            };

            var actual = SdkUIUtilities.GetMessageInlines(message, clickHandler: Hyperlink_Click);

            actual.Count.Should().Be(expected.Count);

            VerifyTextRun(expected[0], actual[0]);
            VerifyHyperlink(expected[1], actual[1]);
            VerifyTextRun(expected[2], actual[2]);
        }

        [Fact]
        public void GetMessageInlines_RendersWebLink()
        {
            const string url = "http://example.com";
            string message = $"The quick [brown fox]({url}) jumps over the lazy dog.";

            var link = new Hyperlink();
            link.Tag = new Uri(url, UriKind.Absolute);
            link.Inlines.Add(new Run("brown fox"));

            var expected = new List<Inline>
            {
                new Run("The quick "),
                link,
                new Run(" jumps over the lazy dog.")
            };

            var actual = SdkUIUtilities.GetMessageInlines(message, clickHandler: Hyperlink_Click);

            actual.Count.Should().Be(expected.Count);

            VerifyTextRun(expected[0], actual[0]);
            VerifyHyperlink(expected[1], actual[1]);
            VerifyTextRun(expected[2], actual[2]);
        }

        [Fact]
        public void GetMessageInlines_RendersWebLinkWithBackslashesInLinkText()
        {
            const string url = "http://example.com";
            string message = $@"The file [..\directory\file.cpp]({url}) has a problem.";

            var link = new Hyperlink();
            link.Tag = new Uri(url, UriKind.Absolute);
            link.Inlines.Add(new Run(@"..\directory\file.cpp"));

            var expected = new List<Inline>
            {
                new Run("The file "),
                link,
                new Run(" has a problem.")
            };

            var actual = SdkUIUtilities.GetMessageInlines(message, clickHandler: Hyperlink_Click);

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

            var expectedLink = expected as Hyperlink;
            var actualLink = actual as Hyperlink;

            actualLink.Inlines.Count.Should().Be(expectedLink.Inlines.Count);
            (actualLink.Inlines.FirstInline as Run).Text.Should().Be((expectedLink.Inlines.FirstInline as Run).Text);
            actual.Tag.Should().Be(expected.Tag);
        }
    }
}

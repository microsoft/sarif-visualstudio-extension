// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;

using Xunit;

using Run = System.Windows.Documents.Run;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class SdkUIUtilitiesTests : SarifViewerPackageUnitTests
    {
        [Fact]
        public void GetFileLocationPath_UriIsNull()
        {
            var dataCache = new RunDataCache();
            int runId = CodeAnalysisResultManager.Instance.GetNextRunIndex();
            CodeAnalysisResultManager.Instance.RunIndexToRunDataCache.Add(runId, dataCache);

            var artifact = new ArtifactLocation();
            string path = SdkUIUtilities.GetFileLocationPath(artifact, runId);
            path.Should().BeNull();

            artifact = null;
            path = SdkUIUtilities.GetFileLocationPath(artifact, runId);
            path.Should().BeNull();
        }

        [Fact]
        public void GetFileLocationPath_UriIsLocalPath()
        {
            var dataCache = new RunDataCache();
            int runId = CodeAnalysisResultManager.Instance.GetNextRunIndex();
            CodeAnalysisResultManager.Instance.RunIndexToRunDataCache.Add(runId, dataCache);

            string filePath = @"C:\repo\src\AnalysisStep.cs";
            var artifact = new ArtifactLocation { Uri = new Uri(filePath, UriKind.Absolute) };
            string path = SdkUIUtilities.GetFileLocationPath(artifact, runId);
            path.Should().Be(filePath);
        }

        [Fact]
        public void GetFileLocationPath_UriPathCanNotBeResolved()
        {
            string repoPath = "file:///C:/code/myProject/src/";
            var run = new Microsoft.CodeAnalysis.Sarif.Run
            {
                OriginalUriBaseIds = new Dictionary<string, ArtifactLocation>
                {
                    ["REPO_ROOT"] = new ArtifactLocation
                    {
                        Uri = new Uri(repoPath),
                    }
                },
            };
            var dataCache = new RunDataCache();
            int runId = CodeAnalysisResultManager.Instance.GetNextRunIndex();
            CodeAnalysisResultManager.Instance.RunIndexToRunDataCache.Add(runId, dataCache);
            CodeAnalysisResultManager.Instance.CacheUriBasePaths(run);

            string filePath = @"AnalysisStep.cs";
            var artifact = new ArtifactLocation { Uri = new Uri(filePath, UriKind.Relative), UriBaseId = "NOTEXIST" };
            string path = SdkUIUtilities.GetFileLocationPath(artifact, runId);
            path.Should().Be(filePath);
        }

        [Fact]
        public void GetFileLocationPath_UriPathCanBeResolved()
        {
            string repoPath = "file:///C:/code/myProject/src/";
            var run = new Microsoft.CodeAnalysis.Sarif.Run
            {
                OriginalUriBaseIds = new Dictionary<string, ArtifactLocation>
                {
                    ["REPO_ROOT"] = new ArtifactLocation
                    {
                        Uri = new Uri(repoPath),
                    }
                },
            };
            var dataCache = new RunDataCache();
            int runId = CodeAnalysisResultManager.Instance.GetNextRunIndex();
            CodeAnalysisResultManager.Instance.RunIndexToRunDataCache.Add(runId, dataCache);
            CodeAnalysisResultManager.Instance.CacheUriBasePaths(run);

            string filePath = @"AnalysisStep.cs";
            var artifact = new ArtifactLocation { Uri = new Uri(filePath, UriKind.Relative), UriBaseId = "REPO_ROOT" };
            string path = SdkUIUtilities.GetFileLocationPath(artifact, runId);
            path.Should().Be(@"C:\code\myProject\src\AnalysisStep.cs");
        }

        [Fact]
        public void GetInlinesForErrorMessage_DoesNotCreateLinks()
        {
            const string message = @"The quick [brown fox](2) jumps over the lazy dog.";

            var expected = new List<Inline>
            {
                new Run("The quick "),
                new Run("brown fox"),
                new Run(" jumps over the lazy dog."),
            };

            List<Inline> actual = SdkUIUtilities.GetInlinesForErrorMessage(message);

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
                new Run(" jumps over the lazy dog."),
            };

            List<Inline> actual = SdkUIUtilities.GetInlinesForErrorMessage(message);

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
            List<Inline> actual = SdkUIUtilities.GetMessageInlines(message, clickHandler: this.Hyperlink_Click);

            actual.Count.Should().Be(0);
        }

        [Fact]
        public void GetMessageInlines_RendersOneLink()
        {
            const string message = @"The quick [brown fox](1) jumps over the lazy dog.";

            var link = new Hyperlink { Tag = 1 };
            link.Inlines.Add(new Run("brown fox"));

            var expected = new List<Inline>
            {
                new Run("The quick "),
                link,
                new Run(" jumps over the lazy dog."),
            };

            List<Inline> actual = SdkUIUtilities.GetMessageInlines(message, clickHandler: this.Hyperlink_Click);

            actual.Count.Should().Be(expected.Count);

            VerifyTextRun(expected[0], actual[0]);
            VerifyHyperlink(expected[1], actual[1]);
            VerifyTextRun(expected[2], actual[2]);
        }

        [Fact]
        public void GetMessageInlines_RendersTwoLinksAndHandlesLinkAtTheEnd()
        {
            const string message = @"The quick [brown fox](1) jumps over the [lazy dog](2)";

            var link1 = new Hyperlink { Tag = 1 };
            link1.Inlines.Add(new Run("brown fox"));

            var link2 = new Hyperlink { Tag = 2 };
            link2.Inlines.Add(new Run("lazy dog"));

            var expected = new List<Inline>
            {
                new Run("The quick "),
                link1,
                new Run(" jumps over the "),
                link2,
            };

            List<Inline> actual = SdkUIUtilities.GetMessageInlines(message, clickHandler: this.Hyperlink_Click);

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

            var link = new Hyperlink { Tag = 1 };
            link.Inlines.Add(new Run("brown fox"));

            var expected = new List<Inline>
            {
                new Run("The quick "),
                link,
                new Run(" jumps over the [lazy dog]."),
            };

            List<Inline> actual = SdkUIUtilities.GetMessageInlines(message, clickHandler: this.Hyperlink_Click);

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
                new Run(" jumps over the lazy dog."),
            };

            List<Inline> actual = SdkUIUtilities.GetMessageInlines(message, clickHandler: this.Hyperlink_Click);

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
                new Run(" has a problem."),
            };

            List<Inline> actual = SdkUIUtilities.GetMessageInlines(message, clickHandler: this.Hyperlink_Click);

            actual.Count.Should().Be(expected.Count);

            VerifyTextRun(expected[0], actual[0]);
            VerifyHyperlink(expected[1], actual[1]);
            VerifyTextRun(expected[2], actual[2]);
        }


        [Fact]
        public void GetPlainText_GetNullIfInputIsNullOrEmpty()
        {
            List<Inline> inputs = null;
            string actual = SdkUIUtilities.GetPlainText(inputs);
            actual.Should().BeNull();

            inputs = new List<Inline>();
            actual = SdkUIUtilities.GetPlainText(inputs);
            actual.Should().BeNull();
        }

        [Fact]
        public void GetPlainText_ConvertInlinesWithNoLink()
        {
            // raw text "The file ['..\directory\file.cpp']({url}) has a problem."
            const string expected = @"The file '..\directory\file.cpp' has a problem.";

            var hyperlink = new Hyperlink(new Run(@"'..\directory\file.cpp'"));
            hyperlink.NavigateUri = new Uri("file://c:/repo/sarif/src/directory/file.cpp", UriKind.Absolute);
            var inputs = new List<Inline>
            {
                new Run("The file "),
                hyperlink,
                new Run(" has a problem."),
            };

            string actual = SdkUIUtilities.GetPlainText(inputs);

            actual.Should().NotBeNull();
            actual.Should().Be(expected);
        }

        [Fact]
        public void GetPlainText_ConvertInlinesWithPathLink()
        {
            // raw text "The quick brown fox jumps over the lazy dog."
            const string expected = @"The quick brown fox jumps over the lazy dog.";

            var inputs = new List<Inline>
            {
                new Run("The quick "),
                new Run("brown fox"),
                new Run(" jumps over the "),
                new Run("lazy dog."),
            };

            string actual = SdkUIUtilities.GetPlainText(inputs);

            actual.Should().NotBeNull();
            actual.Should().Be(expected);
        }

        [Fact]
        public void GetPlainText_ConvertInlinesWithHttpLink()
        {
            // raw text "The quick [brown fox](https://example.com) jumps over the lazy dog.";
            const string url = "http://example.com";
            const string expected = @"The quick brown fox jumps over the lazy dog.";

            var hyperlink = new Hyperlink(new Run("brown fox"));
            hyperlink.NavigateUri = new Uri(url);
            var inputs = new List<Inline>
            {
                new Run("The quick "),
                hyperlink,
                new Run(" jumps over the lazy dog."),
            };

            string actual = SdkUIUtilities.GetPlainText(inputs);

            actual.Should().NotBeNull();
            actual.Should().Be(expected);
        }

        [Fact]
        public void GetPlainText_ConvertInlinesWithTwoHttpLinks()
        {
            // raw text "The quick [brown fox](https://example.com) jumps over the [lazy dog](1).";
            const string url = "http://example.com";
            const string expected = @"The quick brown fox jumps over the lazy dog.";

            var hyperlink1 = new Hyperlink(new Run("brown fox"));
            hyperlink1.NavigateUri = new Uri(url);

            var hyperlink2 = new Hyperlink(new Run("lazy dog"));
            hyperlink2.Tag = 1;

            var inputs = new List<Inline>
            {
                new Run("The quick "),
                hyperlink1,
                new Run(" jumps over the "),
                hyperlink2,
                new Run("."),
            };

            string actual = SdkUIUtilities.GetPlainText(inputs);

            actual.Should().NotBeNull();
            actual.Should().Be(expected);
        }

        [Fact]
        public void GetPlainText_ConvertInlinesWithLiteralBrackets()
        {
            // raw text "The file ['..\directory\file.cpp']({url}) has a \[problem\]."
            const string expected = @"The file '..\directory\file.cpp' has a \[problem\].";

            var hyperlink = new Hyperlink(new Run(@"'..\directory\file.cpp'"));
            hyperlink.NavigateUri = new Uri("file://c:/repo/sarif/src/directory/file.cpp", UriKind.Absolute);
            var inputs = new List<Inline>
            {
                new Run("The file "),
                hyperlink,
                new Run(@" has a \[problem\]."),
            };

            string actual = SdkUIUtilities.GetPlainText(inputs);

            actual.Should().NotBeNull();
            actual.Should().Be(expected);
        }

        [Fact]
        public void ConvertToGithubRawPath_Tests()
        {
            var testcases = new[]
            {
                new
                {
                    Input = "https://github.com/microsoft/sarif-visualstudio-extension/blob/main/.github/workflows/dotnet-format.yml",
                    Expected = "https://raw.githubusercontent.com/microsoft/sarif-visualstudio-extension/main/.github/workflows/dotnet-format.yml",
                },
                new
                {
                    Input = "http://github.com/microsoft/sarif-visualstudio-extension/tree/main/src/Sarif.Viewer.VisualStudio/Data/ruleLookup.json",
                    Expected = "http://raw.githubusercontent.com/microsoft/sarif-visualstudio-extension/main/src/Sarif.Viewer.VisualStudio/Data/ruleLookup.json",
                },
                new
                {
                    Input = "https://test.com/path/to/file",
                    Expected = "https://test.com/path/to/file",
                },
                new
                {
                    Input = "  ",
                    Expected = "  ",
                },
                new
                {
                    Input = (string)null,
                    Expected = (string)null,
                },
                new
                {
                    Input = "https://github.com/appsettings.json",
                    Expected = "https://github.com/appsettings.json",
                },
            };

            foreach (var testcase in testcases)
            {
                string actual = SdkUIUtilities.ConvertToGithubRawPath(testcase.Input);
                actual.Should().Be(testcase.Expected);
            }
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

        private void Hyperlink_Click(object sender, RoutedEventArgs e) { }
    }
}

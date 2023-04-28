// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ErrorList;
using Microsoft.Sarif.Viewer.Models;

using Moq;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    // Added tests to Collection because otherwise the other tests
    // will load in parallel, which causes issues with static collections.
    // Production code will only load one SARIF file at a time.
    // See https://xunit.net/docs/running-tests-in-parallel.
    [Collection("SarifObjectTests")]
    [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "No point in naming test methods \"Async\".")]
    public class SarifFileWithContentsTests : SarifViewerPackageUnitTests
    {
        private const string Key1 = "/item.cpp#fragment";
        private const string Key2 = "/binary.cpp";
        private const string Key3 = "/text.cpp";
        private const string Key4 = "/both.cpp";
        private const string Key5 = "/emptybinary.cpp";
        private const string Key6 = "/emptytext.cpp";
        private const string Key7 = "/existinghash.cpp";
        private const string Key8 = "https://example.com/nonFileUriWIthEmbeddedContents";
        private const string ExpectedContents1 = "This is a test file.";
        private const string ExpectedContents2 = "The quick brown fox jumps over the lazy dog.";
        private const string ExpectedHashValue1 = "HashValue";
        private const string ExpectedHashValue2 = "ef537f25c895bfa782526529a9b63d97aa631564d5d789c2b765448c8635fb6c";
        private const string EmptyStringHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        private readonly SarifLog testLog;

        public SarifFileWithContentsTests()
        {
            this.testLog = new SarifLog
            {
                Runs = new List<Run>
                {
                    new Run
                    {
                        Tool = new Tool
                        {
                            Driver = new ToolComponent
                            {
                                Name = "Test",
                                SemanticVersion = "1.0",
                            },
                        },
                        Artifacts = new List<Artifact>
                        {
                            new Artifact
                            {
                                Location = new ArtifactLocation
                                {
                                    Uri = new Uri("file:///item.cpp#fragment"),
                                },
                                MimeType = "text/x-c",
                                Contents = new ArtifactContent()
                                {
                                    Binary = "VGhpcyBpcyBhIHRlc3QgZmlsZS4=",
                                },
                                Hashes = new Dictionary<string, string>
                                {
                                    { "sha-256", ExpectedHashValue1 },
                                },
                            },
                            new Artifact
                            {
                                Location = new ArtifactLocation
                                {
                                    Uri = new Uri("file:///binary.cpp"),
                                },
                                MimeType = "text/x-c",
                                Contents = new ArtifactContent()
                                {
                                    Binary = "VGhlIHF1aWNrIGJyb3duIGZveCBqdW1wcyBvdmVyIHRoZSBsYXp5IGRvZy4=",
                                },
                                Hashes = new Dictionary<string, string>
                                {
                                    { "sha-256", ExpectedHashValue2 },
                                },
                            },
                            new Artifact
                            {
                                Location = new ArtifactLocation
                                {
                                    Uri = new Uri(Key8),
                                },
                                Contents = new ArtifactContent()
                                {
                                    Binary = "VGhlIHF1aWNrIGJyb3duIGZveCBqdW1wcyBvdmVyIHRoZSBsYXp5IGRvZy4=",
                                },
                                Hashes = new Dictionary<string, string>
                                {
                                    { "sha-256", ExpectedHashValue2 },
                                },
                            },
                            new Artifact
                            {
                                Location = new ArtifactLocation
                                {
                                    Uri = new Uri("file:///text.cpp"),
                                },
                                MimeType = "text/x-c",
                                Contents = new ArtifactContent()
                                {
                                    Text = ExpectedContents1,
                                },
                            },
                            new Artifact
                            {
                                Location = new ArtifactLocation
                                {
                                    Uri = new Uri("file:///both.cpp"),
                                },
                                MimeType = "text/x-c",
                                Contents = new ArtifactContent()
                                {
                                    Binary = "VGhlIHF1aWNrIGJyb3duIGZveCBqdW1wcyBvdmVyIHRoZSBsYXp5IGRvZy4=",
                                    Text = ExpectedContents2,
                                },
                                Hashes = new Dictionary<string, string>
                                {
                                    { "sha-256", ExpectedHashValue2 },
                                },
                            },
                            new Artifact
                            {
                                Location = new ArtifactLocation
                                {
                                    Uri = new Uri("file:///emptybinary.cpp"),
                                },
                                MimeType = "text/x-c",
                                Contents = new ArtifactContent()
                                {
                                    Binary = string.Empty,
                                },
                            },
                            new Artifact
                            {
                                Location = new ArtifactLocation
                                {
                                    Uri = new Uri("file:///emptytext.cpp"),
                                },
                                MimeType = "text/x-c",
                                Contents = new ArtifactContent()
                                {
                                    Text = string.Empty,
                                },
                            },
                            new Artifact
                            {
                                Location = new ArtifactLocation
                                {
                                    Uri = new Uri("file:///existinghash.cpp"),
                                },
                                MimeType = "text/x-c",
                                Contents = new ArtifactContent()
                                {
                                    Text = ExpectedContents2,
                                },
                                Hashes = new Dictionary<string, string>
                                {
                                    { "sha-256", ExpectedHashValue1 },
                                },
                            },
                        },
                        Results = new List<Result>
                        {
                            new Result
                            {
                                AnalysisTarget = new ArtifactLocation
                                {
                                    Uri = new Uri("file:///item.cpp"),
                                },
                                RuleId = "C0001",
                                Message = new Message { Text = "Error 1" },
                                Locations = new List<Location>
                                {
                                    new Location(),
                                },
                            },
                        },
                    },
                },
            };
        }

        private RunDataCache CurrentRunDataCache =>
            resultManager.RunIndexToRunDataCache[this.CurrentRunIndex];

        private int CurrentRunIndex =>

            // CodeAnalysisResultManager.Instance.CurrentRunIndex is currently an internal (instead of private)
            // member of CodeAnalysisResultManager only for this test.
            resultManager.CurrentRunIndex;

        [Fact]
        public async Task SarifFileWithContents_SavesContents()
        {
            await TestUtilities.InitializeTestEnvironmentAsync(this.testLog);

            IDictionary<string, ArtifactDetailsModel> fileDetails = this.CurrentRunDataCache.FileDetails;

            fileDetails.Should().ContainKey(Key1);
        }

        [Fact]
        public async Task SarifFileWithContents_DecodesBinaryContents()
        {
            await TestUtilities.InitializeTestEnvironmentAsync(this.testLog);

            IDictionary<int, RunDataCache> x = CodeAnalysisResultManager.Instance.RunIndexToRunDataCache;
            ArtifactDetailsModel fileDetail = this.CurrentRunDataCache.FileDetails[Key2];
            string contents = fileDetail.GetContents();

            fileDetail.Sha256Hash.Should().Be(ExpectedHashValue2);
            contents.Should().Be(ExpectedContents2);
        }

        [Fact]
        public async Task SarifFileWithContents_OpensEmbeddedBinaryFile()
        {
            // arrange
            string mockContent = null;
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem
                .Setup(fs => fs.FileExists(It.IsAny<string>()))
                .Returns((string path) => false);
            mockFileSystem
                .Setup(fs => fs.FileWriteAllText(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string path, string content) => mockContent = content);
            mockFileSystem
                .Setup(fs => fs.FileSetAttributes(It.IsAny<string>(), FileAttributes.ReadOnly))
                .Verifiable();
            CodeAnalysisResultManager.Instance = new CodeAnalysisResultManager(mockFileSystem.Object);

            // act
            await TestUtilities.InitializeTestEnvironmentAsync(this.testLog);

            string rebaselinedFile = CodeAnalysisResultManager.Instance.CreateFileFromContents(this.CurrentRunIndex, Key2);
            ArtifactDetailsModel fileDetail = this.CurrentRunDataCache.FileDetails[Key2];
            string fileText = mockContent;

            fileDetail.Sha256Hash.Should().BeEquivalentTo(ExpectedHashValue2);
            fileText.Should().BeEquivalentTo(ExpectedContents2);
        }

        [Fact]
        public async Task SarifFileWithContents_OpensEmbeddedNonFileUriBinaryFile()
        {
            string mockContent = null;
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem
                .Setup(fs => fs.FileExists(It.IsAny<string>()))
                .Returns((string path) => false);
            mockFileSystem
                .Setup(fs => fs.FileWriteAllText(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string path, string content) => mockContent = content);
            mockFileSystem
                .Setup(fs => fs.FileSetAttributes(It.IsAny<string>(), FileAttributes.ReadOnly))
                .Verifiable();
            CodeAnalysisResultManager.Instance = new CodeAnalysisResultManager(mockFileSystem.Object, solutionPath: "");
            ErrorListService.CodeManagerInstance = CodeAnalysisResultManager.Instance;
            await TestUtilities.InitializeTestEnvironmentAsync(this.testLog);

            string rebaselinedFile = CodeAnalysisResultManager.Instance.CreateFileFromContents(this.CurrentRunIndex, Key8);
            ArtifactDetailsModel fileDetail = this.CurrentRunDataCache.FileDetails[Key8];
            string fileText = mockContent;

            fileDetail.Sha256Hash.Should().Be(ExpectedHashValue2);
            fileText.Should().Be(ExpectedContents2);
        }

        [Fact]
        public async Task SarifFileWithContents_DecodesTextContents()
        {
            await TestUtilities.InitializeTestEnvironmentAsync(this.testLog);

            ArtifactDetailsModel fileDetail = this.CurrentRunDataCache.FileDetails[Key3];
            string contents = fileDetail.GetContents();

            contents.Should().Be(ExpectedContents1);
        }

        [Fact]
        public async Task SarifFileWithContents_DecodesBinaryContentsWithText()
        {
            await TestUtilities.InitializeTestEnvironmentAsync(this.testLog);

            ArtifactDetailsModel fileDetail = this.CurrentRunDataCache.FileDetails[Key4];
            string contents = fileDetail.GetContents();

            fileDetail.Sha256Hash.Should().Be(ExpectedHashValue2);
            contents.Should().Be(ExpectedContents2);
        }

        [Fact]
        public async Task SarifFileWithContents_HandlesEmptyBinaryContents()
        {
            await TestUtilities.InitializeTestEnvironmentAsync(this.testLog);

            ArtifactDetailsModel fileDetail = this.CurrentRunDataCache.FileDetails[Key5];
            string contents = fileDetail.GetContents();

            fileDetail.Sha256Hash.Should().Be(EmptyStringHash);
            contents.Should().Be(string.Empty);
        }

        [Fact]
        public void SarifFileWithContents_HandlesEmptyTextContents()
        {
            ArtifactDetailsModel fileDetail = this.CurrentRunDataCache.FileDetails[Key6];
            string contents = fileDetail.GetContents();

            fileDetail.Sha256Hash.Should().Be(EmptyStringHash);
            contents.Should().Be(string.Empty);
        }

        [Fact]
        public async Task SarifFileWithContents_HandlesExistingHash()
        {
            await TestUtilities.InitializeTestEnvironmentAsync(this.testLog);

            ArtifactDetailsModel fileDetail = this.CurrentRunDataCache.FileDetails[Key7];
            string contents = fileDetail.GetContents();

            fileDetail.Sha256Hash.Should().Be(ExpectedHashValue1);
            contents.Should().Be(ExpectedContents2);
        }

        [Fact]
        public async Task SarifFileWithContents_GeneratesHash()
        {
            await TestUtilities.InitializeTestEnvironmentAsync(this.testLog);

            ArtifactDetailsModel fileDetail = this.CurrentRunDataCache.FileDetails[Key1];
            string contents = fileDetail.GetContents();

            fileDetail.Sha256Hash.Should().Be(ExpectedHashValue1);
            contents.Should().Be(ExpectedContents1);
        }
    }
}

﻿// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.CodeAnalysis.Sarif;
using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    // Added tests to Collection because otherwise the other tests
    // will load in parallel, which causes issues with static collections.
    // Production code will only load one SARIF file at a time.
    [Collection("SarifObjectTests")]
    public class SarifFileWithContentsTests
    {
        private const int RunId = 1;
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

        public SarifFileWithContentsTests()
        {
            SarifViewerPackage.IsUnitTesting = true;
            var testLog = new SarifLog
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
                                SemanticVersion = "1.0"
                            }
                        },
                        Artifacts = new List<Artifact>
                        {
                            new Artifact
                            {
                                Location = new ArtifactLocation
                                {
                                    Uri = new Uri("file:///item.cpp#fragment")
                                },
                                MimeType = "text/x-c",
                                Contents = new ArtifactContent()
                                {
                                    Binary = "VGhpcyBpcyBhIHRlc3QgZmlsZS4="
                                },
                                Hashes = new Dictionary<string, string>
                                {
                                    { "sha-256", ExpectedHashValue1 }
                                }
                            },
                            new Artifact
                            {
                                Location = new ArtifactLocation
                                {
                                    Uri = new Uri("file:///binary.cpp")
                                },
                                MimeType = "text/x-c",
                                Contents = new ArtifactContent()
                                {
                                    Binary = "VGhlIHF1aWNrIGJyb3duIGZveCBqdW1wcyBvdmVyIHRoZSBsYXp5IGRvZy4="
                                },
                                Hashes = new Dictionary<string, string>
                                {
                                    { "sha-256", ExpectedHashValue2 }
                                }
                            },
                            new Artifact
                            {
                                Location = new ArtifactLocation
                                {
                                    Uri = new Uri(Key8)
                                },
                                Contents = new ArtifactContent()
                                {
                                    Binary = "VGhlIHF1aWNrIGJyb3duIGZveCBqdW1wcyBvdmVyIHRoZSBsYXp5IGRvZy4="
                                },
                                Hashes = new Dictionary<string, string>
                                {
                                    { "sha-256", ExpectedHashValue2 }
                                }
                            },
                            new Artifact
                            {
                                Location = new ArtifactLocation
                                {
                                    Uri = new Uri("file:///text.cpp")
                                },
                                MimeType = "text/x-c",
                                Contents = new ArtifactContent()
                                {
                                    Text = ExpectedContents1
                                }
                            },
                            new Artifact
                            {
                                Location = new ArtifactLocation
                                {
                                    Uri = new Uri("file:///both.cpp")
                                },
                                MimeType = "text/x-c",
                                Contents = new ArtifactContent()
                                {
                                    Binary = "VGhlIHF1aWNrIGJyb3duIGZveCBqdW1wcyBvdmVyIHRoZSBsYXp5IGRvZy4=",
                                    Text = ExpectedContents2
                                },
                                Hashes = new Dictionary<string, string>
                                {
                                    { "sha-256", ExpectedHashValue2 }
                                }
                            },
                            new Artifact
                            {
                                Location = new ArtifactLocation
                                {
                                    Uri = new Uri("file:///emptybinary.cpp")
                                },
                                MimeType = "text/x-c",
                                Contents = new ArtifactContent()
                                {
                                    Binary = ""
                                }
                            },
                            new Artifact
                            {
                                Location = new ArtifactLocation
                                {
                                    Uri = new Uri("file:///emptytext.cpp")
                                },
                                MimeType = "text/x-c",
                                Contents = new ArtifactContent()
                                {
                                    Text = ""
                                }
                            },
                            new Artifact
                            {
                                Location = new ArtifactLocation
                                {
                                    Uri = new Uri("file:///existinghash.cpp")
                                },
                                MimeType = "text/x-c",
                                Contents = new ArtifactContent()
                                {
                                    Text = ExpectedContents2
                                },
                                Hashes = new Dictionary<string, string>
                                {
                                    { "sha-256", ExpectedHashValue1 }
                                }
                            }
                        },
                        Results = new List<Result>
                        {
                            new Result
                            {
                                AnalysisTarget = new ArtifactLocation
                                {
                                    Uri = new Uri(@"file:///item.cpp")
                                },
                                RuleId = "C0001",
                                Message = new Message { Text = "Error 1" },
                                Locations = new List<Location>
                                {
                                    new Location() { }
                                }
                            }
                        }
                    }
                }
            };

            TestUtilities.InitializeTestEnvironment(testLog);
        }

        [Fact]
        public void SarifFileWithContents_SavesContents()
        {
            var fileDetails = CodeAnalysisResultManager.Instance.RunDataCaches[RunId].FileDetails;

            fileDetails.Should().ContainKey(Key1);
        }

        [Fact]
        public void SarifFileWithContents_DecodesBinaryContents()
        {
            var fileDetail = CodeAnalysisResultManager.Instance.RunDataCaches[RunId].FileDetails[Key2];
            var contents = fileDetail.GetContents();

            fileDetail.Sha256Hash.Should().Be(ExpectedHashValue2);
            contents.Should().Be(ExpectedContents2);
        }

        [Fact]
        public void SarifFileWithContents_OpensEmbeddedBinaryFile()
        {
            var rebaselinedFile = CodeAnalysisResultManager.Instance.CreateFileFromContents(RunId, Key2);
            var fileDetail = CodeAnalysisResultManager.Instance.RunDataCaches[RunId].FileDetails[Key2];
            var fileText = File.ReadAllText(rebaselinedFile);

            fileDetail.Sha256Hash.Should().Be(ExpectedHashValue2);
            fileText.Should().Be(ExpectedContents2);
        }

        [Fact]
        public void SarifFileWithContents_OpensEmbeddedNonFileUriBinaryFile()
        {
            var rebaselinedFile = CodeAnalysisResultManager.Instance.CreateFileFromContents(RunId, Key8);
            var fileDetail = CodeAnalysisResultManager.Instance.RunDataCaches[RunId].FileDetails[Key8];
            var fileText = File.ReadAllText(rebaselinedFile);

            fileDetail.Sha256Hash.Should().Be(ExpectedHashValue2);
            fileText.Should().Be(ExpectedContents2);
        }
       
        [Fact]
        public void SarifFileWithContents_DecodesTextContents()
        {
            var fileDetail = CodeAnalysisResultManager.Instance.RunDataCaches[RunId].FileDetails[Key3];
            var contents = fileDetail.GetContents();

            contents.Should().Be(ExpectedContents1);
        }

        [Fact]
        public void SarifFileWithContents_DecodesBinaryContentsWithText()
        {
            var fileDetail = CodeAnalysisResultManager.Instance.RunDataCaches[RunId].FileDetails[Key4];
            var contents = fileDetail.GetContents();

            fileDetail.Sha256Hash.Should().Be(ExpectedHashValue2);
            contents.Should().Be(ExpectedContents2);
        }

        [Fact]
        public void SarifFileWithContents_HandlesEmptyBinaryContents()
        {
            var fileDetail = CodeAnalysisResultManager.Instance.RunDataCaches[RunId].FileDetails[Key5];
            var contents = fileDetail.GetContents();

            fileDetail.Sha256Hash.Should().Be(EmptyStringHash);
            contents.Should().Be(string.Empty);
        }

        [Fact]
        public void SarifFileWithContents_HandlesEmptyTextContents()
        {
            var fileDetail = CodeAnalysisResultManager.Instance.RunDataCaches[RunId].FileDetails[Key6];
            var contents = fileDetail.GetContents();

            fileDetail.Sha256Hash.Should().Be(EmptyStringHash);
            contents.Should().Be(String.Empty);
        }

        [Fact]
        public void SarifFileWithContents_HandlesExistingHash()
        {
            var fileDetail = CodeAnalysisResultManager.Instance.RunDataCaches[RunId].FileDetails[Key7];
            var contents = fileDetail.GetContents();

            fileDetail.Sha256Hash.Should().Be(ExpectedHashValue1);
            contents.Should().Be(ExpectedContents2);
        }

        [Fact]
        public void SarifFileWithContents_GeneratesHash()
        {
            var fileDetail = CodeAnalysisResultManager.Instance.RunDataCaches[RunId].FileDetails[Key1];
            var contents = fileDetail.GetContents();

            fileDetail.Sha256Hash.Should().Be(ExpectedHashValue1);
            contents.Should().Be(ExpectedContents1);
        }
    }
}
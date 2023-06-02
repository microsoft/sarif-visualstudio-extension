// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class VersionControlParserTests
    {
        [Fact]
        public void ConvertToGithubRawPathTests()
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
                // input is already a raw file link
                new
                {
                    Input = "https://raw.githubusercontent.com/microsoft/sarif-visualstudio-extension/main/.github/workflows/dotnet-format.yml",
                    Expected = "https://raw.githubusercontent.com/microsoft/sarif-visualstudio-extension/main/.github/workflows/dotnet-format.yml",
                },
                new
                {
                    Input = "https://test.com/path/to/file",
                    Expected = "https://test.com/path/to/file",
                },
                new
                {
                    Input = "http://github1com/microsoft/sarif-visualstudio-extension/tree/main/src/Sarif.Viewer.VisualStudio/Data/ruleLookup.json",
                    Expected = "http://github1com/microsoft/sarif-visualstudio-extension/tree/main/src/Sarif.Viewer.VisualStudio/Data/ruleLookup.json",
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
                string actual = new GithubVersionControlParser(null).ConvertToRawPath(testcase.Input);
                actual.Should().Be(testcase.Expected);
            }
        }

        [Fact]
        public void ConvertToAdoRawPathTests()
        {
            var testcases = new[]
            {
                new
                {
                    Input = "https://dev.azure.com/org1/project2/_git/repo3?path=%2Fsrc%2Fproduct%2Ftests.cs",
                    Expected = "https://dev.azure.com/org1/project2/_apis/git/repositories/repo3/items?path=%2Fsrc%2Fproduct%2Ftests.cs",
                },
                new
                {
                    Input = "http://dev.azure.com/org1/project2/_git/repo3?path=%2Fsrc%2Fproduct%2Ftests.cs&version=GBusers%2Fyong%2Fadd",
                    Expected = "http://dev.azure.com/org1/project2/_apis/git/repositories/repo3/items?path=%2Fsrc%2Fproduct%2Ftests.cs&versionDescriptor[version]=users%2Fyong%2Fadd",
                },
                // input is already a raw file link
                                new
                {
                    Input = "http://dev.azure.com/org1/project2/_apis/git/repositories/repo3/items?path=%2Fsrc%2Fproduct%2Ftests.cs&versionDescriptor[version]=users%2Fyong%2Fadd",
                    Expected = "http://dev.azure.com/org1/project2/_apis/git/repositories/repo3/items?path=%2Fsrc%2Fproduct%2Ftests.cs&versionDescriptor[version]=users%2Fyong%2Fadd",
                },
                new
                {
                    Input = "https://test.com/path/to/file",
                    Expected = "https://test.com/path/to/file",
                },
                new
                {
                    Input = "http://dev_azure.com/org1/project2/_git/repo3?path=%2Fsrc%2Fproduct%2Ftests.cs&version=GBusers%2Fyong%2Fadd",
                    Expected = "http://dev_azure.com/org1/project2/_git/repo3?path=%2Fsrc%2Fproduct%2Ftests.cs&version=GBusers%2Fyong%2Fadd",
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
                    Input = "https://dev.azure.com/org1/project2/_git/commit/97e943dbc38fe370cd4b3f8630db182092095991",
                    Expected = "https://dev.azure.com/org1/project2/_git/commit/97e943dbc38fe370cd4b3f8630db182092095991",
                },
            };

            foreach (var testcase in testcases)
            {
                string actual = new AdoVersionControlParser(null).ConvertToRawPath(testcase.Input);
                actual.Should().Be(testcase.Expected);
            }
        }

        [Fact]
        public void GithubVersionControlParserTests()
        {
            var testcases = new[]
            {
                new
                {
                    VCData = new VersionControlDetails
                            {
                                RepositoryUri = new Uri("https://github.com/microsoft/sarif-visualstudio-extension/"),
                                RevisionId = "378c2ee96a7dc1d8e487e2a02ce4dc73f67750e7",
                                Branch = "main",
                            },
                    ExpectedParser = true,
                    RelativeFilePathInput = ".github/workflows/dotnet-format.yml",
                    Expected = "https://raw.githubusercontent.com/microsoft/sarif-visualstudio-extension/main/.github/workflows/dotnet-format.yml",
                },

                new
                {
                    VCData = new VersionControlDetails
                            {
                                RepositoryUri = new Uri("https://github.com/microsoft/sarif-visualstudio-extension"),
                                RevisionId = "378c2ee96a7dc1d8e487e2a02ce4dc73f67750e7",
                                Branch = "main",
                            },
                    ExpectedParser = true,
                    RelativeFilePathInput = ".github/workflows/dotnet-format.yml",
                    Expected = "https://raw.githubusercontent.com/microsoft/sarif-visualstudio-extension/main/.github/workflows/dotnet-format.yml",
                },

                // not supported source control
                new
                {
                    VCData = new VersionControlDetails
                            {
                                RepositoryUri = new Uri("https://gitee.com/anji-plus/report"),
                                RevisionId = "378c2ee96a7dc1d8e487e2a02ce4dc73f67750e7",
                                Branch = "main",
                            },
                    ExpectedParser = false,
                    RelativeFilePathInput = "pom.xml",
                    Expected = "https://gitee.com/anji-plus/report/raw/master/pom.xml",
                },

                new
                {
                    VCData = (VersionControlDetails)null,
                    ExpectedParser = false,
                    RelativeFilePathInput = (string)null,
                    Expected = (string)null,
                },
            };

            foreach (var testcase in testcases)
            {
                bool result = VersionControlParserFactory.TryGetVersionControlParser(testcase.VCData, out IVersionControlParser parser);
                result.Should().Be(testcase.ExpectedParser);
                if (result)
                {
                    Uri actual = parser.GetSourceFileUri(testcase.RelativeFilePathInput);
                    actual.Should().Be(testcase.Expected);
                }
            }
        }
    }
}

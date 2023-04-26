// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Models;
using Microsoft.Sarif.Viewer.Sarif;

using Moq;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests.Sarif
{
    public class LocationExtensionsTest
    {
        private const string CodeSample =
    @"
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace AnalysisTestProject2
{
    internal class Class4
    {
    }
}
            ";

        [Fact]
        public void ExtractSnippet_ReturnsExpectedValue()
        {
            var mock = new Mock<IFileSystem>();
            var regionCache = new FileRegionsCache(fileSystem: mock.Object);
            var testCases = new[]
            {
                new
                {
                    location = new Location { PhysicalLocation = null },
                    run = new Run(),
                    regionCache = regionCache,
                    expectedResult = string.Empty,
                },
                new
                {
                    location = new Location { PhysicalLocation = new PhysicalLocation { ArtifactLocation = null } },
                    run = new Run(),
                    regionCache = regionCache,
                    expectedResult = string.Empty,
                },
                new
                {
                    location = new Location { PhysicalLocation = new PhysicalLocation { ArtifactLocation = new ArtifactLocation { Uri = null } } },
                    run = new Run(),
                    regionCache = regionCache,
                    expectedResult = string.Empty,
                },
                new
                {
                    location = new Location { PhysicalLocation = new PhysicalLocation { ArtifactLocation = new ArtifactLocation { Uri = new Uri("file://temp.cs") } } },
                    run = new Run(),
                    regionCache = regionCache,
                    expectedResult = string.Empty,
                },
                new
                {
                    location = new Location
                    {
                        PhysicalLocation = new PhysicalLocation
                        {
                            ArtifactLocation = new ArtifactLocation { Uri = new Uri("file://temp.cs") },
                            Region = new Region(),
                        },
                    },
                    run = new Run(),
                    regionCache = regionCache,
                    expectedResult = string.Empty,
                },
                new
                {
                    location = new Location
                    {
                        PhysicalLocation = new PhysicalLocation
                        {
                            ArtifactLocation = new ArtifactLocation { Uri = new Uri("file://temp.cs") },
                            Region = new Region { ByteOffset = 0 },
                        },
                    },
                    run = new Run(),
                    regionCache = regionCache,
                    expectedResult = string.Empty,
                },
                new
                {
                    location = new Location
                    {
                        PhysicalLocation = new PhysicalLocation
                        {
                            ArtifactLocation = new ArtifactLocation { Uri = new Uri("file://temp.cs") },
                            Region = new Region { Snippet = new ArtifactContent { Text = "private class Class5" } },
                        },
                    },
                    run = new Run(),
                    regionCache = regionCache,
                    expectedResult = "private class Class5",
                },
            };

            int failedCases = 0;
            foreach (var testcase in testCases)
            {
                string snippet = testcase.location.ExtractSnippet(testcase.run, testcase.regionCache);
                if (snippet != testcase.expectedResult)
                {
                    failedCases++;
                }
            }

            failedCases.Should().Be(0, "failed test cases: " + failedCases.ToString());
        }

        [Fact]
        public void ExtractSnippet_FileDoesNotExist_ReturnEmpty()
        {
            var mock = new Mock<IFileSystem>();
            mock.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);
            var regionCache = new FileRegionsCache(fileSystem: mock.Object);

            LocationModel location = new Location
            {
                PhysicalLocation = new PhysicalLocation
                {
                    ArtifactLocation = new ArtifactLocation { Uri = new Uri("file://temp.cs") },
                    Region = new Region { Snippet = new ArtifactContent { Text = "private class Class5" } },
                },
            }.ToLocationModel(new Run(), 1, 1);

            string snippet = location.ExtractSnippet(regionCache, mock.Object);
            snippet.Should().BeEquivalentTo(string.Empty);
        }

        [Fact]
        public void ExtractSnippet_FileExists_ReturnEmpty()
        {
            var mock = new Mock<IFileSystem>();
            mock.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);
            mock.Setup(fs => fs.FileReadAllText(It.IsAny<string>())).Returns(CodeSample);
            var regionCache = new FileRegionsCache(fileSystem: mock.Object);
            string expectedSnippet = "internal class Class4";

            LocationModel location = new Location
            {
                PhysicalLocation = new PhysicalLocation
                {
                    ArtifactLocation = new ArtifactLocation { Uri = new Uri("file://temp.cs") },
                    Region = new Region
                    {
                        Snippet = new ArtifactContent { Text = expectedSnippet },
                        StartLine = 7,
                        StartColumn = 5,
                        EndLine = 7,
                        EndColumn = 25,
                    },
                },
            }.ToLocationModel(new Run(), 1, 1);

            string snippet = location.ExtractSnippet(regionCache, mock.Object);
            snippet.Should().BeEquivalentTo(expectedSnippet);
        }
    }
}

﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.Shell;

using Moq;

using Xunit;

namespace Microsoft.Sarif.Viewer.VisualStudio.UnitTests
{
    public class ResultTextMarkerTests : SarifViewerPackageUnitTests
    {
        [Fact]
        public void TryToFullyPopulateRegionAndFilePath_EmptyFullFilePath_ShouldFail()
        {
            var textMarker = new ResultTextMarker(runIndex: 1, resultId: 1, uriBaseId: "SRCROOT", region: new Region(), fullFilePath: string.Empty, nonHghlightedColor: string.Empty, highlightedColor: string.Empty, context: null, fileSystem: null);

            textMarker.TryToFullyPopulateRegionAndFilePath().Should().BeFalse();
            textMarker.regionAndFilePathAreFullyPopulated.Should().BeNull();
        }

        [Fact]
        public void TryToFullyPopulateRegionAndFilePath_FullFilePathDoesNotExist_ShouldFail()
        {
            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

            var fileSystem2Mock = new Mock<IFileSystem2>();
            fileSystem2Mock.Setup(fs => fs.IsPathRooted(It.IsAny<string>())).Returns(true);

            string sourceFilePath = Path.Combine(Directory.GetCurrentDirectory(), @"src\view\controller.cs");
            var textMarker = new ResultTextMarker(runIndex: 1, resultId: 1, uriBaseId: "SRCROOT", region: new Region(), fullFilePath: sourceFilePath, nonHghlightedColor: string.Empty, highlightedColor: string.Empty, context: null, fileSystem: fileSystemMock.Object, fileSystem2: fileSystem2Mock.Object);

            textMarker.TryToFullyPopulateRegionAndFilePath().Should().BeFalse();
            textMarker.regionAndFilePathAreFullyPopulated.Should().BeFalse();
        }

        [Fact]
        public void TryToFullyPopulateRegionAndFilePath_FullFilePathExists_ShouldSucceed()
        {
            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);

            var fileSystem2Mock = new Mock<IFileSystem2>();
            fileSystem2Mock.Setup(fs => fs.IsPathRooted(It.IsAny<string>())).Returns(true);

            string sourceFilePath = Path.Combine(Directory.GetCurrentDirectory(), @"src\view\controller.cs");
            var textMarker = new ResultTextMarker(runIndex: 1, resultId: 1, uriBaseId: "SRCROOT", region: new Region(), fullFilePath: sourceFilePath, nonHghlightedColor: string.Empty, highlightedColor: string.Empty, context: null, fileSystem: fileSystemMock.Object, fileSystem2: fileSystem2Mock.Object);

            textMarker.TryToFullyPopulateRegionAndFilePath().Should().BeTrue();
            textMarker.regionAndFilePathAreFullyPopulated.Should().BeTrue();
        }

        [Fact]
        public void TryToFullyPopulateRegionAndFilePath_RelativeFilePath_InWorkingDirectory_ShouldSucceed()
        {
            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);
            fileSystemMock.Setup(fs => fs.EnvironmentCurrentDirectory).Returns(Directory.GetCurrentDirectory());

            var fileSystem2Mock = new Mock<IFileSystem2>();
            fileSystem2Mock.Setup(fs => fs.IsPathRooted(It.IsAny<string>())).Returns(true);

            string sourceFilePath = "src/view/controller.cs";
            var textMarker = new ResultTextMarker(runIndex: 1, resultId: 1, uriBaseId: "SRCROOT", region: new Region(), fullFilePath: sourceFilePath, nonHghlightedColor: string.Empty, highlightedColor: string.Empty, context: null, fileSystem: fileSystemMock.Object, fileSystem2: fileSystem2Mock.Object);

            textMarker.TryToFullyPopulateRegionAndFilePath().Should().BeTrue();
            textMarker.regionAndFilePathAreFullyPopulated.Should().BeTrue();
        }
    }
}

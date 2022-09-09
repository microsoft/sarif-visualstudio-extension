// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using EnvDTE80;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Sarifer;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Workspace.VSIntegration.UI;

using Moq;

using Xunit;

using static Microsoft.CodeAnalysis.Sarif.Sarifer.Commands.AnalyzeSolutionFolderNodeExtender;

namespace Sarif.Sarifer.UnitTests
{
    public class AnalyzeSolutionFolderCommandHandlerTests
    {
        public AnalyzeSolutionFolderCommandHandlerTests()
        {
            SariferPackage.IsUnitTesting = true;
        }

        [Fact]
        public void AnalyzeTargets_FileNodeSelected_Test()
        {
            const string codeFile = @"C:\github\repo\myproject\src\project\code.cs";

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem
                .Setup(fs => fs.FileExists(codeFile))
                .Returns(true);

            var mockAnalysisService = new Mock<IBackgroundAnalysisService>();

            var mockNode = new Mock<IFileNode>();
            mockNode
                .Setup(node => node.FullPath)
                .Returns(codeFile);

            var command = new AnalyzeSolutionFolderCommandHandler(
                mockNode.Object,
                mockAnalysisService.Object,
                mockFileSystem.Object);

            command.AnalyzeTargets(new[] { mockNode.Object });

            mockAnalysisService.Verify(
                service => service.AnalyzeAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public void AnalyzeTargets_FileNodeDoesNotExist_Test()
        {
            const string codeFile = @"C:\github\repo\myproject\src\project\code.cs";

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem
                .Setup(fs => fs.FileExists(codeFile))
                .Returns(false);

            var mockAnalysisService = new Mock<IBackgroundAnalysisService>();

            var mockNode = new Mock<IFileNode>();
            mockNode
                .Setup(node => node.FullPath)
                .Returns(codeFile);

            var command = new AnalyzeSolutionFolderCommandHandler(
                mockNode.Object,
                mockAnalysisService.Object,
                mockFileSystem.Object);

            command.AnalyzeTargets(new[] { mockNode.Object });

            mockAnalysisService.Verify(
                service => service.AnalyzeAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // todo: Lack of tests for AnalyzeTargets(IEnumerable<IFolderNode>) due to
        // it uses System.IO.DirectoryInfo/FileInfo etc which cannot be mocked.
    }
}

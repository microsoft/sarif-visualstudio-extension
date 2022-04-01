// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using CSharpFunctionalExtensions;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Services;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Services.GitHub;

using Moq;

using Sarif.Viewer.VisualStudio.Shell.Core;

using Xunit;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.UnitTests
{
    public class ResultSourceFactoryTests
    {
        [Fact]
        public void GetResultSourceService_ReturnsGitHubSourceService_WhenPathContainsDotGitDirectory()
        {
            string path = @"C:\Git\MyProject";

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);

            var mockGitExe = new Mock<IGitExe>();
            mockGitExe.Setup(g => g.GetRepoRootAsync()).Returns(new ValueTask<string>(path));

            var resultSourceFactory = new ResultSourceFactory(mockFileSystem.Object, mockGitExe.Object);
            Result<IResultSourceService, ErrorType> result = resultSourceFactory.GetResultSourceServiceAsync(path).ConfigureAwait(false).GetAwaiter().GetResult();

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeOfType(typeof(GitHubSourceService));
        }

        [Fact]
        public void GetResultSourceService_ReturnsPlatformNotSupported_WhenPathDoesNotContainsDotGitDirectory()
        {
            string path = @"C:\Git\MyProject";

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);

            var mockGitExe = new Mock<IGitExe>();
            mockGitExe.Setup(g => g.GetRepoRootAsync()).Returns(new ValueTask<string>(path));

            var resultSourceFactory = new ResultSourceFactory(mockFileSystem.Object, mockGitExe.Object);
            Result<IResultSourceService, ErrorType> result = resultSourceFactory.GetResultSourceServiceAsync(path).ConfigureAwait(false).GetAwaiter().GetResult();

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(ErrorType.PlatformNotSupported);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using CSharpFunctionalExtensions;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Abstractions;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Services;
using Microsoft.Sarif.Viewer.ResultSources.Factory;
using Microsoft.Sarif.Viewer.Shell;

using Moq;

using Ninject;

using Xunit;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.UnitTests
{
    public class ResultSourceFactoryTests
    {
        [Fact]
        public void GetResultSourceService_ReturnsGitHubSourceService_WhenPathContainsDotGitDirectory()
        {
            string path = @"C:\Git\MyProject";
            string uri = "https://github.com/user/myproject.git";

            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockHttpClientAdapter = new Mock<IHttpClientAdapter>();
            var mockSecretStoreRepository = new Mock<ISecretStoreRepository>();
            var mockFileWatcher = new Mock<IFileWatcher>();

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);

            var mockGitExe = new Mock<IGitExe>();
            mockGitExe.Setup(g => g.GetRepoRootAsync()).Returns(new ValueTask<string>(path));
            mockGitExe.Setup(g => g.GetRepoUriAsync()).Returns(new ValueTask<string>(uri));

            var standardKernel = new StandardKernel();
            standardKernel.Bind<IServiceProvider>().ToConstant(mockServiceProvider.Object);
            standardKernel.Bind<IHttpClientAdapter>().ToConstant(mockHttpClientAdapter.Object);
            standardKernel.Bind<ISecretStoreRepository>().ToConstant(mockSecretStoreRepository.Object);
            standardKernel.Bind<IFileWatcher>().ToConstant(mockFileWatcher.Object);
            standardKernel.Bind<IFileSystem>().ToConstant(mockFileSystem.Object);
            standardKernel.Bind<IGitExe>().ToConstant(mockGitExe.Object);

            var resultSourceFactory = new ResultSourceFactory(path, standardKernel);
            Result<IResultSourceService, ErrorType> result = resultSourceFactory.GetResultSourceServiceAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.GetType().Name.Should().Be("GitHubSourceService");
        }

        [Fact]
        public void GetResultSourceService_ReturnsPlatformNotSupported_WhenPathDoesNotContainsDotGitDirectory()
        {
            string path = @"C:\Git\MyProject";
            string uri = "https://github.com/user/myproject.git";

            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockHttpClientAdapter = new Mock<IHttpClientAdapter>();
            var mockSecretStoreRepository = new Mock<ISecretStoreRepository>();
            var mockFileWatcher = new Mock<IFileWatcher>();

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);

            var mockGitExe = new Mock<IGitExe>();
            mockGitExe.Setup(g => g.GetRepoRootAsync()).Returns(new ValueTask<string>(path));
            mockGitExe.Setup(g => g.GetRepoUriAsync()).Returns(new ValueTask<string>(uri));

            var standardKernel = new StandardKernel();
            standardKernel.Bind<IServiceProvider>().ToConstant(mockServiceProvider.Object);
            standardKernel.Bind<IHttpClientAdapter>().ToConstant(mockHttpClientAdapter.Object);
            standardKernel.Bind<ISecretStoreRepository>().ToConstant(mockSecretStoreRepository.Object);
            standardKernel.Bind<IFileWatcher>().ToConstant(mockFileWatcher.Object);
            standardKernel.Bind<IFileSystem>().ToConstant(mockFileSystem.Object);
            standardKernel.Bind<IGitExe>().ToConstant(mockGitExe.Object);

            var resultSourceFactory = new ResultSourceFactory(path, standardKernel);
            Result<IResultSourceService, ErrorType> result = resultSourceFactory.GetResultSourceServiceAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(ErrorType.PlatformNotSupported);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using CSharpFunctionalExtensions;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ResultSources.Domain;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Abstractions;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Services;
using Microsoft.Sarif.Viewer.Shell;

using Moq;

using Ninject;

using Xunit;

using Result = CSharpFunctionalExtensions.Result;

namespace Microsoft.Sarif.Viewer.ResultSources.Factory.UnitTests
{
    public class ResultSourceFactoryTests
    {
        public ResultSourceFactoryTests()
        {
            ResultSourceFactory.IsUnitTesting = true;
        }

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
            mockGitExe.Setup(g => g.GetRepoRootAsync(null)).Returns(new ValueTask<string>(path));
            mockGitExe.Setup(g => g.GetRepoUriAsync(null)).Returns(new ValueTask<string>(uri));

            var mockInfoBarService = new Mock<IInfoBarService>();
            var mockStatusBarService = new Mock<IStatusBarService>();

            var standardKernel = new StandardKernel();
            standardKernel.Bind<IServiceProvider>().ToConstant(mockServiceProvider.Object);
            standardKernel.Bind<IHttpClientAdapter>().ToConstant(mockHttpClientAdapter.Object);
            standardKernel.Bind<ISecretStoreRepository>().ToConstant(mockSecretStoreRepository.Object);
            standardKernel.Bind<IFileWatcher>().ToConstant(mockFileWatcher.Object);
            standardKernel.Bind<IFileSystem>().ToConstant(mockFileSystem.Object);
            standardKernel.Bind<IGitExe>().ToConstant(mockGitExe.Object);
            standardKernel.Bind<IInfoBarService>().ToConstant(mockInfoBarService.Object);
            standardKernel.Bind<IStatusBarService>().ToConstant(mockStatusBarService.Object);

            var resultSourceFactory = new ResultSourceFactory(path, standardKernel, (string key) => true);
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
            mockGitExe.Setup(g => g.GetRepoRootAsync(null)).Returns(new ValueTask<string>(path));
            mockGitExe.Setup(g => g.GetRepoUriAsync(null)).Returns(new ValueTask<string>(uri));

            var mockInfoBarService = new Mock<IInfoBarService>();
            var mockStatusBarService = new Mock<IStatusBarService>();

            var standardKernel = new StandardKernel();
            standardKernel.Bind<IServiceProvider>().ToConstant(mockServiceProvider.Object);
            standardKernel.Bind<IHttpClientAdapter>().ToConstant(mockHttpClientAdapter.Object);
            standardKernel.Bind<ISecretStoreRepository>().ToConstant(mockSecretStoreRepository.Object);
            standardKernel.Bind<IFileWatcher>().ToConstant(mockFileWatcher.Object);
            standardKernel.Bind<IFileSystem>().ToConstant(mockFileSystem.Object);
            standardKernel.Bind<IGitExe>().ToConstant(mockGitExe.Object);
            standardKernel.Bind<IInfoBarService>().ToConstant(mockInfoBarService.Object);
            standardKernel.Bind<IStatusBarService>().ToConstant(mockStatusBarService.Object);

            var resultSourceFactory = new ResultSourceFactory(path, standardKernel, (string key) => true);
            Result<IResultSourceService, ErrorType> result = resultSourceFactory.GetResultSourceServiceAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(ErrorType.PlatformNotSupported);
        }

        [Fact]
        public async Task RequestAnalysisResults_RequestsResultsOnce_WhenSourceActive_Async()
        {
            var mockResultSource = new Mock<IResultSourceService>();
            mockResultSource.Setup(s => s.RequestAnalysisScanResultsAsync(null));

            var mockResultSourceFactory = new Mock<IResultSourceFactory>();
            mockResultSourceFactory.Setup(f => f.GetResultSourceServicesAsync()).Returns(Task.FromResult(Result.Success<IResultSourceService, ErrorType>(mockResultSource.Object)));

            var resultSourceHost = new ResultSourceHost(mockResultSourceFactory.Object);
            await resultSourceHost.RequestAnalysisResultsAsync();

            mockResultSource.Verify(s => s.RequestAnalysisScanResultsAsync(null), Times.Once);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using CSharpFunctionalExtensions;

using FluentAssertions;

using Microsoft.Alm.Authentication;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Abstractions;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Errors;
using Microsoft.Sarif.Viewer.ResultSources.GitHubAdvancedSecurity.Models;
using Microsoft.Sarif.Viewer.ResultSources.GitHubAdvancedSecurity.Services;
using Microsoft.Sarif.Viewer.Shell;

using Moq;

using Octokit;

using Xunit;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.UnitTests
{
    public class GitHubSourceServiceTests
    {
        [Fact]
        public async Task IsGitHubProject_ReturnsTrue_WhenPathContainsDotGitDirectory_Async()
        {
            string path = @"C:\Git\MyProject";

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);

            var mockGitExe = new Mock<IGitExe>();
            mockGitExe.Setup(g => g.GetRepoRootAsync()).Returns(new ValueTask<string>(path));

            var gitHubSourceService = new GitHubSourceService(It.IsAny<string>(), null, null, null, null, null, mockFileSystem.Object, mockGitExe.Object);
            bool result = await gitHubSourceService.IsActiveAsync();
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsGitHubProject_ReturnsFalse_WhenPathDoesNotContainsDotGitDirectory_Async()
        {
            string path = @"C:\Git\MyProject";

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);

            var mockGitExe = new Mock<IGitExe>();
            mockGitExe.Setup(g => g.GetRepoRootAsync()).Returns(new ValueTask<string>(path));

            var gitHubSourceService = new GitHubSourceService(It.IsAny<string>(), null, null, null, null, null, mockFileSystem.Object, mockGitExe.Object);
            bool result = await gitHubSourceService.IsActiveAsync();
            result.Should().BeFalse();
        }

        [Fact]
        public void ParseBranchString_ReturnsCorrectValue_WhenBranchHContainsNoSlashes()
        {
            string input = "my-branch";
            string expectedPath = string.Empty;
            string expectedName = "my-branch";

            var mockGitExe = new Mock<IGitExe>();

            var gitHubSourceService = new GitHubSourceService(It.IsAny<string>(), null, null, null, null, null, null, mockGitExe.Object);

            (string Path, string Name) result = gitHubSourceService.ParseBranchString(input);
            result.Path.Should().Be(expectedPath);
            result.Name.Should().Be(expectedName);
        }

        [Fact]
        public void ParseBranchString_ReturnsCorrectValue_WhenBranchHContainsOneSlashe()
        {
            string input = "project/my-branch";
            string expectedPath = "project";
            string expectedName = "my-branch";

            var mockGitExe = new Mock<IGitExe>();

            var gitHubSourceService = new GitHubSourceService(It.IsAny<string>(), null, null, null, null, null, null, mockGitExe.Object);

            (string Path, string Name) result = gitHubSourceService.ParseBranchString(input);
            result.Path.Should().Be(expectedPath);
            result.Name.Should().Be(expectedName);
        }

        [Fact]
        public void ParseBranchString_ReturnsCorrectValue_WhenBranchHContainsMultipleSlashes()
        {
            string input = "users/bob/my-branch";
            string expectedPath = @"users\bob";
            string expectedName = "my-branch";

            var mockGitExe = new Mock<IGitExe>();

            var gitHubSourceService = new GitHubSourceService(It.IsAny<string>(), null, null, null, null, null, null, mockGitExe.Object);

            (string Path, string Name) result = gitHubSourceService.ParseBranchString(input);
            result.Path.Should().Be(expectedPath);
            result.Name.Should().Be(expectedName);
        }

        [Fact]
        public async Task GetCachedSecret_ReturnsSecret_WhenCachedTokenExists_Async()
        {
            string path = @"C:\Git\MyProject";
            string uri = "https://github.com/user/myproject.git";
            string branch = "my-branch";
            var cachedSecret = new Entities.Secret { Value = "GITHUB-ACCESS-TOKEN" };

            var mockServiceProvider = new Mock<IServiceProvider>();

            var mockHttpClientAdapter = new Mock<IHttpClientAdapter>();

            var mockSecretStoreRepository = new Mock<ISecretStoreRepository>();
            mockSecretStoreRepository.Setup(r => r.ReadSecret(It.IsAny<TargetUri>())).Returns(cachedSecret);

            var mockFileWatcher = new Mock<IFileWatcher>();

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);

            var mockGitExe = new Mock<IGitExe>();
            mockGitExe.Setup(g => g.GetRepoRootAsync()).Returns(new ValueTask<string>(path));
            mockGitExe.Setup(g => g.GetRepoUriAsync()).Returns(new ValueTask<string>(uri));
            mockGitExe.Setup(g => g.GetCurrentBranchAsync()).Returns(new ValueTask<string>(branch));

            var gitHubSourceService = new GitHubSourceService(
                path,
                mockServiceProvider.Object,
                mockHttpClientAdapter.Object,
                mockSecretStoreRepository.Object,
                mockFileWatcher.Object,
                mockFileWatcher.Object,
                mockFileSystem.Object,
                mockGitExe.Object);

            await gitHubSourceService.InitializeAsync();

            var mockGitHubClient = new Mock<IGitHubClient>();
            mockGitHubClient.Setup(g => g.User.Current()).Returns(Task.FromResult(new User()));

            Maybe<Models.Secret> result = await gitHubSourceService.GetCachedAccessTokenAsync(mockGitHubClient.Object);
            result.HasValue.Should().BeTrue();
            result.Value.Value.Should().Be(cachedSecret.Value);
        }

        [Fact]
        public async Task GetCachedSecret_ReturnsNull_WhenCachedTokenIsInvalid_Async()
        {
            string path = @"C:\Git\MyProject";
            string uri = "https://github.com/user/myproject.git";
            string branch = "my-branch";
            var cachedSecret = new Entities.Secret { Value = "GITHUB-EXPIRED-ACCESS-TOKEN" };

            var mockServiceProvider = new Mock<IServiceProvider>();

            var mockHttpClientAdapter = new Mock<IHttpClientAdapter>();

            var mockSecretStoreRepository = new Mock<ISecretStoreRepository>();
            mockSecretStoreRepository.Setup(r => r.ReadSecret(It.IsAny<TargetUri>())).Returns(cachedSecret);
            mockSecretStoreRepository.Setup(r => r.DeleteSecret(It.IsAny<TargetUri>()));

            var mockFileWatcher = new Mock<IFileWatcher>();

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);

            var mockGitExe = new Mock<IGitExe>();
            mockGitExe.Setup(g => g.GetRepoRootAsync()).Returns(new ValueTask<string>(path));
            mockGitExe.Setup(g => g.GetRepoUriAsync()).Returns(new ValueTask<string>(uri));
            mockGitExe.Setup(g => g.GetCurrentBranchAsync()).Returns(new ValueTask<string>(branch));

            var gitHubSourceService = new GitHubSourceService(
                path,
                mockServiceProvider.Object,
                mockHttpClientAdapter.Object,
                mockSecretStoreRepository.Object,
                mockFileWatcher.Object,
                mockFileWatcher.Object,
                mockFileSystem.Object,
                mockGitExe.Object);

            await gitHubSourceService.InitializeAsync();

            var mockGitHubClient = new Mock<IGitHubClient>();
            mockGitHubClient.Setup(g => g.User.Current()).Throws<AuthorizationException>();

            Maybe<Models.Secret> result = await gitHubSourceService.GetCachedAccessTokenAsync(mockGitHubClient.Object);
            result.HasValue.Should().BeFalse();
            mockSecretStoreRepository.Verify(r => r.DeleteSecret(It.IsAny<TargetUri>()), Times.Once);
        }

        [Fact]
        public async Task GetRequestedSecret_ReturnsSecret_WhenReceivedFromGitHub_Async()
        {
            string path = @"C:\Git\MyProject";
            string uri = "https://github.com/user/myproject.git";
            string branch = "my-branch";
            string Secret = "gho_aCc3Ss70keN";

            var httpResponseMessage = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"{""access_token"": """ + Secret + @"""}")
            };

            var userVerificationResponse = new UserVerificationResponse()
            {
                DeviceCode = "ABCD-1234",
                ExpiresInSeconds = 100,
                PollingIntervalSeconds = 10
            };

            var mockServiceProvider = new Mock<IServiceProvider>();

            var mockHttpClientAdapter = new Mock<IHttpClientAdapter>();
            mockHttpClientAdapter.Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), CancellationToken.None)).ReturnsAsync(httpResponseMessage);

            var mockSecretStoreRepository = new Mock<ISecretStoreRepository>();
            mockSecretStoreRepository.Setup(r => r.WriteSecret(It.IsAny<TargetUri>(), It.IsAny<Entities.Secret>()));

            var mockFileWatcher = new Mock<IFileWatcher>();

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);

            var mockGitExe = new Mock<IGitExe>();
            mockGitExe.Setup(g => g.GetRepoRootAsync()).Returns(new ValueTask<string>(path));
            mockGitExe.Setup(g => g.GetRepoUriAsync()).Returns(new ValueTask<string>(uri));
            mockGitExe.Setup(g => g.GetCurrentBranchAsync()).Returns(new ValueTask<string>(branch));

            var gitHubSourceService = new GitHubSourceService(
                path,
                mockServiceProvider.Object,
                mockHttpClientAdapter.Object,
                mockSecretStoreRepository.Object,
                mockFileWatcher.Object,
                mockFileWatcher.Object,
                mockFileSystem.Object,
                mockGitExe.Object);

            await gitHubSourceService.InitializeAsync();

            Result<Models.Secret, Error> result = await gitHubSourceService.GetRequestedAccessTokenAsync(userVerificationResponse);

            result.IsSuccess.Should().BeTrue();
            result.Value.Value.Should().Be(Secret);
        }

        [Fact]
        public async Task GetRequestedSecret_Failes_WhenRequestTimesOut_Async()
        {
            string path = @"C:\Git\MyProject";
            string uri = "https://github.com/user/myproject.git";
            string branch = "my-branch";

            var httpResponseMessage = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent(@"{""error_description"": ""expired_token""}")
            };

            var userVerificationResponse = new UserVerificationResponse()
            {
                DeviceCode = "ABCD-1234",
                ExpiresInSeconds = 0,
                PollingIntervalSeconds = 0
            };

            var mockServiceProvider = new Mock<IServiceProvider>();

            var mockHttpClientAdapter = new Mock<IHttpClientAdapter>();
            mockHttpClientAdapter.Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), CancellationToken.None)).ReturnsAsync(httpResponseMessage);

            var mockSecretStoreRepository = new Mock<ISecretStoreRepository>();
            mockSecretStoreRepository.Setup(r => r.WriteSecret(It.IsAny<TargetUri>(), It.IsAny<Entities.Secret>()));

            var mockFileWatcher = new Mock<IFileWatcher>();

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);

            var mockGitExe = new Mock<IGitExe>();
            mockGitExe.Setup(g => g.GetRepoRootAsync()).Returns(new ValueTask<string>(path));
            mockGitExe.Setup(g => g.GetRepoUriAsync()).Returns(new ValueTask<string>(uri));
            mockGitExe.Setup(g => g.GetCurrentBranchAsync()).Returns(new ValueTask<string>(branch));

            var gitHubSourceService = new GitHubSourceService(
                path,
                mockServiceProvider.Object,
                mockHttpClientAdapter.Object,
                mockSecretStoreRepository.Object,
                mockFileWatcher.Object,
                mockFileWatcher.Object,
                mockFileSystem.Object,
                mockGitExe.Object);

            await gitHubSourceService.InitializeAsync();

            Result<Models.Secret, Error> result = await gitHubSourceService.GetRequestedAccessTokenAsync(userVerificationResponse);

            result.IsSuccess.Should().BeFalse();
            result.Error.Message.Should().Be("expired_token");
        }
    }
}

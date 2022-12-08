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
using Microsoft.Sarif.Viewer.ResultSources.Domain.Services;
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
        private const string GitHubAccessToken = "GITHUB-ACCESS-TOKEN";

        [Fact]
        public async Task IsActive_ReturnsTrue_WhenPathContainsDotGitDirectory_Async()
        {
            string path = @"C:\Git\MyProject";

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);

            var mockGitExe = new Mock<IGitExe>();
            mockGitExe.Setup(g => g.GetRepoRootAsync()).Returns(new ValueTask<string>(path));

            var gitHubSourceService = new GitHubSourceService(It.IsAny<string>(), null, null, null, null, null, mockFileSystem.Object, mockGitExe.Object, null, null);
            CSharpFunctionalExtensions.Result result = await gitHubSourceService.IsActiveAsync();
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task IsActive_ReturnsFalse_WhenPathDoesNotContainsDotGitDirectory_Async()
        {
            string path = @"C:\Git\MyProject";

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);

            var mockGitExe = new Mock<IGitExe>();
            mockGitExe.Setup(g => g.GetRepoRootAsync()).Returns(new ValueTask<string>(path));

            var gitHubSourceService = new GitHubSourceService(It.IsAny<string>(), null, null, null, null, null, mockFileSystem.Object, mockGitExe.Object, null, null);
            CSharpFunctionalExtensions.Result result = await gitHubSourceService.IsActiveAsync();
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public void ParseBranchString_ReturnsCorrectValue_WhenBranchHContainsNoSlashes()
        {
            string input = "my-branch";
            string expectedPath = string.Empty;
            string expectedName = "my-branch";

            var mockGitExe = new Mock<IGitExe>();

            var gitHubSourceService = new GitHubSourceService(It.IsAny<string>(), null, null, null, null, null, null, mockGitExe.Object, null, null);

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

            var gitHubSourceService = new GitHubSourceService(It.IsAny<string>(), null, null, null, null, null, null, mockGitExe.Object, null, null);

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

            var gitHubSourceService = new GitHubSourceService(It.IsAny<string>(), null, null, null, null, null, null, mockGitExe.Object, null, null);

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
            var cachedSecret = new Entities.Secret { Value = GitHubAccessToken };

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

            var mockInfoBarService = new Mock<IInfoBarService>();
            var mockStatusBarService = new Mock<IStatusBarService>();

            var gitHubSourceService = new GitHubSourceService(
                path,
                mockServiceProvider.Object,
                mockHttpClientAdapter.Object,
                mockSecretStoreRepository.Object,
                mockFileWatcher.Object,
                mockFileWatcher.Object,
                mockFileSystem.Object,
                mockGitExe.Object,
                mockInfoBarService.Object,
                mockStatusBarService.Object);

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

            var mockInfoBarService = new Mock<IInfoBarService>();
            var mockStatusBarService = new Mock<IStatusBarService>();

            var gitHubSourceService = new GitHubSourceService(
                path,
                mockServiceProvider.Object,
                mockHttpClientAdapter.Object,
                mockSecretStoreRepository.Object,
                mockFileWatcher.Object,
                mockFileWatcher.Object,
                mockFileSystem.Object,
                mockGitExe.Object,
                mockInfoBarService.Object,
                mockStatusBarService.Object);

            await gitHubSourceService.InitializeAsync();

            var mockGitHubClient = new Mock<IGitHubClient>();
            mockGitHubClient.Setup(g => g.User.Current()).Throws<AuthorizationException>();

            Maybe<Models.Secret> result = await gitHubSourceService.GetCachedAccessTokenAsync(mockGitHubClient.Object);
            result.HasValue.Should().BeFalse();
            mockSecretStoreRepository.Verify(r => r.DeleteSecret(It.IsAny<TargetUri>()), Times.Once);
        }

        [Fact]
        public async Task GetUserVerificationCode_Fails_WhenRequestFails_Async()
        {
            string path = @"C:\Git\MyProject";

            var httpResponseMessage = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(string.Empty)
            };

            var mockServiceProvider = new Mock<IServiceProvider>();

            var mockHttpClientAdapter = new Mock<IHttpClientAdapter>();
            mockHttpClientAdapter.Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), CancellationToken.None)).ReturnsAsync(httpResponseMessage);

            var mockSecretStoreRepository = new Mock<ISecretStoreRepository>();
            var mockFileWatcher = new Mock<IFileWatcher>();
            var mockFileSystem = new Mock<IFileSystem>();
            var mockGitExe = new Mock<IGitExe>();

            var mockInfoBarService = new Mock<IInfoBarService>();
            var mockStatusBarService = new Mock<IStatusBarService>();

            var gitHubSourceService = new GitHubSourceService(
                path,
                mockServiceProvider.Object,
                mockHttpClientAdapter.Object,
                mockSecretStoreRepository.Object,
                mockFileWatcher.Object,
                mockFileWatcher.Object,
                mockFileSystem.Object,
                mockGitExe.Object,
                mockInfoBarService.Object,
                mockStatusBarService.Object);

            Result<UserVerificationResponse, Error> result = await gitHubSourceService.GetUserVerificationCodeAsync();

            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task GetUserVerificationCode_Succeeds_WhenRequestRetursCode_Async()
        {
            string path = @"C:\Git\MyProject";

            var httpResponseMessage = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"{""device_code"": ""3584d83530557fdd1f46af8289938c8ef79f9dc5"",""expires_in"":900,""interval"":5,""user_code"":""WDJB-MJHT"",""verification_uri"":""https://github.com/login/device""}")
            };

            var expectedUserVerificationResponse = new UserVerificationResponse()
            {
                DeviceCode = "3584d83530557fdd1f46af8289938c8ef79f9dc5",
                ExpiresInSeconds = 900,
                PollingIntervalSeconds = 5,
                UserCode = "WDJB-MJHT",
                VerificationUri = "https://github.com/login/device"
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
            var mockInfoBarService = new Mock<IInfoBarService>();
            var mockStatusBarService = new Mock<IStatusBarService>();

            var gitHubSourceService = new GitHubSourceService(
                path,
                mockServiceProvider.Object,
                mockHttpClientAdapter.Object,
                mockSecretStoreRepository.Object,
                mockFileWatcher.Object,
                mockFileWatcher.Object,
                mockFileSystem.Object,
                mockGitExe.Object,
                mockInfoBarService.Object,
                mockStatusBarService.Object);

            Result<UserVerificationResponse, Error> result = await gitHubSourceService.GetUserVerificationCodeAsync();

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Should().BeEquivalentTo(expectedUserVerificationResponse);
        }

        [Fact]
        public async Task GetRequestedAccessToken_ReturnsAccessToken_WhenReceivedFromGitHub_Async()
        {
            string path = @"C:\Git\MyProject";
            string uri = "https://github.com/user/myproject.git";
            string branch = "my-branch";

            var httpResponseMessage = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"{""access_token"": """ + GitHubAccessToken + @"""}")
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

            var mockInfoBarService = new Mock<IInfoBarService>();
            var mockStatusBarService = new Mock<IStatusBarService>();

            var gitHubSourceService = new GitHubSourceService(
                path,
                mockServiceProvider.Object,
                mockHttpClientAdapter.Object,
                mockSecretStoreRepository.Object,
                mockFileWatcher.Object,
                mockFileWatcher.Object,
                mockFileSystem.Object,
                mockGitExe.Object,
                mockInfoBarService.Object,
                mockStatusBarService.Object);

            await gitHubSourceService.InitializeAsync();

            Result<Models.Secret, Error> result = await gitHubSourceService.GetRequestedAccessTokenAsync(userVerificationResponse);

            result.IsSuccess.Should().BeTrue();
            result.Value.Value.Should().Be(GitHubAccessToken);
        }

        [Fact]
        public async Task GetRequestedAccessToken_Fails_WhenRequestTimesOut_Async()
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

            var mockInfoBarService = new Mock<IInfoBarService>();
            var mockStatusBarService = new Mock<IStatusBarService>();

            var gitHubSourceService = new GitHubSourceService(
                path,
                mockServiceProvider.Object,
                mockHttpClientAdapter.Object,
                mockSecretStoreRepository.Object,
                mockFileWatcher.Object,
                mockFileWatcher.Object,
                mockFileSystem.Object,
                mockGitExe.Object,
                mockInfoBarService.Object,
                mockStatusBarService.Object);

            await gitHubSourceService.InitializeAsync();

            Result<Models.Secret, Error> result = await gitHubSourceService.GetRequestedAccessTokenAsync(userVerificationResponse);

            result.IsSuccess.Should().BeFalse();
            result.Error.Message.Should().Be("expired_token");
        }

        [Fact]
        public async Task GetAnalysisIdAsync_Fails_WhenRequestFails_Async()
        {
            string path = @"C:\Git\MyProject";
            string uri = "https://api.github.com/repos/user/myproject/code-scanning/analyses";
            string branch = "my-branch";

            var httpResponseMessage = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.NotFound
            };

            var mockServiceProvider = new Mock<IServiceProvider>();

            var mockHttpClientAdapter = new Mock<IHttpClientAdapter>();
            mockHttpClientAdapter.Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), CancellationToken.None)).ReturnsAsync(httpResponseMessage);

            var mockSecretStoreRepository = new Mock<ISecretStoreRepository>();
            var mockFileWatcher = new Mock<IFileWatcher>();
            var mockFileSystem = new Mock<IFileSystem>();
            var mockGitExe = new Mock<IGitExe>();
            var mockInfoBarService = new Mock<IInfoBarService>();
            var mockStatusBarService = new Mock<IStatusBarService>();

            var gitHubSourceService = new GitHubSourceService(
                path,
                mockServiceProvider.Object,
                mockHttpClientAdapter.Object,
                mockSecretStoreRepository.Object,
                mockFileWatcher.Object,
                mockFileWatcher.Object,
                mockFileSystem.Object,
                mockGitExe.Object,
                mockInfoBarService.Object,
                mockStatusBarService.Object);

            Result<string, ErrorType> result = await gitHubSourceService.GetAnalysisIdAsync(
                mockHttpClientAdapter.Object,
                uri,
                branch,
                GitHubAccessToken,
                null // commitHash
            );

            result.Error.Should().Be(ErrorType.AnalysesUnavailable);
        }

        [Fact]
        public async Task GetAnalysisIdAsync_WithNoCommitHash_Fails_WhenNoResultsReturned_Async()
        {
            string path = @"C:\Git\MyProject";
            string uri = "https://api.github.com/repos/user/myproject/code-scanning/analyses";
            string branch = "my-branch";

            var httpResponseMessage = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[]")
            };

            var mockServiceProvider = new Mock<IServiceProvider>();

            var mockHttpClientAdapter = new Mock<IHttpClientAdapter>();
            mockHttpClientAdapter.Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), CancellationToken.None)).ReturnsAsync(httpResponseMessage);

            var mockSecretStoreRepository = new Mock<ISecretStoreRepository>();
            var mockFileWatcher = new Mock<IFileWatcher>();
            var mockFileSystem = new Mock<IFileSystem>();
            var mockGitExe = new Mock<IGitExe>();
            var mockInfoBarService = new Mock<IInfoBarService>();
            var mockStatusBarService = new Mock<IStatusBarService>();

            var gitHubSourceService = new GitHubSourceService(
                path,
                mockServiceProvider.Object,
                mockHttpClientAdapter.Object,
                mockSecretStoreRepository.Object,
                mockFileWatcher.Object,
                mockFileWatcher.Object,
                mockFileSystem.Object,
                mockGitExe.Object,
                mockInfoBarService.Object,
                mockStatusBarService.Object);

            Result<string, ErrorType> result = await gitHubSourceService.GetAnalysisIdAsync(
                mockHttpClientAdapter.Object,
                uri,
                branch,
                GitHubAccessToken,
                null // commitHash
            );

            result.Error.Should().Be(ErrorType.AnalysesUnavailable);
        }

        [Fact]
        public async Task GetAnalysisIdAsync_WithCommitHash_Fails_WhenNoResultsReturned_Async()
        {
            string path = @"C:\Git\MyProject";
            string uri = "https://api.github.com/repos/user/myproject/code-scanning/analyses";
            string branch = "my-branch";
            string commitHash = "64ab23c";

            var httpResponseMessage = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"[{""commit_sha"":""eeee123""}]")
            };

            var mockServiceProvider = new Mock<IServiceProvider>();

            var mockHttpClientAdapter = new Mock<IHttpClientAdapter>();
            mockHttpClientAdapter.Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), CancellationToken.None)).ReturnsAsync(httpResponseMessage);

            var mockSecretStoreRepository = new Mock<ISecretStoreRepository>();
            var mockFileWatcher = new Mock<IFileWatcher>();
            var mockFileSystem = new Mock<IFileSystem>();
            var mockGitExe = new Mock<IGitExe>();
            var mockInfoBarService = new Mock<IInfoBarService>();
            var mockStatusBarService = new Mock<IStatusBarService>();

            var gitHubSourceService = new GitHubSourceService(
                path,
                mockServiceProvider.Object,
                mockHttpClientAdapter.Object,
                mockSecretStoreRepository.Object,
                mockFileWatcher.Object,
                mockFileWatcher.Object,
                mockFileSystem.Object,
                mockGitExe.Object,
                mockInfoBarService.Object,
                mockStatusBarService.Object);

            Result<string, ErrorType> result = await gitHubSourceService.GetAnalysisIdAsync(
                mockHttpClientAdapter.Object,
                uri,
                branch,
                GitHubAccessToken,
                commitHash);

            result.Error.Should().Be(ErrorType.AnalysesUnavailable);
        }

        [Fact]
        public async Task GetAnalysisIdAsync_WithNoCommitHash_Succeeds_WhenResultsReturned_Async()
        {
            string path = @"C:\Git\MyProject";
            string uri = "https://api.github.com/repos/user/myproject/code-scanning/analyses";
            string branch = "my-branch";
            string expectedAnalysisId = "321";

            var httpResponseMessage = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"[{""commit_sha"":""64ab23c"",""id"":""321""},{""commit_sha"":""2345abc"",""id"":""320""}]")
            };

            var mockServiceProvider = new Mock<IServiceProvider>();

            var mockHttpClientAdapter = new Mock<IHttpClientAdapter>();
            mockHttpClientAdapter.Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), CancellationToken.None)).ReturnsAsync(httpResponseMessage);

            var mockSecretStoreRepository = new Mock<ISecretStoreRepository>();
            var mockFileWatcher = new Mock<IFileWatcher>();
            var mockFileSystem = new Mock<IFileSystem>();
            var mockGitExe = new Mock<IGitExe>();
            var mockInfoBarService = new Mock<IInfoBarService>();
            var mockStatusBarService = new Mock<IStatusBarService>();

            var gitHubSourceService = new GitHubSourceService(
                path,
                mockServiceProvider.Object,
                mockHttpClientAdapter.Object,
                mockSecretStoreRepository.Object,
                mockFileWatcher.Object,
                mockFileWatcher.Object,
                mockFileSystem.Object,
                mockGitExe.Object,
                mockInfoBarService.Object,
                mockStatusBarService.Object);

            Result<string, ErrorType> result = await gitHubSourceService.GetAnalysisIdAsync(
                mockHttpClientAdapter.Object,
                uri,
                branch,
                GitHubAccessToken,
                null //commitHash
            );

            result.IsSuccess.Should().Be(true);
            result.Value.Should().Be(expectedAnalysisId);
        }

        [Fact]
        public async Task GetAnalysisIdAsync_WithCommitHash_Succeeds_WhenResultsReturned_Async()
        {
            string path = @"C:\Git\MyProject";
            string uri = "https://api.github.com/repos/user/myproject/code-scanning/analyses";
            string branch = "my-branch";
            string commitHash = "64ab23c";
            string expectedAnalysisId = "321";

            var httpResponseMessage = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"[{""commit_sha"":""64ab23c"",""id"":""321""}]")
            };

            var mockServiceProvider = new Mock<IServiceProvider>();

            var mockHttpClientAdapter = new Mock<IHttpClientAdapter>();
            mockHttpClientAdapter.Setup(h => h.SendAsync(It.IsAny<HttpRequestMessage>(), CancellationToken.None)).ReturnsAsync(httpResponseMessage);

            var mockSecretStoreRepository = new Mock<ISecretStoreRepository>();
            var mockFileWatcher = new Mock<IFileWatcher>();
            var mockFileSystem = new Mock<IFileSystem>();
            var mockGitExe = new Mock<IGitExe>();
            var mockInfoBarService = new Mock<IInfoBarService>();
            var mockStatusBarService = new Mock<IStatusBarService>();

            var gitHubSourceService = new GitHubSourceService(
                path,
                mockServiceProvider.Object,
                mockHttpClientAdapter.Object,
                mockSecretStoreRepository.Object,
                mockFileWatcher.Object,
                mockFileWatcher.Object,
                mockFileSystem.Object,
                mockGitExe.Object,
                mockInfoBarService.Object,
                mockStatusBarService.Object);

            Result<string, ErrorType> result = await gitHubSourceService.GetAnalysisIdAsync(
                mockHttpClientAdapter.Object,
                uri,
                branch,
                GitHubAccessToken,
                commitHash);

            result.IsSuccess.Should().Be(true);
            result.Value.Should().Be(expectedAnalysisId);
        }
    }
}

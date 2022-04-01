// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using CSharpFunctionalExtensions;

using FluentAssertions;

using Microsoft.Alm.Authentication;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Abstractions;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Services.GitHub;
using Microsoft.Sarif.Viewer.Shell;

using Moq;

using Octokit;

using Sarif.Viewer.VisualStudio.Shell.Core;

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

            var gitHubSourceService = new GitHubSourceService(path, mockFileSystem.Object, mockGitExe.Object);
            bool result = await gitHubSourceService.IsGitHubProjectAsync();
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

            var gitHubSourceService = new GitHubSourceService(path, mockFileSystem.Object, mockGitExe.Object);
            bool result = await gitHubSourceService.IsGitHubProjectAsync();
            result.Should().BeFalse();
        }

        [Fact]
        public void ParseBranchString_ReturnsCorrectValue_WhenBranchHContainsNoSlashes()
        {
            string input = "my-branch";
            string expectedPath = string.Empty;
            string expectedName = "my-branch";

            var mockGitExe = new Mock<IGitExe>();

            var gitHubSourceService = new GitHubSourceService(It.IsAny<string>(), null, mockGitExe.Object);

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

            var gitHubSourceService = new GitHubSourceService(It.IsAny<string>(), null, mockGitExe.Object);

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

            var gitHubSourceService = new GitHubSourceService(It.IsAny<string>(), null, mockGitExe.Object);

            (string Path, string Name) result = gitHubSourceService.ParseBranchString(input);
            result.Path.Should().Be(expectedPath);
            result.Name.Should().Be(expectedName);
        }

        [Fact]
        public async Task GetCachedAccessToken_ReturnsAccessToken_WhenCachedTokenExists_Async()
        {
            string path = @"C:\Git\MyProject";
            string uri = "https://github.com/user/repo.git";
            string branch = "my-branch";
            var cachedAccessToken = new Entities.AccessToken { Value = "GITHUB-ACCESS-TOKEN" };

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);

            var mockGitExe = new Mock<IGitExe>();
            mockGitExe.Setup(g => g.GetRepoRootAsync()).Returns(new ValueTask<string>(path));
            mockGitExe.Setup(g => g.GetRepoUriAsync()).Returns(new ValueTask<string>(uri));
            mockGitExe.Setup(g => g.GetCurrentBranchAsync()).Returns(new ValueTask<string>(branch));

            var gitHubSourceService = new GitHubSourceService(path, mockFileSystem.Object, mockGitExe.Object);

            var mockServiceProvider = new Mock<IServiceProvider>();

            var mockSecretStoreRepository = new Mock<ISecretStoreRepository>();
            mockSecretStoreRepository.Setup(r => r.ReadAccessToken(It.IsAny<TargetUri>())).Returns(cachedAccessToken);
            
            var mockFileWatcher = new Mock<IFileWatcher>();

            await gitHubSourceService.InitializeAsync(
                mockServiceProvider.Object,
                mockSecretStoreRepository.Object,
                mockFileWatcher.Object,
                mockFileWatcher.Object);

            var mockGitHubClient = new Mock<IGitHubClient>();
            mockGitHubClient.Setup(g => g.User.Current()).Returns(Task.FromResult(new User()));

            Maybe<Models.AccessToken> result = await gitHubSourceService.GetCachedAccessTokenAsync(mockGitHubClient.Object);
            result.HasValue.Should().BeTrue();
            result.Value.Value.Should().Be(cachedAccessToken.Value);
        }

        [Fact]
        public async Task GetCachedAccessToken_ReturnsNull_WhenCachedTokenIsInvalid_Async()
        {
            string path = @"C:\Git\MyProject";
            string uri = "https://github.com/user/repo.git";
            string branch = "my-branch";

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);

            var mockGitExe = new Mock<IGitExe>();
            mockGitExe.Setup(g => g.GetRepoRootAsync()).Returns(new ValueTask<string>(path));
            mockGitExe.Setup(g => g.GetRepoUriAsync()).Returns(new ValueTask<string>(uri));
            mockGitExe.Setup(g => g.GetCurrentBranchAsync()).Returns(new ValueTask<string>(branch));

            var gitHubSourceService = new GitHubSourceService(path, mockFileSystem.Object, mockGitExe.Object);

            var mockServiceProvider = new Mock<IServiceProvider>();

            var mockSecretStoreRepository = new Mock<ISecretStoreRepository>();
            mockSecretStoreRepository.Setup(r => r.DeleteAccessToken(It.IsAny<TargetUri>()));

            var mockFileWatcher = new Mock<IFileWatcher>();

            await gitHubSourceService.InitializeAsync(
                mockServiceProvider.Object,
                mockSecretStoreRepository.Object,
                mockFileWatcher.Object,
                mockFileWatcher.Object); // .ConfigureAwait(false).GetAwaiter();

            var mockGitHubClient = new Mock<IGitHubClient>();
            mockGitHubClient.Setup(g => g.User.Current()).Throws<AuthorizationException>();

            Maybe<Models.AccessToken> result = await gitHubSourceService.GetCachedAccessTokenAsync(mockGitHubClient.Object); // .ConfigureAwait(false).GetAwaiter().GetResult();
            result.HasValue.Should().BeFalse();
        }
    }
}

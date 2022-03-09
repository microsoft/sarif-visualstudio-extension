// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using CSharpFunctionalExtensions;

using FluentAssertions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Abstractions;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Services;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Services.GitHub;

using Moq;

using Xunit;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.UnitTests
{
    public class ResultSourceServiceTests
    {
        [Fact]
        public void GetResultSourceService_ReturnsGitHubSourceService_WhenPathContainsDotGitDirectory()
        {
            string path = @"C:\Git\MyProject";

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);

            var mockServiceProvider = new Mock<IServiceProvider>();

            var mockSecretSetoreRepo = new Mock<ISecretStoreRepository>();

            var resultSourceService = new ResultSourceService(mockServiceProvider.Object, mockSecretSetoreRepo.Object);
            Result<IResultSourceService, ErrorType> result = resultSourceService.GetResultSourceService(path);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeOfType(typeof(GitHubSourceService));
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using CSharpFunctionalExtensions;

using Microsoft.Sarif.Viewer.ResultSources.Domain.Abstractions;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Errors;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Models;
using Microsoft.Sarif.Viewer.Shell;

using Octokit;

using Sarif.Viewer.VisualStudio.ResultSources.Domain.Core;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Services.GitHub
{
    public interface IGitHubSourceService : IResultSourceService
    {
        /// <summary>
        /// Initializes the service instance.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="secretStoreRepository">The <see cref="ISecretStoreRepository"/>.</param>
        /// <param name="fileWatcherBranchChange">The file watcher for Git branch changes.</param>
        /// <param name="fileWatcherGitPush">The file watcher for Git pushes.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task InitializeAsync(
               IServiceProvider serviceProvider,
               ISecretStoreRepository secretStoreRepository,
               IFileWatcher fileWatcherBranchChange,
               IFileWatcher fileWatcherGitPush);

        /// <summary>
        /// Determines if the current repo is hosted by GitHub.
        /// </summary>
        /// <returns>True if the current repo is hosted by GitHub; otherwise, false.</returns>
        Task<bool> IsGitHubProjectAsync();

        /// <summary>
        /// Requests a user verification code.
        /// </summary>
        /// <param name="httpClientAdapter">The <see cref="IHttpClientAdapter"/>.</param>
        /// <returns>The response data.</returns>
        Task<Result<UserVerificationResponse, Error>> GetUserVerificationCodeAsync(IHttpClientAdapter httpClientAdapter);

        /// <summary>
        /// Requests the access token from the secure store.
        /// </summary>
        /// <param name="gitHubClient">The <see cref="IGitHubClient"/>.</param>
        /// <returns>The access token, if found; otherwise, null.</returns>
        Task<Maybe<Models.AccessToken>> GetCachedAccessTokenAsync(IGitHubClient gitHubClient = null);

        /// <summary>
        /// Polls the GitHub API for the user-authorized access token.
        /// </summary>
        /// <param name="httpClientAdapter">The <see cref="IHttpClientAdapter"/>.</param>
        /// <param name="verificationResponse">The response data received from the user verification code request.</param>
        /// <returns>The access token if successful; otherwise, an error.</returns>
        Task<Result<Models.AccessToken, Error>> GetRequestedAccessTokenAsync(IHttpClientAdapter httpClientAdapter, UserVerificationResponse verificationResponse);
    }
}

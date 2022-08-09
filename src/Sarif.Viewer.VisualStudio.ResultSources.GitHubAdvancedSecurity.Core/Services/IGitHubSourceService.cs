// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using CSharpFunctionalExtensions;

using Microsoft.Sarif.Viewer.ResultSources.Domain.Errors;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Models;
using Microsoft.Sarif.Viewer.ResultSources.GitHubAdvancedSecurity.Models;

using Octokit;

namespace Microsoft.Sarif.Viewer.ResultSources.GitHubAdvancedSecurity.Services
{
    /// <summary>
    /// Provides a result source service for GitHub Advanced Security.
    /// </summary>
    public interface IGitHubSourceService
    {
        /// <summary>
        /// Requests a user verification code.
        /// </summary>
        /// <returns>The response data.</returns>
        Task<Result<UserVerificationResponse, Error>> GetUserVerificationCodeAsync();

        /// <summary>
        /// Requests the access token from the secure store.
        /// </summary>
        /// <param name="gitHubClient">The <see cref="IGitHubClient"/>.</param>
        /// <returns>The access token, if found; otherwise, null.</returns>
        Task<Maybe<Secret>> GetCachedAccessTokenAsync(IGitHubClient gitHubClient = null);

        /// <summary>
        /// Polls the GitHub API for the user-authorized access token.
        /// </summary>
        /// <param name="verificationResponse">The response data received from the user verification code request.</param>
        /// <returns>The access token if successful; otherwise, an error.</returns>
        Task<Result<Secret, Error>> GetRequestedAccessTokenAsync(UserVerificationResponse verificationResponse);
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using CSharpFunctionalExtensions;

using Microsoft.Sarif.Viewer.ResultSources.Domain.Errors;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Models;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Services.GitHub
{
    public interface IGitHubSourceService : IResultSourceService
    {
        /// <summary>
        /// Determines if the current repo is hosted by GitHub.
        /// </summary>
        /// <returns>True if the current repo is hosted by GitHub; otherwise, false.</returns>
        bool IsGitHubProject();

        /// <summary>
        /// Requests a user verification code.
        /// </summary>
        /// <returns>The response data.</returns>
        Task<Result<UserVerificationResponse, Error>> GetUserVerificationCodeAsync();

        /// <summary>
        /// Requests the access token from the secure store.
        /// </summary>
        /// <returns>The access token, if found; otherwise, null.</returns>
        Task<Maybe<AccessToken>> GetCachedAccessTokenAsync();

        /// <summary>
        /// Polls the GitHub API for the user-authorized access token.
        /// </summary>
        /// <param name="verificationResponse">The response data received from the user verification code request.</param>
        /// <returns>The access token if successful; otherwise, an error.</returns>
        Task<Result<AccessToken, Error>> GetRequestedAccessTokenAsync(UserVerificationResponse verificationResponse);
    }
}

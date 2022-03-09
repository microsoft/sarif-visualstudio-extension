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
        bool IsGitHubProject();

        Task<Result<UserVerificationResponse, Error>> GetUserVerificationCodeAsync();

        Task<Maybe<AccessToken>> GetCachedAccessTokenAsync();

        Task<Result<AccessToken, Error>> GetRequestedAccessTokenAsync(UserVerificationResponse verificationResponse);

        // ValueTask<string> GetRepoUriAsync();

        // ValueTask<string> GetCurrentBranchAsync();
    }
}

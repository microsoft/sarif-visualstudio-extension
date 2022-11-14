// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using CSharpFunctionalExtensions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ResultSources.AzureDevOps.Entities;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Services;

namespace Microsoft.Sarif.Viewer.ResultSources.AzureDevOps.Services
{
    public interface IAzureDevOpsResultSourceService
    {
        /// <summary>
        /// Gets the solution's ADO <see cref="GitRepository"/>.
        /// </summary>
        /// <returns>The repository.</returns>
        Task<Maybe<GitRepository>> GetRepositoryAsync();

        /// <summary>
        /// Gets the build ID for the current commit hash.
        /// </summary>
        /// <returns>The build ID.</returns>
        Task<Result<int, ErrorType>> GetBuildIdAsync();

        /// <summary>
        /// Downloads and extracts the static analysis results artifact for the specified build.
        /// </summary>
        /// <param name="buildId">The build ID.</param>
        /// <returns>The static analysis results artifact.</returns>
        Task<Maybe<SarifLog>> DownloadAndExtractArtifactAsync(int buildId);
    }
}

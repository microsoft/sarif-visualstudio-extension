// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using CSharpFunctionalExtensions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Services;

namespace Microsoft.Sarif.Viewer.ResultSources.AzureDevOps.Services
{
    public interface IAdvSecForAdoResultSourceService
    {
        /// <summary>
        /// Gets the latest build ID from the static analysis pipeline.
        /// </summary>
        /// <returns>The latest build ID.</returns>
        Task<Result<int, ErrorType>> GetLatestBuildIdAsync();

        /// <summary>
        /// Downloads and extracts the static analysis results artifact for the specified build.
        /// </summary>
        /// <param name="buildId">The build ID.</param>
        /// <returns>The static analysis results artifact.</returns>
        Task<Maybe<SarifLog>> DownloadAndExtractArtifactAsync(int buildId);
    }
}

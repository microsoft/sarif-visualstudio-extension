// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using CSharpFunctionalExtensions;

using Microsoft.Sarif.Viewer.ResultSources.Domain.Services;

namespace Microsoft.Sarif.Viewer.ResultSources.AdvancedSecurityForAdo.Services
{
    public interface IAdvSecForAdoResultSourceService
    {
        /// <summary>
        /// Gets the latest build ID from the static analysis pipeline.
        /// </summary>
        /// <returns>The latest build ID.</returns>
        Task<Result<string, ErrorType>> GetLatestBuildIdAsync();

        /// <summary>
        /// Gets the download URL for the static analysis results artifact for the specified build.
        /// </summary>
        /// <param name="buildId">The build ID.</param>
        /// <returns>The download URL for the static analysis results artifact.</returns>
        Task<Result<string, ErrorType>> GetArtifactDownloadUrlAsync(string buildId);
    }
}

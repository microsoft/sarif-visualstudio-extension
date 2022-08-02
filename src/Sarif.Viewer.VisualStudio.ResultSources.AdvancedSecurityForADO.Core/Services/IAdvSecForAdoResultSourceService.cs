// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.Sarif.Viewer.ResultSources.AdvancedSecurityForAdo.Services
{
    public interface IAdvSecForAdoResultSourceService
    {
        /// <summary>
        /// Gets the latest build ID from the static analysis pipeline.
        /// </summary>
        /// <returns>The latest build ID.</returns>
        Task<string> GetLatestBuildIdAsync();

        /// <summary>
        /// Gets the download URL for the static analysis results artifact for the specified build.
        /// </summary>
        /// <param name="buildId">The build ID.</param>
        /// <returns>The download URL for the static analysis results artifact.</returns>
        Task<string> GetArtifactDownloadUrlAsync(string buildId);
    }
}

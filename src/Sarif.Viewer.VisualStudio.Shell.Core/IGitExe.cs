// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.Sarif.Viewer.Shell
{
    public interface IGitExe
    {
        /// <summary>
        /// Gets or sets the full path of a folder anywhere within the repo.
        /// </summary>
        string RepoPath { get; set; }

        /// <summary>
        /// Gets the root repo path of current solution.
        /// </summary>
        /// <param name="filePath">The file path we want the repo root of.</param>
        /// <returns>The root repo path.</returns>
        ValueTask<string> GetRepoRootAsync(string filePath = null);

        /// <summary>
        /// Gets the root repo URI of current solution.
        /// </summary>
        /// <param name="filePath">The file path we want the repo uri of.</param>
        /// <returns>The root repo path.</returns>
        ValueTask<string> GetRepoUriAsync(string filePath = null);

        /// <summary>
        /// Gets current repo branch name.
        /// </summary>
        /// <param name="filePath">The file path we want the repo branch name of.</param>
        /// <returns>The current branch name.</returns>
        ValueTask<string> GetCurrentBranchAsync(string filePath = null);

        /// <summary>
        /// Gets the current repo commit hash..
        /// </summary>
        /// <param name="filePath">The file path we want the commit hash of.</param>
        /// <returns>The current commit hash.</returns>
        ValueTask<string> GetCurrentCommitHashAsync(string filePath = null);
    }
}

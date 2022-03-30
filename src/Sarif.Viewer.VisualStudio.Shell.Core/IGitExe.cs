// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Sarif.Viewer.VisualStudio.Shell.Core
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
        /// <returns>The root repo path.</returns>
        ValueTask<string> GetRepoRootAsync();

        /// <summary>
        /// Gets the root repo URI of current solution.
        /// </summary>
        /// <returns>The root repo path.</returns>
        ValueTask<string> GetRepoUriAsync();

        /// <summary>
        /// Gets current repo branch name.
        /// </summary>
        /// <returns>The current branch name.</returns>
        ValueTask<string> GetCurrentBranchAsync();

        /// <summary>
        /// Gets the current repo commit hash..
        /// </summary>
        /// <returns>The current commit hash.</returns>
        ValueTask<string> GetCurrentCommitHashAsync();
    }
}

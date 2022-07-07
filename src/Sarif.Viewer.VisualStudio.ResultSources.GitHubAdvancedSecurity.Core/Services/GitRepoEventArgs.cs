// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Sarif.Viewer.ResultSources.Domain.Models;

namespace Microsoft.Sarif.Viewer.ResultSources.GitHubAdvancedSecurity.Services
{
    internal class GitRepoEventArgs : ResultsUpdatedEventArgs
    {
        public string BranchName { get; set; }
    }
}

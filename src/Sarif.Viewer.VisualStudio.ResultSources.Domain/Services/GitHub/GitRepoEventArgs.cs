// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Services.GitHub
{
    internal class GitRepoEventArgs : EventArgs
    {
        public string BranchName { get; set; }
    }
}

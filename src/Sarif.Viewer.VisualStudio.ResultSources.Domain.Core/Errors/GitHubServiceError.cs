// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Errors
{
    public class GitHubServiceError : Error
    {
        public GitHubServiceError(string message)
            : base(message)
        {
        }
    }
}

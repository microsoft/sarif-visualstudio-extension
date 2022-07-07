// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Sarif.Viewer.ResultSources.Domain.Errors;

namespace Microsoft.Sarif.Viewer.ResultSources.GitHubAdvancedSecurity.Errors
{
    /// <summary>
    /// Represents a GitHub service error.
    /// </summary>
    public class GitHubServiceError : Error
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubServiceError"/> class.
        /// </summary>
        /// <param name="message">The error messasge.</param>
        public GitHubServiceError(string message)
            : base(message)
        {
        }
    }
}

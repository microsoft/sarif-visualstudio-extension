// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Services
{
    public enum ErrorType
    {
        /// <summary>
        /// The project build platform is not supported.
        /// </summary>
        PlatformNotSupported = 0,

        /// <summary>
        /// An disallowed null argument was detected.
        /// </summary>
        ArgumentNull = 1,

        /// <summary>
        /// The specified directory was not found.
        /// </summary>
        DirectoryNotFound = 2,

        /// <summary>
        /// The path exceeded the maximum length.
        /// </summary>
        PathTooLong = 3,

        /// <summary>
        /// Access to a resource was denied.
        /// </summary>
        AccessDenied = 4,

        /// <summary>
        /// An access token ws not found.
        /// </summary>
        MissingAccessToken = 5,

        /// <summary>
        /// An unknown access token error occurred.
        /// </summary>
        UnknownAccessTokenError = 6,

        /// <summary>
        /// The specified repo URL was not compatible with the result service.
        /// </summary>
        IncompatibleRepoUrl = 7,

        /// <summary>
        /// An access token has been requested and we are awaiting user response.
        /// </summary>
        WaitingForUserVerification = 8,
    }
}

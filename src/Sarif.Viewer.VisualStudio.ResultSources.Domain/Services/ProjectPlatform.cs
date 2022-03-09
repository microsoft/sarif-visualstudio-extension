// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Services
{
    public enum ProjectPlatform
    {
        /// <summary>
        /// The project platform is not supported.
        /// </summary>
        Unsupported = 0,

        /// <summary>
        /// GitHub platform.
        /// </summary>
        GitHub = 1,
    }
}

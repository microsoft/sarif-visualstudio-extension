// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Sarif.Viewer
{
    /// <summary>
    /// Constants specifying various exceptional conditions that can occur in a SARIF log file.
    /// </summary>
    [Flags]
    internal enum ExceptionalConditions
    {
        /// <summary>
        /// No exceptional conditions were detected in the log file.
        /// </summary>
        None = 0,

        /// <summary>
        /// The log file contained no results.
        /// </summary>
        NoResults = 0x00000001,

        /// <summary>
        /// The log file was not valid JSON.
        /// </summary>
        InvalidJson = 0x00000002,

        /// <summary>
        /// The log file contained at least one error-level tool configuration notification.
        /// </summary>
        ConfigurationError = 0x00000004,

        /// <summary>
        /// The log file contained at least one error-level tool execution notification.
        /// </summary>
        ExecutionError = 0x00000008,
    }
}

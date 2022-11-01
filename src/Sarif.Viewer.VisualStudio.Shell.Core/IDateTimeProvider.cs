// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Sarif.Viewer.Shell
{
    /// <summary>
    /// Interface for getting <see cref="DateTime"/> related data.
    /// </summary>
    public interface IDateTimeProvider
    {
        /// <summary>
        /// Gets a <see cref="T:System.DateTime" /> object that is set to the current date and time on this computer, expressed as the Coordinated Universal Time (UTC).
        /// </summary>
        DateTime UtcNow { get; }
    }
}

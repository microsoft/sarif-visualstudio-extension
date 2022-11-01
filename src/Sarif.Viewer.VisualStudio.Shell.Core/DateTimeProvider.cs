// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Sarif.Viewer.Shell
{
    /// <summary>
    /// Implements the <see cref="IDateTimeProvider"/> interface.
    /// </summary>
    public class DateTimeProvider : IDateTimeProvider
    {
        /// <inheritdoc cref="IDateTimeProvider"/>
        public DateTime UtcNow => DateTime.UtcNow;
    }
}

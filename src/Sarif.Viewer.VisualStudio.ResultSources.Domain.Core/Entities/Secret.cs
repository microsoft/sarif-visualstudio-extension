// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Entities
{
    /// <summary>
    /// Represents a secret string.
    /// </summary>
    public class Secret
    {
        /// <summary>
        /// Gets or sets the secret string.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the time when the secret expires.
        /// </summary>
        public DateTimeOffset? ExpiresOn { get; set; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Secret"/> is expired.
        /// </summary>
        public bool IsExpired => ExpiresOn.HasValue
                              && ExpiresOn < DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets the secret value.
        /// </summary>
        /// <param name="secret">The <see cref="Secret"/>.</param>
        public static implicit operator string(Secret secret)
        {
            return secret.Value;
        }
    }
}

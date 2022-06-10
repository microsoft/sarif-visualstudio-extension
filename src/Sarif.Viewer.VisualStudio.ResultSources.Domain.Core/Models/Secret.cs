// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Models
{
    /// <summary>
    /// Represents a secret string.
    /// </summary>
    public class Secret
    {
        /// <summary>
        /// Gets or sets the secret value.
        /// </summary>
        public string Value { get; set; }

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

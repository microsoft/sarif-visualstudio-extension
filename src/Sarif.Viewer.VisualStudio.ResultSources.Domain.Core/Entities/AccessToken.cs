// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Entities
{
    /// <summary>
    /// Represents an access token.
    /// </summary>
    public class AccessToken
    {
        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets the access token.
        /// </summary>
        /// <param name="accessToken">The <see cref="AccessToken"/>.</param>
        public static implicit operator string(AccessToken accessToken)
        {
            return accessToken.Value;
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CSharpFunctionalExtensions;

using Microsoft.Alm.Authentication;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Entities;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Errors;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Abstractions
{
    public interface ISecretStoreRepository
    {
        /// <summary>
        /// Reads the <see cref="AccessToken"/> tagged with the specified <see cref="TargetUri"/>.
        /// </summary>
        /// <param name="targetUri">The <see cref="TargetUri"/>.</param>
        /// <returns>The <see cref="AccessToken"/>, if found; otherwise, null.</returns>
        Maybe<AccessToken> ReadAccessToken(TargetUri targetUri);

        /// <summary>
        /// Writes the specified <see cref="AccessToken"/> to the store.
        /// </summary>
        /// <param name="targetUri">The <see cref="TargetUri"/> with which to tag the <see cref="AccessToken"/>.</param>
        /// <param name="accessToken">The <see cref="AccessToken"/> to write to the store.</param>
        /// <returns><see cref="Result"/>.</returns>
        Result<bool, Error> WriteAccessToken(TargetUri targetUri, AccessToken accessToken);

        /// <summary>
        /// Deletes the <see cref="AccessToken"/> tagged with the specified <see cref="TargetUri"/>.
        /// </summary>
        /// <param name="targetUri">The <see cref="TargetUri"/> with which to tag the <see cref="AccessToken"/>.</param>
        /// <returns><see cref="Result"/>.</returns>
        Result<bool, Error> DeleteAccessToken(TargetUri targetUri);
    }
}

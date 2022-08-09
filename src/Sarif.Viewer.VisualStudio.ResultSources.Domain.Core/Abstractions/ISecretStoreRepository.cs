// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CSharpFunctionalExtensions;

using Microsoft.Alm.Authentication;

using Secret = Microsoft.Sarif.Viewer.ResultSources.Domain.Entities.Secret;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Abstractions
{
    /// <summary>
    /// Provides a repository for secure data storage.
    /// </summary>
    public interface ISecretStoreRepository
    {
        /// <summary>
        /// Reads the <see cref="Secret"/> tagged with the specified <see cref="TargetUri"/>.
        /// </summary>
        /// <param name="targetUri">The <see cref="TargetUri"/>.</param>
        /// <returns>The <see cref="Secret"/>, if found; otherwise, null.</returns>
        Maybe<Secret> ReadSecret(TargetUri targetUri);

        /// <summary>
        /// Writes the specified <see cref="Secret"/> to the store.
        /// </summary>
        /// <param name="targetUri">The <see cref="TargetUri"/> with which to tag the <see cref="Secret"/>.</param>
        /// <param name="secret">The <see cref="Secret"/> to write to the store.</param>
        /// <returns><see cref="Result"/>.</returns>
        Result WriteSecret(TargetUri targetUri, Secret secret);

        /// <summary>
        /// Deletes the <see cref="Secret"/> tagged with the specified <see cref="TargetUri"/>.
        /// </summary>
        /// <param name="targetUri">The <see cref="TargetUri"/> with which to tag the <see cref="Secret"/>.</param>
        /// <returns><see cref="Result"/>.</returns>
        Result DeleteSecret(TargetUri targetUri);
    }
}

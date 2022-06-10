// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using CSharpFunctionalExtensions;

using Mapster;

using Microsoft.Alm.Authentication;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Abstractions;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Entities;

namespace Microsoft.Sarif.Viewer.ResultSources.ACL
{
    public class SecretStoreRepository : ISecretStoreRepository
    {
        private const string SecretsNamespace = "microsoft-sarif-visualstudio-extension";
        private readonly SecretStore secretStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecretStoreRepository"/> class.
        /// </summary>
        public SecretStoreRepository()
        {
            this.secretStore = new SecretStore(SecretsNamespace);
        }

        /// <inheritdoc cref="ISecretStoreRepository.ReadAccessToken(TargetUri)"/>
        public Maybe<AccessToken> ReadAccessToken(TargetUri targetUri)
        {
            Token accessToken = this.secretStore.ReadToken(targetUri);
            return accessToken?.Adapt<AccessToken>();
        }

        /// <inheritdoc cref="ISecretStoreRepository.WriteAccessToken(TargetUri, AccessToken)"/>
        public Result WriteAccessToken(TargetUri targetUri, AccessToken accessToken)
        {
            if (accessToken?.Value == null)
            {
                // Throw here because we expect our callers to have checked for this.
                throw new ArgumentException($"{nameof(accessToken.Value)} cannot be null");
            }

            bool result = this.secretStore.WriteToken(targetUri, new Token(accessToken.Value, TokenType.Personal));
            return Result.SuccessIf(result, "Failed to write access token to secret store");
        }

        /// <inheritdoc cref="ISecretStoreRepository.DeleteAccessToken(TargetUri)"/>
        public Result DeleteAccessToken(TargetUri targetUri)
        {
            bool result = this.secretStore.DeleteToken(targetUri);
            return Result.SuccessIf(result, "Failed to delete access token from secret store");
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using CSharpFunctionalExtensions;

using Mapster;

using Microsoft.Alm.Authentication;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Abstractions;

using Secret = Microsoft.Sarif.Viewer.ResultSources.Domain.Entities.Secret;

namespace Microsoft.Sarif.Viewer.ResultSources.ACL
{
    /// <inheritdoc cref="ISecretStoreRepository"/>
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

        /// <inheritdoc cref="ISecretStoreRepository.ReadSecret(TargetUri)"/>
        public Maybe<Secret> ReadSecret(TargetUri targetUri)
        {
            Token secret = this.secretStore.ReadToken(targetUri);
            return secret?.Adapt<Secret>();
        }

        /// <inheritdoc cref="ISecretStoreRepository.WriteSecret(TargetUri, Secret)"/>
        public Result WriteSecret(TargetUri targetUri, Secret secret)
        {
            if (secret?.Value == null)
            {
                // Throw here because we expect our callers to have checked for this.
                throw new ArgumentException($"{nameof(secret.Value)} cannot be null");
            }

            bool result = this.secretStore.WriteToken(targetUri, new Token(secret.Value, TokenType.Personal));
            return Result.SuccessIf(result, "Failed to write access token to secret store");
        }

        /// <inheritdoc cref="ISecretStoreRepository.DeleteSecret(TargetUri)"/>
        public Result DeleteSecret(TargetUri targetUri)
        {
            bool result = this.secretStore.DeleteToken(targetUri);
            return Result.SuccessIf(result, "Failed to delete secret from secret store");
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CSharpFunctionalExtensions;

using Mapster;

using Microsoft.Alm.Authentication;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Abstractions;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Entities;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Errors;

namespace Microsoft.Sarif.Viewer.ResultSources.ACL
{
    public class SecretStoreRepository : ISecretStoreRepository
    {
        private const string SecretsNamespace = "microsoft-sarif-visualstudio-extension";
        private readonly SecretStore secretStore;

        public SecretStoreRepository()
        {
            this.secretStore = new SecretStore(SecretsNamespace);
        }

        public Maybe<AccessToken> ReadAccessToken(TargetUri targetUri)
        {
            Token accessToken = this.secretStore.ReadToken(targetUri);
            return accessToken?.Adapt<AccessToken>();
        }

        public Result<bool, Error> WriteAccessToken(TargetUri targetUri, AccessToken accessToken)
        {
            bool result = this.secretStore.WriteToken(targetUri, new Token(accessToken.Value, TokenType.Personal));
            return result ?
                Result.Success<bool, Error>(true) :
                Result.Failure<bool, Error>(new SecretStoreError("Failed to write access token to secret store"));
        }

        public Result<bool, Error> DeleteAccessToken(TargetUri targetUri)
        {
            bool result = this.secretStore.DeleteToken(targetUri);
            return result ?
                Result.Success<bool, Error>(true) :
                Result.Failure<bool, Error>(new SecretStoreError("Failed to delete access token from secret store"));
        }
    }
}

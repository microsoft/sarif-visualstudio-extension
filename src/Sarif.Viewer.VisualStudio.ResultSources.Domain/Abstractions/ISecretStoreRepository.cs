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
        Maybe<AccessToken> ReadAccessToken(TargetUri targetUri);

        Result<bool, Error> WriteAccessToken(TargetUri targetUri, AccessToken accessToken);

        Result<bool, Error> DeleteAccessToken(TargetUri targetUri);
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sarif.Viewer.VisualStudio.ResultSources.Domain.Core
{
    public interface IHttpClientAdapter
    {
        HttpRequestMessage BuildRequest(
               HttpMethod httpMethod,
               string url,
               string accept = "application/json",
               string token = null);

        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);
    }
}

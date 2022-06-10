// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sarif.Viewer.VisualStudio.ResultSources.Domain.Core
{
    /// <summary>
    /// Represents an HTTP client adapter.
    /// </summary>
    public interface IHttpClientAdapter
    {
        /// <summary>
        /// Builds an HTTP request message object.
        /// </summary>
        /// <param name="httpMethod">The <see cref="HttpMethod"/> of the request.</param>
        /// <param name="url">The URL to be requested.</param>
        /// <param name="accept">The Accept request header value.</param>
        /// <param name="token">The Authorization request header value.</param>
        /// <returns>The <see cref="HttpRequestMessage"/>.</returns>
        HttpRequestMessage BuildRequest(
               HttpMethod httpMethod,
               string url,
               string accept = "application/json",
               string token = null);

        /// <summary>
        /// Sends the specified <see cref="HttpRequestMessage"/>.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);
    }
}

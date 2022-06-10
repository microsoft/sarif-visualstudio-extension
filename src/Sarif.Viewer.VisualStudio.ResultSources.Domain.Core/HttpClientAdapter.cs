// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Sarif.Viewer.VisualStudio.ResultSources.Domain.Core
{
    /// <inheritdoc cref="IHttpClientAdapter"/>
    public class HttpClientAdapter : IHttpClientAdapter
    {
        private const string UserAgent = "microsoft/sarif-visualstudio-extension";
        private readonly HttpClient httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientAdapter"/> class.
        /// </summary>
        public HttpClientAdapter()
        {
            this.httpClient = new HttpClient();
        }

        /// <inheritdoc cref="IHttpClientAdapter.BuildRequest(HttpMethod, string, string, string)"/>
        public HttpRequestMessage BuildRequest(
            HttpMethod httpMethod,
            string url,
            string accept = "application/json",
            string token = null)
        {
            var requestMessage = new HttpRequestMessage(httpMethod, url);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));
            requestMessage.Headers.Add("User-Agent", UserAgent);

            if (!string.IsNullOrWhiteSpace(token))
            {
                requestMessage.Headers.Add("Authorization", $"Bearer {token}");
            }

            return requestMessage;
        }

        /// <inheritdoc cref="IHttpClientAdapter.SendAsync(HttpRequestMessage, CancellationToken)"/>
        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default) =>
            await this.httpClient.SendAsync(request, cancellationToken);
    }
}

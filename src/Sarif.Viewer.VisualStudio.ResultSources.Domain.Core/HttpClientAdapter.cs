// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Sarif.Viewer.VisualStudio.ResultSources.Domain.Core
{
    public class HttpClientAdapter : IHttpClientAdapter
    {
        private readonly HttpClient httpClient;

        public HttpClientAdapter(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public HttpRequestMessage BuildRequest(
            HttpMethod httpMethod,
            string url,
            string accept = "application/json",
            string token = null)
        {
            var requestMessage = new HttpRequestMessage(httpMethod, url);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));
            requestMessage.Headers.Add("User-Agent", "microsoft/sarif-visualstudio-extension");

            if (!string.IsNullOrWhiteSpace(token))
            {
                requestMessage.Headers.Add("Authorization", $"Bearer {token}");
            }

            return requestMessage;
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default) =>
            this.httpClient.SendAsync(request, cancellationToken);
    }
}

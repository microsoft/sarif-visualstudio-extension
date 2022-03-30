// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain
{
    internal class HttpUtility
    {
        internal async Task<HttpResponseMessage> GetHttpResponseAsync(
            HttpClient httpClient,
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

            return await httpClient.SendAsync(requestMessage);
        }
    }
}

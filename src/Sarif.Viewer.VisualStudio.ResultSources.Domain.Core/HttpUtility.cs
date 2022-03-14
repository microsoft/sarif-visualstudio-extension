// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain
{
    internal static class HttpUtility
    {
        internal static async Task<HttpResponseMessage> GetHttpResponseAsync(
            HttpMethod method,
            string url,
            string accept = "application/json",
            string token = null)
        {
            var httpClient = new HttpClient();
            HttpRequestMessage requestMessage = new HttpRequestMessage(method, url);
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

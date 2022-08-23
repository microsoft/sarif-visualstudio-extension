// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain
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

        /// <summary>
        /// Downloads a file and saves it to a temporary file location.
        /// </summary>
        /// <param name="url">The file download URL.</param>
        /// <returns>The absolute path of the file if the download was successful; otherwise, null.</returns>
        Task<string> DownloadFileAsync(string url);

        /// <summary>
        /// Downloads a file and saves it to the specified location.
        /// </summary>
        /// <param name="url">The file download URL.</param>
        /// <param name="filePath">The absolute path at which to save the file.</param>
        /// <returns>The absolute path of the file if the download was successful; otherwise, null.</returns>
        Task<string> DownloadFileAsync(string url, string filePath);
    }
}

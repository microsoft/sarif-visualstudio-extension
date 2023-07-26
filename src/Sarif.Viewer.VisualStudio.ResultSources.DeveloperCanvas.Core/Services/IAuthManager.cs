// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Identity.Client;

namespace Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core.Services
{
    /// <summary>
    /// Handles the credentials retreival for the user.
    /// </summary>
    internal interface IAuthManager
    {
        /// <summary>
        /// Gets the authentication for a particular user.
        /// If not authenticated already, will prompt the user to authenticate.
        /// If it failed to authenticate, will return null;
        /// </summary>
        /// <returns>The authentication result if valid, null otherwise.</returns>
        public Task<AuthenticationResult> AuthenticateAsync();

        /// <summary>
        /// Gets an <see cref="HttpClient"/> that has the credentials to interact with the DevCanvas API.
        /// </summary>
        /// <returns>A <see cref="HttpClient"/> that can be used with DevCanvas APIs if authenticated, null otherwise.</returns>
        public Task<HttpClient> GetHttpClientAsync();
    }
}

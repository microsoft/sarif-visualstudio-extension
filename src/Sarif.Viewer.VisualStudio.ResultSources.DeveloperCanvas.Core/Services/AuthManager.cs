// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Identity.Client;

namespace Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core.Services
{
    internal class AuthManager : IAuthManager
    {
        private readonly IPublicClientApplication publicClientApplication;

        private const string existingClientIdApproved = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";
        //private readonly string[] scopes = new string[0];
        private readonly string[] scopes = new string[] { "api://7ba8d231-9a00-4118-8a4d-9423b0f0a0f5/user_impersonation" };
        private const string AadInstanceUrlFormat = "https://login.microsoftonline.com/{0}/v2.0";
        private const string msAadTenant = "72f988bf-86f1-41af-91ab-2d7cd011db47"; // GUID for the microsoft AAD tenant;

        public AuthManager()
        {
            string tenantToTry = msAadTenant;
            string authorityUrl = string.Format(CultureInfo.InvariantCulture, AadInstanceUrlFormat, tenantToTry);
            this.publicClientApplication = PublicClientApplicationBuilder
                .Create(existingClientIdApproved)
                .WithAuthority(AzureCloudInstance.AzurePublic, tenantToTry)
                .WithDefaultRedirectUri()
                //.WithAuthority(authorityUrl)
                //.WithRedirectUri("https://insightsapi.devcanvas.trafficmanager.net/.auth/login/aad/callback")
                //.WithRedirectUri("https://insightwebv2.azurewebsites.net/.auth/login/aad/callback")
                //.WithRedirectUri(@"https://insightwebv2.azurewebsites.net")
                .Build();
        }

        public async Task<AuthenticationResult> AuthenticateAsync()
        {
            try
            {
                IEnumerable<IAccount> accounts = await this.publicClientApplication.GetAccountsAsync();
                return await this.publicClientApplication
                    //.AcquireTokenSilent(new List<string>(), accounts.FirstOrDefault())
                    .AcquireTokenSilent(this.scopes, accounts.FirstOrDefault())
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                try
                {
                    // If the token has expired or the cache was empty, display a login prompt
                    return await this.publicClientApplication
                       //.AcquireTokenInteractive(new List<string>())
                       .AcquireTokenInteractive(this.scopes)
                       .WithClaims(ex.Claims)
                       .ExecuteAsync();
                }
                catch (Exception) { }
            }
            return null;
        }

        public async Task<HttpClient?> GetHttpClientAsync()
        {
            AuthenticationResult authentication = await this.AuthenticateAsync();
            if (authentication == null)
            {
                return null;
            }
            string accessToken = authentication.AccessToken;
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return client;
        }
    }
}

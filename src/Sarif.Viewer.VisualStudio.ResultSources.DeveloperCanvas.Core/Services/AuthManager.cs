// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IdentityModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using Microsoft.Identity.Client.Extensions.Msal;

namespace Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core.Services
{
    /// <summary>
    /// Handles the credentials retreival for the user.
    /// </summary>
    internal class AuthManager : IAuthManager
    {
        private readonly IPublicClientApplication publicClientApplication;

        private const string existingClientIdApproved = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";
        private readonly string[] ppeScopes = new string[] { "api://5360327a-4e80-4925-8701-51fa2000738e/user_impersonation" };
        private readonly string[] devScopes = new string[] { "api://a5880bae-b129-42c5-9d6c-d8de8f305adf/user_impersonation" };
        private readonly string[] prodScopes = new string[] { "api://7ba8d231-9a00-4118-8a4d-9423b0f0a0f5/user_impersonation" };
        private readonly string[] usedScopes;
        private const string AadInstanceUrlFormat = "https://login.microsoftonline.com/{0}/v2.0";
        private const string msAadTenant = "72f988bf-86f1-41af-91ab-2d7cd011db47"; // GUID for the microsoft AAD tenant;
        private readonly SemaphoreSlim slimSemaphore;
        private readonly MsalCacheHelper cacheHelper;

        public AuthManager()
        {
            var brokerOpt = new BrokerOptions(BrokerOptions.OperatingSystems.Windows);
            brokerOpt.Title = "DevCanvas";

            string authorityUrl = string.Format(CultureInfo.InvariantCulture, AadInstanceUrlFormat, msAadTenant);
            this.publicClientApplication = PublicClientApplicationBuilder
                .Create(existingClientIdApproved)
                .WithAuthority(AzureCloudInstance.AzurePublic, msAadTenant)
                .WithBroker(brokerOpt)
                .WithDefaultRedirectUri()
                .Build();

            StorageCreationProperties storageProperties = new StorageCreationPropertiesBuilder($"{nameof(DevCanvasResultSourceService)}_MSAL_cache_{msAadTenant}.txt", MsalCacheHelper.UserRootDirectory)
    .Build();
            cacheHelper = MsalCacheHelper.CreateAsync(storageProperties).GetAwaiter().GetResult();
            cacheHelper.RegisterCache(this.publicClientApplication.UserTokenCache);
            slimSemaphore = new SemaphoreSlim(1);

            usedScopes = ppeScopes;
        }

        public async Task<AuthenticationResult> AuthenticateAsync()
        {
            try
            {
                await slimSemaphore.WaitAsync();
                try
                {
                    IEnumerable<IAccount> accounts = await this.publicClientApplication.GetAccountsAsync();
                    AuthenticationResult result = await this.publicClientApplication
                        .AcquireTokenSilent(this.usedScopes, accounts.FirstOrDefault())
                        .ExecuteAsync();
                    Trace.WriteLine($"Using credentails of {accounts.First().Username}");
                    return result;
                }
                catch (MsalUiRequiredException ex)
                {
                    try
                    {
                        // If the token has expired or the cache was empty, display a login prompt
                        return await this.publicClientApplication
                           .AcquireTokenInteractive(this.usedScopes)
                           .WithClaims(ex.Claims)
                           .ExecuteAsync();
                    }
                    catch (Exception)
                    {
                        Trace.WriteLine("Failed to acquire token interactively");
                    }
                }
            }
            finally
            {
                slimSemaphore.Release();
            }

            Trace.WriteLine("Failed to acquire token at all. ");
            return null;
        }

        public async Task<HttpClient> GetHttpClientAsync()
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

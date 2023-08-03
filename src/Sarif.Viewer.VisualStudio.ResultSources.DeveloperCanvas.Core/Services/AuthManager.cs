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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using Microsoft.Identity.Client.Extensions.Msal;

namespace Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core.Services
{
    /// <summary>
    /// Handles the credentials retrieval for the user.
    /// </summary>
    internal class AuthManager : IAuthManager
    {
        private IPublicClientApplication publicClientApplication;

        private const string existingClientIdApproved = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";
        private readonly static string[] prodScopes = new string[] { "api://7ba8d231-9a00-4118-8a4d-9423b0f0a0f5/user_impersonation" };
        private readonly static string prodBrokerTitle = "Log into DevCanvas. https://aka.ms/devcanvas";
        private readonly static string[] ppeScopes = new string[] { "api://5360327a-4e80-4925-8701-51fa2000738e/user_impersonation" };
        private readonly static string ppeBrokerTitle = "Log into DevCanvas PPE environment. https://aka.ms/devcanvas";
        private readonly static string[] devScopes = new string[] { "api://a5880bae-b129-42c5-9d6c-d8de8f305adf/user_impersonation" };
        private readonly static string devBrokerTitle = "Log into DevCanvas Developer environment. https://aka.ms/devcanvas";
        private readonly static string[][] serverScopes = new string[][] { prodScopes, ppeScopes, devScopes };
        private readonly static string[] serverBrokerTitles = new string[] { prodBrokerTitle, ppeBrokerTitle, devBrokerTitle };
        private const string AadInstanceUrlFormat = "https://login.microsoftonline.com/{0}/v2.0";
        private const string msAadTenant = "72f988bf-86f1-41af-91ab-2d7cd011db47"; // GUID for the microsoft AAD tenant;
        private readonly SemaphoreSlim slimSemaphore;
        private MsalCacheHelper cacheHelper;

        /// <summary>
        /// Denotes the last server that was accessed. Used when decided whether we need to refresh the <see cref="publicClientApplication"/>
        /// </summary>
        private int? previousServerIndex = null;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private IntPtr GetIntPtr()
        {
            // Works, no title.
            IntPtr hWnd = GetForegroundWindow();
            return hWnd;
        }

        public AuthManager()
        {
            slimSemaphore = new SemaphoreSlim(1);
        }

        private void SetupClientApp(int serverIndex)
        {
            DevCanvasTracer.WriteLine("Setting up client app.");
            var brokerOpt = new BrokerOptions(BrokerOptions.OperatingSystems.Windows)
            {
                Title = serverBrokerTitles[serverIndex],
            };

            string authorityUrl = string.Format(CultureInfo.InvariantCulture, AadInstanceUrlFormat, msAadTenant);
            this.publicClientApplication = PublicClientApplicationBuilder
                .Create(existingClientIdApproved)
                .WithParentActivityOrWindow(GetIntPtr)
                .WithBroker(brokerOpt)
                .Build();

            StorageCreationProperties storageProperties = new StorageCreationPropertiesBuilder($"{nameof(DevCanvasResultSourceService)}_MSAL_cache_{msAadTenant}.txt", MsalCacheHelper.UserRootDirectory)
    .Build();
            cacheHelper = MsalCacheHelper.CreateAsync(storageProperties).GetAwaiter().GetResult();
            cacheHelper.RegisterCache(this.publicClientApplication.UserTokenCache);
        }

        /// <summary>
        /// Gets the authentication for a particular user.
        /// If not authenticated already, will prompt the user to authenticate.
        /// If it failed to authenticate, will return null;
        /// </summary>
        /// <returns>The authentication result if valid, null otherwise.</returns>
        private async Task<AuthenticationResult> AuthenticateAsync(int endpointIndex)
        {
            try
            {
                await slimSemaphore.WaitAsync();
                DevCanvasTracer.WriteLine($"previous: {previousServerIndex} current: {endpointIndex}");
                if (previousServerIndex == null || endpointIndex != previousServerIndex) // if this is the first server being requested or we need to change server.
                {
                    SetupClientApp(endpointIndex);
                    previousServerIndex = endpointIndex;
                }
                string[] scopes = serverScopes[endpointIndex];

                try
                {
                    IEnumerable<IAccount> accounts = await this.publicClientApplication.GetAccountsAsync();
                    foreach (IAccount account in accounts)
                    {
                        DevCanvasTracer.WriteLine($"account.Username: {account.Username}, account.Environment: {account.Environment}");
                    }

                    AuthenticationResult result = await this.publicClientApplication
                        .AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                        .ExecuteAsync();
                    DevCanvasTracer.WriteLine($"Automatically retrieved credentails for {accounts.First().Username} to access {endpointIndex}");
                    return result;
                }
                catch (MsalUiRequiredException ex)
                {
                    try
                    {
                        // If the token has expired or the cache was empty, display a login prompt
                        return await this.publicClientApplication
                           .AcquireTokenInteractive(scopes)
                           .WithClaims(ex.Claims)
                           .WithUseEmbeddedWebView(true)
                           .ExecuteAsync();
                    }
                    catch (Exception e)
                    {
                        DevCanvasTracer.WriteLine($"Failed to acquire token interactively.\nException: {e}");
                    }
                }
            }
            finally
            {
                slimSemaphore.Release();
            }

            DevCanvasTracer.WriteLine("Failed to acquire token at all.");
            return null;
        }

        public async Task<HttpClient> GetHttpClientAsync(int endpointIndex)
        {
            AuthenticationResult authentication = await this.AuthenticateAsync(endpointIndex);
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

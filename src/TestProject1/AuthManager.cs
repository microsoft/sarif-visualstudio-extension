using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Identity.Client;

namespace TestProject1
{

    public class AuthManager
    {
        private readonly IPublicClientApplication publicClientApplication;
        /// <summary>
        /// This one is from the as-ado branch
        /// </summary>
        //private const string ClientId = "b86035bd-b0d6-48e8-aa8e-ac09b247525b";

        ///publicInsightsServerPMEProdAADApp
        private const string ClientId = "7ba8d231-9a00-4118-8a4d-9423b0f0a0f5";
        private readonly string[] scopes = new string[] { "499b84ac-1321-427f-aa17-267ca6975798/user_impersonation" }; // Constant value to target Azure DevOps. Do not change!
        private const string AadInstanceUrlFormat = "https://login.microsoftonline.com/{0}/v2.0";
        private const string msAadTenant = "72f988bf-86f1-41af-91ab-2d7cd011db47"; // GUID for the microsoft AAD tenant;
        private const string tenantId = "975f013f-7f24-47e8-a7d3-abc4752bf346"; // from the az portal page for devcanvas insight api

        public AuthManager()
        {
            string authorityUrl = string.Format(CultureInfo.InvariantCulture, AadInstanceUrlFormat, msAadTenant);
            this.publicClientApplication = PublicClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(authorityUrl)
                //.WithRedirectUri("https://insightsapi.devcanvas.trafficmanager.net/.auth/login/aad/callback")
                .WithRedirectUri("https://insightwebv2.azurewebsites.net/.auth/login/aad/callback")
                //.WithRedirectUri(@"https://insightwebv2.azurewebsites.net")
                .Build();
        }

        public async Task<AuthenticationResult> AuthenticateAsync()
        {
            AuthenticationResult result = null;

            try
            {
                IEnumerable<IAccount> accounts = await this.publicClientApplication.GetAccountsAsync();
                result = await this.publicClientApplication
                    .AcquireTokenSilent(new List<string>(), accounts.FirstOrDefault())
                    //.AcquireTokenSilent(this.scopes, accounts.FirstOrDefault())
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                try
                {
                    Dictionary<string, string> extraParams = new Dictionary<string, string>();
                    // If the token has expired or the cache was empty, display a login prompt
                    result = await this.publicClientApplication
                       .AcquireTokenInteractive(new List<string>())
                       .WithClaims(ex.Claims)
                       .ExecuteAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            return result;
        }

    }
}

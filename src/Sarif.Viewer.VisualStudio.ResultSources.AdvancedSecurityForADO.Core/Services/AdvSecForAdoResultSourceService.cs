// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using CSharpFunctionalExtensions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Identity.Client;
using Microsoft.Sarif.Viewer.ResultSources.AdvancedSecurityForAdo.Models;
using Microsoft.Sarif.Viewer.ResultSources.Domain;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Models;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Services;

using Newtonsoft.Json;

using Sarif.Viewer.VisualStudio.ResultSources.AdvancedSecurityForAdo.Models;

using Result = CSharpFunctionalExtensions.Result;

namespace Microsoft.Sarif.Viewer.ResultSources.AdvancedSecurityForAdo.Services
{
    public class AdvSecForAdoResultSourceService : IResultSourceService, IAdvSecForAdoResultSourceService
    {
        private const string ClientId = "b86035bd-b0d6-48e8-aa8e-ac09b247525b";
        private const string AadInstanceUrlFormat = "https://login.microsoftonline.com/{0}/v2.0";
        private const string SettingsFilePath = "AdvSecADO.json";
        private const string AzureDevOpsBaseUrl = "https://dev.azure.com/";
        private const string ListBuildsApiQueryString = "/_apis/build/builds?api-version=6.0&deletedFilter=excludeDeleted";
        private const string GetBuildArtifactApiQueryStringFormat = "/_apis/build/builds/{0}/artifacts?artifactName=CodeAnalysisLogs&api-version=6.0";

        private readonly string[] scopes = new string[] { "499b84ac-1321-427f-aa17-267ca6975798/user_impersonation" }; // Constant value to target Azure DevOps. Do not change!
        private readonly string solutionRootPath;
        private readonly IServiceProvider serviceProvider;
        private readonly IHttpClientAdapter httpClientAdapter;
        private readonly IFileSystem fileSystem;

        private Settings settings;
        private string authorityUrl;
        private string authHeader;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdvSecForAdoResultSourceService"/> class.
        /// </summary>
        /// <param name="solutionRootPath">The full path of the solution directory.</param>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="httpClientAdapter">The <see cref="IHttpClientAdapter"/>.</param>
        /// <param name="fileSystem">The file system.</param>
        public AdvSecForAdoResultSourceService(
            string solutionRootPath,
            IServiceProvider serviceProvider,
            IHttpClientAdapter httpClientAdapter,
            IFileSystem fileSystem)
        {
            this.solutionRootPath = solutionRootPath;
            this.serviceProvider = serviceProvider;
            this.httpClientAdapter = httpClientAdapter;
            this.fileSystem = fileSystem;
        }

        /// <inheritdoc cref="IResultSourceService.ResultsUpdated"/>
        public event EventHandler<ResultsUpdatedEventArgs> ResultsUpdated;

        /// <inheritdoc cref="IResultSourceService.InitializeAsync()"/>
        public async Task InitializeAsync()
        {
            if (!string.IsNullOrWhiteSpace(this.solutionRootPath))
            {
                string path = Path.Combine(this.solutionRootPath, SettingsFilePath);
                if (fileSystem.FileExists(path))
                {
                    string settingsText = File.ReadAllText(path);

                    try
                    {
                        this.settings = JsonConvert.DeserializeObject<Settings>(settingsText);
                        this.authorityUrl = string.Format(CultureInfo.InvariantCulture, AadInstanceUrlFormat, this.settings.Tenant);

                        AuthenticationResult authResult = await AuthenticateAsync();
                        this.authHeader = authResult.CreateAuthorizationHeader();
                    }
                    catch (JsonSerializationException) { }
                }
            }
        }

        /// <inheritdoc cref="IResultSourceService.IsActiveAsync()"/>
        public Task<Result> IsActiveAsync()
        {
            Result result = !string.IsNullOrWhiteSpace(this.solutionRootPath) && fileSystem.FileExists(Path.Combine(this.solutionRootPath, SettingsFilePath)) ?
                Result.Success() :
                Result.Failure(nameof(AdvSecForAdoResultSourceService));
            return Task.FromResult(result);
        }

        /// <inheritdoc cref="IResultSourceService.RequestAnalysisResultsAsync(object)"/>
        public Task<Result<bool, ErrorType>> RequestAnalysisResultsAsync(object data = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IAdvSecForAdoResultSourceService.GetLatestBuildIdAsync()"/>
        public async Task<Result<int, ErrorType>> GetLatestBuildIdAsync()
        {
            // TODO: what filters are needed?
            HttpRequestMessage requestMessage = httpClientAdapter.BuildRequest(
                HttpMethod.Get,
                AzureDevOpsBaseUrl + ListBuildsApiQueryString,
                token: "token-here");
            HttpResponseMessage responseMessage = await httpClientAdapter.SendAsync(requestMessage);

            if (responseMessage.IsSuccessStatusCode)
            {
                List<Build> builds = JsonConvert.DeserializeObject<List<Build>>(await responseMessage.Content.ReadAsStringAsync());

                if (builds.Count > 0)
                {
                    return Result.Success<int, ErrorType>(builds.First().Id);
                }
            }

            return Result.Failure<int, ErrorType>(ErrorType.DataUnavailable);
        }

        /// <inheritdoc cref="IAdvSecForAdoResultSourceService.GetArtifactDownloadUrlAsync(string)"/>
        public async Task<Result<string, ErrorType>> GetArtifactDownloadUrlAsync(int buildId)
        {
            HttpRequestMessage requestMessage = httpClientAdapter.BuildRequest(
                HttpMethod.Get,
                AzureDevOpsBaseUrl + string.Format(GetBuildArtifactApiQueryStringFormat, buildId),
                token: "token-here");

            return await Task.FromResult(string.Empty);
        }

        private async Task<AuthenticationResult> AuthenticateAsync()
        {
            IPublicClientApplication application = PublicClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(this.authorityUrl)
                .WithDefaultRedirectUri()
                .Build();

            AuthenticationResult result;

            try
            {
                IEnumerable<IAccount> accounts = await application.GetAccountsAsync();
                result = await application
                    .AcquireTokenSilent(this.scopes, accounts.FirstOrDefault())
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                // If the token has expired or the cache was empty, display a login prompt
                result = await application
                    .AcquireTokenInteractive(scopes)
                    .WithClaims(ex.Claims)
                    .ExecuteAsync();
            }

            return result;
        }

        private void RaiseResultsUpdatedEvent(ResultsUpdatedEventArgs eventArgs = null)
        {
            ResultsUpdated?.Invoke(this, eventArgs);
        }
    }
}

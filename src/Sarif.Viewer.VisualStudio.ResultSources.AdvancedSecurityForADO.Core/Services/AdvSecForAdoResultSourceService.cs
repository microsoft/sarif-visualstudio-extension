// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;

using CSharpFunctionalExtensions;

using Microsoft.Alm.Authentication;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Sarif.Viewer.ResultSources.AdvancedSecurityForAdo.Models;
using Microsoft.Sarif.Viewer.ResultSources.Domain;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Abstractions;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Models;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Services;
using Microsoft.Sarif.Viewer.Shell;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using AdoBuild = Microsoft.Sarif.Viewer.ResultSources.AdvancedSecurityForAdo.Models.Build;
using File = System.IO.File;
using Result = CSharpFunctionalExtensions.Result;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Sarif.Viewer.ResultSources.AdvancedSecurityForAdo.Services
{
    public class AdvSecForAdoResultSourceService : IResultSourceService, IAdvSecForAdoResultSourceService
    {
        private const string SettingsFilePath = "AdvSecADO.json";
        private const string ScanResultsFileName = "advsec-ado-results.sarif";
        private const string ClientId = "b86035bd-b0d6-48e8-aa8e-ac09b247525b";
        private const string AadInstanceUrlFormat = "https://login.microsoftonline.com/{0}/v2.0";
        private const string AzureDevOpsBaseUrl = "https://dev.azure.com/";
        private const string ListBuildsApiQueryString = "/_apis/build/builds?deletedFilter=excludeDeleted&statusFilter=completed"; // api-version=7.0&
        private const string GetBuildArtifactApiQueryStringFormat = "/_apis/build/builds/{0}/artifacts?artifactName=CodeAnalysisLogs&api-version=7.0&%24format=zip";

        private static readonly TargetUri s_baseTargetUri = new TargetUri(AzureDevOpsBaseUrl);

        private readonly string[] scopes = new string[] { "499b84ac-1321-427f-aa17-267ca6975798/user_impersonation" }; // Constant value to target Azure DevOps. Do not change!
        private readonly string solutionRootPath;
        private readonly IServiceProvider serviceProvider;
        private readonly ISecretStoreRepository secretStoreRepository;
        private readonly IHttpClientAdapter httpClientAdapter;
        private readonly IFileSystem fileSystem;

        private Settings settings;
        private IPublicClientApplication publicClientApplication;
        private string orgAndProject;
        private string authorityUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdvSecForAdoResultSourceService"/> class.
        /// </summary>
        /// <param name="solutionRootPath">The full path of the solution directory.</param>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="secretStoreRepository">The <see cref="ISecretStoreRepository"/>.</param>
        /// <param name="httpClientAdapter">The <see cref="IHttpClientAdapter"/>.</param>
        /// <param name="fileSystem">The file system.</param>
        public AdvSecForAdoResultSourceService(
            string solutionRootPath,
            IServiceProvider serviceProvider,
            ISecretStoreRepository secretStoreRepository,
            IHttpClientAdapter httpClientAdapter,
            IFileSystem fileSystem)
        {
            this.solutionRootPath = solutionRootPath;
            this.serviceProvider = serviceProvider;
            this.secretStoreRepository = secretStoreRepository;
            this.httpClientAdapter = httpClientAdapter;
            this.fileSystem = fileSystem;
        }

        /// <inheritdoc cref="IResultSourceService.ServiceEvent"/>
        public event EventHandler<ServiceEventArgs> ServiceEvent;

        /// <inheritdoc cref="IResultSourceService.FirstMenuId"/>
        public int FirstMenuId { get; set; }

        /// <inheritdoc cref="IResultSourceService.FirstCommandId"/>
        public int FirstCommandId { get; set; }

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
                        this.orgAndProject = $"{settings.OrganizationName}/{settings.ProjectName}";
                        this.authorityUrl = string.Format(CultureInfo.InvariantCulture, AadInstanceUrlFormat, this.settings.Tenant);

                        StorageCreationProperties storageProperties =
                            new StorageCreationPropertiesBuilder("AdvSecADO_MSAL_cache.txt", MsalCacheHelper.UserRootDirectory)
                            .Build();

                        this.publicClientApplication = PublicClientApplicationBuilder
                            .Create(ClientId)
                            .WithAuthority(this.authorityUrl)
                            .WithDefaultRedirectUri()
                            .Build();

                        MsalCacheHelper cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
                        cacheHelper.RegisterCache(this.publicClientApplication.UserTokenCache);
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
        public async Task<Result<bool, ErrorType>> RequestAnalysisResultsAsync(object data = null)
        {
            Result<int, ErrorType> buildResult = await this.GetLatestBuildIdAsync();

            if (buildResult.IsSuccess)
            {
                Maybe<SarifLog> artifactResult = await DownloadAndExtractArtifactAsync(buildResult.Value);

                if (artifactResult.HasValue)
                {
                    var eventArgs = new ResultsUpdatedEventArgs
                    {
                        SarifLog = artifactResult.Value,
                        LogFileName = ScanResultsFileName,
                        UseDotSarifDirectory = false,
                    };
                    RaiseServiceEvent(eventArgs);
                    return Result.Success<bool, ErrorType>(true);
                }
            }

            return Result.Failure<bool, ErrorType>(ErrorType.AnalysesUnavailable);
        }

        /// <inheritdoc cref="IAdvSecForAdoResultSourceService.GetLatestBuildIdAsync()"/>
        public async Task<Result<int, ErrorType>> GetLatestBuildIdAsync()
        {
            AuthenticationResult authResult = await AuthenticateAsync();

            HttpRequestMessage requestMessage = httpClientAdapter.BuildRequest(
                HttpMethod.Get,
                AzureDevOpsBaseUrl + this.orgAndProject + ListBuildsApiQueryString + $"repositoryType={settings.RepositoryType}",
                token: authResult?.AccessToken);

            HttpResponseMessage responseMessage = await httpClientAdapter.SendAsync(requestMessage);

            if (responseMessage.IsSuccessStatusCode)
            {
                try
                {
                    string content = await responseMessage.Content.ReadAsStringAsync();
                    var jObject = JObject.Parse(content);

                    if (jObject.TryGetValue("value", out JToken jToken))
                    {
                        List<AdoBuild> builds = JsonConvert.DeserializeObject<List<AdoBuild>>(jToken.ToString());

                        if (builds.Count > 0)
                        {
                            var filteredBuilds = builds
                                .Where(b => b.Definition?.Name == settings.PipelineName)
                                .Where(b => b.Definition.Type == "build")
                                .Where(b => b.Result == "succeeded" || b.Result == "partiallySucceeded")
                                .ToList();
                            return Result.Success<int, ErrorType>(filteredBuilds.First().Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            return Result.Failure<int, ErrorType>(ErrorType.DataUnavailable);
        }

        /// <inheritdoc cref="IAdvSecForAdoResultSourceService.DownloadAndExtractArtifactAsync(int)"/>
        public async Task<Maybe<SarifLog>> DownloadAndExtractArtifactAsync(int buildId)
        {
            SarifLog sarifLog = null;

            try
            {
                AuthenticationResult authResult = await AuthenticateAsync();

                HttpRequestMessage requestMessage = httpClientAdapter.BuildRequest(
                    HttpMethod.Get,
                    AzureDevOpsBaseUrl + this.orgAndProject + string.Format(GetBuildArtifactApiQueryStringFormat, buildId),
                    token: authResult?.AccessToken);

                // string downloadedFilePath = await this.httpClientAdapter.DownloadFileAsync(url);
                HttpResponseMessage responseMessage = await httpClientAdapter.SendAsync(requestMessage);

                if (responseMessage.IsSuccessStatusCode)
                {
                    string tempFilePath = Path.GetTempFileName();

                    using (Stream stream = await responseMessage.Content.ReadAsStreamAsync())
                    {
                        using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
                        {
                            await stream.CopyToAsync(fileStream);
                        }
                    }

                    string extractFolder = Path.GetTempPath();

                    try
                    {
                        using (ZipArchive zipArchive = ZipFile.OpenRead(tempFilePath))
                        {
                            foreach (ZipArchiveEntry entry in zipArchive.Entries)
                            {
                                if (entry.FullName.EndsWith(".sarif", StringComparison.OrdinalIgnoreCase))
                                {
                                    string fileName = ScanResultsFileName;

                                    // Gets the full path to ensure that relative segments are removed.
                                    string destinationPath = Path.GetFullPath(Path.Combine(extractFolder, fileName));

                                    // Ordinal match is safest because case-sensitive volumes can be mounted
                                    // within volumes that are case-insensitive.
                                    if (destinationPath.StartsWith(extractFolder, StringComparison.Ordinal))
                                    {
                                        string sarifFolder = ShellUtilities.GetDotSarifDirectoryPath();
                                        string outputPath = Path.Combine(sarifFolder, fileName);
                                        entry.ExtractToFile(outputPath);
                                        sarifLog = SarifLog.Load(outputPath);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch (SecurityException) { }
            catch (IOException) { }

            return sarifLog;
        }

        private async Task<AuthenticationResult> AuthenticateAsync()
        {
            AuthenticationResult result = null;

            try
            {
                IEnumerable<IAccount> accounts = await this.publicClientApplication.GetAccountsAsync();
                result = await this.publicClientApplication
                    .AcquireTokenSilent(this.scopes, accounts.FirstOrDefault())
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                try
                {
                    // If the token has expired or the cache was empty, display a login prompt
                    result = await this.publicClientApplication
                       .AcquireTokenInteractive(scopes)
                       .WithClaims(ex.Claims)
                       .ExecuteAsync();
                }
                catch
                {
                }
            }

            return result;
        }

        private void RaiseServiceEvent(ServiceEventArgs eventArgs = null)
        {
            ServiceEvent?.Invoke(this, eventArgs);
        }
    }
}

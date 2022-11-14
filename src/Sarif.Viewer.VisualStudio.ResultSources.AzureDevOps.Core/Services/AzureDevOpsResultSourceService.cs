// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;

using CSharpFunctionalExtensions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Sarif.Viewer.ResultSources.AzureDevOps.Entities;
using Microsoft.Sarif.Viewer.ResultSources.AzureDevOps.Models;
using Microsoft.Sarif.Viewer.ResultSources.Domain;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Models;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Services;
using Microsoft.Sarif.Viewer.Shell;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using AdoBuild = Microsoft.Sarif.Viewer.ResultSources.AzureDevOps.Entities.Build;
using File = System.IO.File;
using Result = CSharpFunctionalExtensions.Result;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Sarif.Viewer.ResultSources.AzureDevOps.Services
{
    public class AzureDevOpsResultSourceService : IResultSourceService, IAzureDevOpsResultSourceService
    {
        private const string ScanResultsFileName = "advsec-ado-results.sarif";
        private const string ClientId = "b86035bd-b0d6-48e8-aa8e-ac09b247525b";
        private const string AadInstanceUrlFormat = "https://login.microsoftonline.com/{0}/v2.0";
        private const string AzureDevOpsBaseUrl = "https://dev.azure.com/";
        private const string ListRepositoriesApiQueryString = "/_apis/git/repositories?api-version=6.0";
        private const string ListBuildsApiQueryStringFormat = "/_apis/build/builds?repositoryId={0}&branchName={1}deletedFilter=excludeDeleted&statusFilter=completed"; // api-version=7.0&
        private const string GetBuildArtifactApiQueryStringFormat = "/_apis/build/builds/{0}/artifacts?artifactName=CodeAnalysisLogs&api-version=7.0&%24format=zip";
        private const string GitLocalRefFileBaseRelativePath = @".git\refs\remotes\origin";

        private readonly string[] scopes = new string[] { "499b84ac-1321-427f-aa17-267ca6975798/user_impersonation" }; // Constant value to target Azure DevOps. Do not change!
        private readonly string solutionRootPath;
        private readonly IHttpClientAdapter httpClientAdapter;
        private readonly IFileWatcher fileWatcherBranchChange;
        private readonly IFileWatcher fileWatcherGitPush;
        private readonly IFileSystem fileSystem;
        private readonly IGitExe gitExe;

        private readonly Dictionary<string, (AzureDevOpsServiceType ServiceType, Type SettingsType)> serviceDictionary = new Dictionary<string, (AzureDevOpsServiceType ServiceType, Type SettingsType)>
        {
            { "AzureDevOps.json", (AzureDevOpsServiceType.AzureDevOps, typeof(AzureDevOpsSettings)) },
            { "AdvSecForADO.json", (AzureDevOpsServiceType.AdvancedSecurity, typeof(AdvancedSecuritySettings)) },
        };

        private string repositoryId;
        private Settings settings;
        private IPublicClientApplication publicClientApplication;
        private string orgAndProject;
        private string authorityUrl;

        private string repoPath;
        private (string BranchName, string CommitHash) scanDataRequestParameters;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureDevOpsResultSourceService"/> class.
        /// </summary>
        /// <param name="solutionRootPath">The full path of the solution directory.</param>
        /// <param name="httpClientAdapter">The <see cref="IHttpClientAdapter"/>.</param>
        /// <param name="fileWatcherBranchChange">The file watcher for Git branch changes.</param>
        /// <param name="fileWatcherGitPush">The file watcher for Git pushes.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="gitExe">The <see cref="IGitExe"/>.</param>
        public AzureDevOpsResultSourceService(
            string solutionRootPath,
            IHttpClientAdapter httpClientAdapter,
            IFileWatcher fileWatcherBranchChange,
            IFileWatcher fileWatcherGitPush,
            IFileSystem fileSystem,
            IGitExe gitExe)
        {
            this.solutionRootPath = solutionRootPath;
            this.httpClientAdapter = httpClientAdapter;
            this.fileWatcherBranchChange = fileWatcherBranchChange;
            this.fileWatcherGitPush = fileWatcherGitPush;
            this.fileSystem = fileSystem;
            this.gitExe = gitExe;
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
            this.repoPath ??= await gitExe.GetRepoRootAsync();

            if (!string.IsNullOrWhiteSpace(this.solutionRootPath))
            {
                Result<Settings, string> populateSettingsResult = PopulateSettings();

                if (populateSettingsResult.IsSuccess)
                {
                    this.orgAndProject = $"{settings.OrganizationName}/{settings.ProjectName}";
                    this.authorityUrl = string.Format(CultureInfo.InvariantCulture, AadInstanceUrlFormat, this.settings.Tenant);

                    StorageCreationProperties storageProperties =
                        new StorageCreationPropertiesBuilder($"{Path.GetFileNameWithoutExtension(this.settings.SettingsFileName)}_MSAL_cache_{settings.Tenant}.txt", MsalCacheHelper.UserRootDirectory)
                        .Build();

                    this.publicClientApplication = PublicClientApplicationBuilder
                        .Create(ClientId)
                        .WithAuthority(this.authorityUrl)
                        .WithDefaultRedirectUri()
                        .Build();

                    MsalCacheHelper cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
                    cacheHelper.RegisterCache(this.publicClientApplication.UserTokenCache);

                    Maybe<GitRepository> result = await GetRepositoryAsync();

                    if (result != null)
                    {
                        this.repositoryId = result.Value.Id;
                    }
                }
            }
        }

        /// <inheritdoc cref="IResultSourceService.IsActiveAsync()"/>
        public Task<Result> IsActiveAsync()
        {
            Result<Settings, string> populateSettingsResult = PopulateSettings();

            Result result = populateSettingsResult.IsSuccess
                ? Result.Success()
                : Result.Failure(populateSettingsResult.Error);
            return Task.FromResult(result);
        }

        /// <inheritdoc cref="IResultSourceService.RequestAnalysisResultsAsync(object)"/>
        public async Task<Result<bool, ErrorType>> RequestAnalysisResultsAsync(object data = null)
        {
            Result<int, ErrorType> buildResult = await this.GetBuildIdAsync();

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

        /// <inheritdoc cref="IAzureDevOpsResultSourceService.GetRepositoryAsync()"/>
        public async Task<Maybe<GitRepository>> GetRepositoryAsync()
        {
            GitRepository gitRepository = null;
            string url = AzureDevOpsBaseUrl + this.orgAndProject + ListRepositoriesApiQueryString;
            Result<List<GitRepository>, (HttpStatusCode statusCode, string reason)> result = await GetDeserializedHttpResponseObjectAsync<List<GitRepository>>(url);

            if (result.IsSuccess)
            {
                string remoteUrl = await this.gitExe.GetRepoUriAsync();
                gitRepository = result.Value
                    .Where(r => r.RemoteUrl.Equals(remoteUrl, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();
            }

            return gitRepository;
        }

        /// <inheritdoc cref="IAzureDevOpsResultSourceService.GetBuildIdAsync()"/>
        public async Task<Result<int, ErrorType>> GetBuildIdAsync()
        {
            string url = AzureDevOpsBaseUrl + this.orgAndProject + string.Format(ListBuildsApiQueryStringFormat, this.repositoryId, this.scanDataRequestParameters.BranchName); // + $"repositoryType={settings.RepositoryType}"
            Result<List<AdoBuild>, (HttpStatusCode statusCode, string reason)> result = await GetDeserializedHttpResponseObjectAsync<List<AdoBuild>>(url);

            if (result.IsSuccess && result.Value.Any())
            {
                AdoBuild build = result.Value
                    .Where(b => b.Definition?.Name == settings.PipelineName)
                    .Where(b => b.Definition?.Type == "build")
                    .Where(b => b.SourceVersion.Equals(this.scanDataRequestParameters.CommitHash, StringComparison.OrdinalIgnoreCase))
                    .Where(b => b.Result == "succeeded" || b.Result == "partiallySucceeded")
                    .FirstOrDefault();

                if (build != null)
                {
                    return Result.Success<int, ErrorType>(build.Id);
                }
            }

            return Result.Failure<int, ErrorType>(ErrorType.DataUnavailable);
        }

        /// <inheritdoc cref="IAzureDevOpsResultSourceService.DownloadAndExtractArtifactAsync(int)"/>
        public async Task<Maybe<SarifLog>> DownloadAndExtractArtifactAsync(int buildId)
        {
            SarifLog sarifLog = null;
            string url = AzureDevOpsBaseUrl + this.orgAndProject + string.Format(GetBuildArtifactApiQueryStringFormat, buildId);
            Result<HttpContent, (HttpStatusCode statusCode, string reason)> result = await GetHttpResponseContentAsync(url);

            if (result.IsSuccess)
            {
                try
                {
                    string tempFilePath = Path.GetTempFileName();

                    using (Stream stream = await result.Value.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
                    {
                        await stream.CopyToAsync(fileStream);
                    }

                    string extractFolder = Path.GetTempPath();

                    using (ZipArchive zipArchive = ZipFile.OpenRead(tempFilePath))
                    {
                        foreach (ZipArchiveEntry entry in zipArchive.Entries)
                        {
                            if (entry.FullName.EndsWith(".sarif", StringComparison.OrdinalIgnoreCase))
                            {
                                string fileName = ScanResultsFileName;

                                // Gets the full path to ensure that relative segments are removed.
                                string outputPath = Path.GetFullPath(Path.Combine(extractFolder, fileName));

                                // Ordinal match is safest because case-sensitive volumes can be mounted
                                // within volumes that are case-insensitive.
                                if (outputPath.StartsWith(extractFolder, StringComparison.Ordinal))
                                {
                                    entry.ExtractToFile(outputPath, true);
                                    sarifLog = SarifLog.Load(outputPath);
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (SecurityException) { }
                catch (IOException) { }
            }

            return sarifLog;
        }

        internal (string Path, string Name) ParseBranchString(string branch)
        {
            // This needs to handle goofy branch names like "//asdf///-".
            int index = branch.LastIndexOf('/');
            if (index == -1)
            {
                return (string.Empty, branch);
            }
            else
            {
                string path = branch.Substring(0, index).Replace('/', '\\');
                string name = branch.Substring(index + 1);
                return (path, name);
            }
        }

        private async Task<Result<HttpContent, (HttpStatusCode statusCode, string reason)>> GetHttpResponseContentAsync(string url)
        {
            AuthenticationResult authResult = await AuthenticateAsync();

            HttpRequestMessage requestMessage = httpClientAdapter.BuildRequest(
                HttpMethod.Get,
                url,
                token: authResult?.AccessToken);

            HttpResponseMessage responseMessage = await httpClientAdapter.SendAsync(requestMessage);

            if (responseMessage.IsSuccessStatusCode)
            {
                return Result.Success<HttpContent, (HttpStatusCode, string)>(responseMessage.Content);
            }
            else
            {
                return Result.Failure<HttpContent, (HttpStatusCode, string)>((responseMessage.StatusCode, responseMessage.ReasonPhrase));
            }
        }

        private async Task<Result<T, (HttpStatusCode statusCode, string reason)>> GetDeserializedHttpResponseObjectAsync<T>(string url)
        {
            Result<HttpContent, (HttpStatusCode statusCode, string reason)> result = await GetHttpResponseContentAsync(url);

            if (result.IsSuccess)
            {
                var jObject = JObject.Parse(await result.Value.ReadAsStringAsync());
                if (jObject.TryGetValue("value", out JToken jToken))
                {
                    T responseObject = JsonConvert.DeserializeObject<T>(jToken.ToString());
                    return Result.Success<T, (HttpStatusCode statusCode, string reason)>(responseObject);
                }

                return Result.Failure<T, (HttpStatusCode, string)>((HttpStatusCode.NotFound, "Request failed"));
            }
            else
            {
                return Result.Failure<T, (HttpStatusCode, string)>(result.Error);
            }
        }

        private void InitializeFileWatchers()
        {
            this.fileWatcherBranchChange.FilePath = Path.Combine(repoPath, ".git");
            this.fileWatcherBranchChange.Filter = "HEAD";
            this.fileWatcherBranchChange.FileRenamed += this.FileWatcherBranchChange_FileRenamed;
            this.fileWatcherBranchChange.Start();

            this.fileWatcherGitPush.FileRenamed += this.FileWatcherGitPush_Renamed;
            this.fileWatcherGitPush.Start();
        }

        private async Task UpdateBranchAndCommitHashAsync()
        {
            string branchName = await gitExe.GetCurrentBranchAsync();
            (string Path, string Name) parsedBranch = ParseBranchString(branchName);
            string commitHash = await gitExe.GetCurrentCommitHashAsync();

            this.scanDataRequestParameters.BranchName = branchName;
            this.scanDataRequestParameters.CommitHash = commitHash;

            this.fileWatcherGitPush.DisableRaisingEvents();
            this.fileWatcherGitPush.FilePath = Path.Combine(repoPath, GitLocalRefFileBaseRelativePath, parsedBranch.Path);
            this.fileWatcherGitPush.Filter = parsedBranch.Name;
            this.fileWatcherGitPush.EnableRaisingEvents();
        }

        private void FileWatcherBranchChange_FileRenamed(object sender, FileSystemEventArgs e)
        {
            _ = RequestAnalysisResultsAsync();
        }

        private void FileWatcherGitPush_Renamed(object sender, FileSystemEventArgs e)
        {
            _ = RequestAnalysisResultsAsync(data: true);
        }

        private Result<Settings, string> PopulateSettings()
        {
            if (this.settings != null)
            {
                return Result.Success<Settings, string>(this.settings);
            }

            if (!string.IsNullOrWhiteSpace(this.solutionRootPath))
            {
                foreach (string settingsFileName in serviceDictionary.Keys)
                {
                    string path = Path.Combine(this.solutionRootPath, settingsFileName);
                    if (fileSystem.FileExists(path))
                    {
                        string settingsText = File.ReadAllText(path);

                        try
                        {
                            this.settings = (Settings)JsonConvert.DeserializeObject(settingsText, serviceDictionary[settingsFileName].SettingsType);
                            return Result.Success<Settings, string>(this.settings);
                        }
                        catch (JsonSerializationException ex)
                        {
                            return Result.Failure<Settings, string>(ex.ToString());
                        }
                    }
                }

                return Result.Failure<Settings, string>($"No settings file found in {this.solutionRootPath}");
            }

            return Result.Failure<Settings, string>($"{nameof(this.solutionRootPath)} not provided");
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

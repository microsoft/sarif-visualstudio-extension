// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using CSharpFunctionalExtensions;

using Microsoft.Alm.Authentication;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ResultSources.Domain;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Abstractions;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Errors;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Models;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Services;
using Microsoft.Sarif.Viewer.ResultSources.GitHubAdvancedSecurity.Errors;
using Microsoft.Sarif.Viewer.ResultSources.GitHubAdvancedSecurity.Models;
using Microsoft.Sarif.Viewer.Shell;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Octokit;

using ResourceStrings = Microsoft.Sarif.Viewer.ResultSources.GitHubAdvancedSecurity.Resources.Resources;
using Result = CSharpFunctionalExtensions.Result;
using SarifResult = Microsoft.CodeAnalysis.Sarif.Result;
using Secret = Microsoft.Sarif.Viewer.ResultSources.Domain.Models.Secret;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Sarif.Viewer.ResultSources.GitHubAdvancedSecurity.Services
{
    /// <summary>
    /// GitHubSourceService class.
    /// </summary>
    public partial class GitHubSourceService : IResultSourceService, IGitHubSourceService
    {
        private const string SecretsNamespace = "microsoft-sarif-visualstudio-extension";
        private const string ClientId = "23c8243801d898f93624";
        private const string Scope = "security_events";
        private const string GitHubRepoUriPattern = @"^https://(www.)?github.com/(?<user>[a-z\d](?:[a-z\d]|-(?=[a-z\d])){0,38})/(?<repo>[a-z\d](?:[a-z\d]|-(?=[a-z\d])){0,38}).git$";
        private const string BaseUrl = "https://github.com";
        private const string DeviceCodeUrlFormat = "https://github.com/login/device/code?client_id={0}&scope={1}";
        private const string AccessTokenUrlFormat = "https://github.com/login/oauth/access_token?client_id={0}&device_code={1}&grant_type=urn:ietf:params:oauth:grant-type:device_code";
        private const string CodeScanningBaseApiUrlFormat = "https://api.github.com/repos/{0}/{1}/code-scanning/";
        private const string GetAnalysesEndpoint = "analyses";
        private const string DismissAlertEndpointFormat = "alerts/{0}";
        private const string GitLocalRefFileBaseRelativePath = @".git\refs\remotes\origin";
        private const int ScanResultsPollIntervalSeconds = 10;
        private const int ScanResultsPollTimeoutSeconds = 1200;

        private static readonly TargetUri s_baseTargetUri = new TargetUri(BaseUrl);
        private static readonly Dictionary<string, string> s_DismissAlertReasons = new Dictionary<string, string>
        {
            // { "UI string", "GHAS API value" } -- https://docs.github.com/en/rest/code-scanning#update-a-code-scanning-alert
            { ResourceStrings.DismissAlertReason_FalsePositive, "false positive" },
            { ResourceStrings.DismissAlertReason_WontFix, "won't fix" },
            { ResourceStrings.DismissAlertReason_UsedInTests, "used in tests" },
        };

        private readonly IServiceProvider serviceProvider;
        private readonly IHttpClientAdapter httpClientAdapter;
        private readonly ISecretStoreRepository secretStoreRepository;
        private readonly IFileWatcher fileWatcherBranchChange;
        private readonly IFileWatcher fileWatcherGitPush;
        private readonly IFileSystem fileSystem;
        private readonly IGitExe gitExe;
        private readonly BrowserService browserService;
        private readonly IInfoBarService infoBarService;
        private readonly IStatusBarService statusBarService;

        private string repoPath;
        private string codeScanningBaseApiUrl;
        private IVsInfoBarUIElement infoBar;
        private CancellationTokenSource pollingCancellationTokenSource;
        private Task pollingTask = Task.CompletedTask;

        private (string BranchName, string CommitHash) scanDataRequestParameters;

        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubSourceService"/> class.
        /// </summary>
        /// <param name="solutionRootPath">The full path of the solution directory.</param>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="httpClientAdapter">The <see cref="IHttpClientAdapter"/>.</param>
        /// <param name="secretStoreRepository">The <see cref="ISecretStoreRepository"/>.</param>
        /// <param name="fileWatcherBranchChange">The file watcher for Git branch changes.</param>
        /// <param name="fileWatcherGitPush">The file watcher for Git pushes.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="gitExe">The git.exe helper.</param>
        /// <param name="infoBarService">The <see cref="IInfoBarService"/>.</param>
        /// <param name="statusBarService">The <see cref="IStatusBarService"/>.</param>
        public GitHubSourceService(
            string solutionRootPath,
            IServiceProvider serviceProvider,
            IHttpClientAdapter httpClientAdapter,
            ISecretStoreRepository secretStoreRepository,
            IFileWatcher fileWatcherBranchChange,
            IFileWatcher fileWatcherGitPush,
            IFileSystem fileSystem,
            IGitExe gitExe,
            IInfoBarService infoBarService,
            IStatusBarService statusBarService)
        {
            this.serviceProvider = serviceProvider;
            this.httpClientAdapter = httpClientAdapter;
            this.secretStoreRepository = secretStoreRepository;
            this.fileWatcherBranchChange = fileWatcherBranchChange;
            this.fileWatcherGitPush = fileWatcherGitPush;
            this.fileSystem = fileSystem;
            this.gitExe = gitExe;
            this.gitExe.RepoPath = solutionRootPath;
            this.infoBarService = infoBarService;
            this.statusBarService = statusBarService;

            this.pollingCancellationTokenSource = new CancellationTokenSource();

            this.browserService = new BrowserService();
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
            string repoUrl = await gitExe.GetRepoUriAsync();

            Match match = Regex.Match(repoUrl, GitHubRepoUriPattern);
            if (match.Success)
            {
                this.codeScanningBaseApiUrl = string.Format(CodeScanningBaseApiUrlFormat, match.Groups["user"], match.Groups["repo"]);
            }

            // Add the "Dismiss alert" command to the Error List context menu.
            var flyout = new ErrorListMenuFlyout(ResourceStrings.DismissAlert_FlyoutMenuText)
            {
                BeforeQueryStatusMenuCommand = this.DismissAlerts_BeforeQueryStatusAsync,
            };

            foreach (string key in s_DismissAlertReasons.Keys)
            {
                var command = new ErrorListMenuCommand(key)
                {
                    InvokeMenuCommand = this.DismissAlert_ExecuteAsync,
                    BeforeQueryStatusMenuCommand = this.DismissAlerts_BeforeQueryStatusAsync,
                };

                flyout.Commands.Add(command);
            }

            var eventArgs = new RequestAddMenuItemsEventArgs()
            {
                FirstMenuId = this.FirstMenuId,
                FirstCommandId = this.FirstCommandId,
            };
            eventArgs.MenuItems.Flyouts.Add(flyout);

            RaiseServiceEvent(eventArgs);
        }

        /// <inheritdoc cref="IResultSourceService.IsActiveAsync()"/>
        public async Task<Result> IsActiveAsync()
        {
            if (string.IsNullOrWhiteSpace(this.repoPath))
            {
                this.repoPath = await gitExe.GetRepoRootAsync();
            }

            return !string.IsNullOrWhiteSpace(this.repoPath) && fileSystem.DirectoryExists(Path.Combine(this.repoPath, ".github")) ?
                Result.Success() :
                Result.Failure(nameof(GitHubSourceService));
        }

        /// <inheritdoc cref="IGitHubSourceService.GetUserVerificationCodeAsync()"/>
        public async Task<Result<UserVerificationResponse, Error>> GetUserVerificationCodeAsync()
        {
            HttpRequestMessage requestMessage = this.httpClientAdapter.BuildRequest(
                HttpMethod.Post,
                string.Format(DeviceCodeUrlFormat, ClientId, Scope));

            HttpResponseMessage responseMessage = await this.httpClientAdapter.SendAsync(requestMessage);
            string responseContent = await responseMessage.Content.ReadAsStringAsync();
            if (responseMessage.IsSuccessStatusCode)
            {
                UserVerificationResponse response = JsonConvert.DeserializeObject<UserVerificationResponse>(responseContent);
                return Result.Success<UserVerificationResponse, Error>(response);
            }
            else
            {
                return Result.Failure<UserVerificationResponse, Error>(new GitHubServiceError(responseContent));
            }
        }

        /// <inheritdoc cref="IGitHubSourceService.GetCachedAccessTokenAsync(IGitHubClient)"/>
        public async Task<Maybe<Secret>> GetCachedAccessTokenAsync(IGitHubClient gitHubClient = null)
        {
            Maybe<Domain.Entities.Secret> accessToken = secretStoreRepository.ReadSecret(s_baseTargetUri);
            if (accessToken.HasValue)
            {
                string token = accessToken.Value.Value;

                if (gitHubClient == null)
                {
                    var connection = new Connection(new ProductHeaderValue(SecretsNamespace));
                    gitHubClient = new GitHubClient(connection)
                    {
                        Credentials = new Credentials(token, AuthenticationType.Bearer),
                    };
                }

                try
                {
                    // Validate the connection.
                    User user = await gitHubClient.User.Current();
                    return new Secret { Value = token };
                }
                catch (AuthorizationException)
                {
                    // Cached token is invalid, delete it.
                    _ = secretStoreRepository.DeleteSecret(s_baseTargetUri);
                }
            }

            return Maybe.None;
        }

        /// <inheritdoc cref="IGitHubSourceService.GetRequestedAccessTokenAsync(UserVerificationResponse)"/>
        public async Task<Result<Secret, Error>> GetRequestedAccessTokenAsync(UserVerificationResponse verificationResponse)
        {
            string accessToken = string.Empty;
            string url = string.Format(AccessTokenUrlFormat, ClientId, verificationResponse.DeviceCode);
            DateTime expireTime = DateTime.UtcNow.AddSeconds(verificationResponse.ExpiresInSeconds);
            JObject jObject = null;

            while (jObject == null || (string.IsNullOrEmpty(accessToken) && DateTime.UtcNow < expireTime))
            {
                // Delay per the interval provided in the response.
                await Task.Delay(verificationResponse.PollingIntervalSeconds * 1000);

                HttpRequestMessage requestMessage = this.httpClientAdapter.BuildRequest(
                    HttpMethod.Post,
                    url);

                HttpResponseMessage responseMessage = await this.httpClientAdapter.SendAsync(requestMessage);
                string responseContent = await responseMessage.Content.ReadAsStringAsync();

                jObject = JObject.Parse(responseContent);
                accessToken = jObject.Value<string>("access_token");
            }

            if (!string.IsNullOrEmpty(accessToken))
            {
                this.secretStoreRepository.WriteSecret(s_baseTargetUri, new Domain.Entities.Secret() { Value = accessToken });
                return Result.Success<Secret, Error>(new Secret { Value = accessToken });
            }
            else
            {
                return Result.Failure<Secret, Error>(new GitHubServiceError(jObject.Value<string>("error_description")));
            }
        }

        /// <inheritdoc cref="IResultSourceService.RequestAnalysisResultsAsync(object)"/>
        public async Task<Result<bool, ErrorType>> RequestAnalysisResultsAsync(object data = null)
        {
            Maybe<Secret> getAccessTokenResult = await GetCachedAccessTokenAsync();
            if (getAccessTokenResult.HasValue)
            {
                await this.UpdateBranchAndCommitHashAsync();
                this.InitializeFileWatchers();

                lock (this.pollingTask)
                {
                    _ = this.statusBarService.SetStatusTextAsync("Retrieving static analysis results...");

                    bool showInfoBar = data is IConvertible d && d.ToBoolean(null);

                    if (this.pollingTask.IsCompleted)
                    {
                        // Start polling but don't wait for task completion.
                        this.pollingTask = this.PollForUpdatedResultsAsync(showInfoBar);
                    }
                }

                return Result.Success<bool, ErrorType>(true);
            }
            else
            {
                // Start the auth process.
                await StartAuthSequenceAsync();
                return Result.Failure<bool, ErrorType>(ErrorType.AccessDenied);
            }
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

        internal async Task StartAuthSequenceAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            Result<UserVerificationResponse, Error> userCodeResult = await this.GetUserVerificationCodeAsync();
            if (userCodeResult.IsSuccess)
            {
                string userCode = userCodeResult.Value.UserCode;

                // Callback for the Copy Code button
                Task CopyCodeButtonCallback()
                {
                    Clipboard.SetText(userCode);
                    return Task.CompletedTask;
                }

                // The callback for the infobar button.
                Task VerifyButtonCallback()
                {
                    _ = WaitForUserVerificationAsync();
                    return Task.CompletedTask;
                }

                // Fire-and-forget method so the VS shell isn't waiting for Task completion.
                async Task WaitForUserVerificationAsync()
                {
                    using Process process = this.browserService.NavigateUrl(userCodeResult.Value.VerificationUri);

                    Result<Secret, Error> requestedTokenResult = await GetRequestedAccessTokenAsync(userCodeResult.Value);
                    if (requestedTokenResult.IsSuccess)
                    {
                        await this.CloseInfoBarAsync();

                        _ = RequestAnalysisResultsAsync();
                    }
                }

                Func<Task> copyCodeCallbackMethod = CopyCodeButtonCallback;
                Func<Task> verifyCallbackMethod = VerifyButtonCallback;

                var infoBarModel = new InfoBarModel(
                    textSpans: new[]
                    {
                        new InfoBarTextSpan("The Microsoft SARIF Viewer needs you to login to your GitHub account. Click the button and enter the verification code "),
                        new InfoBarTextSpan($"{userCode}", bold: true),
                    },
                    actionItems: new[]
                    {
                        new InfoBarButton("Copy Code", copyCodeCallbackMethod),
                        new InfoBarButton("Verify on GitHub", verifyCallbackMethod),
                    },
                    image: GitHubInfoBarHelper.GetInfoBarImageMoniker(),
                    isCloseButtonVisible: true);

                await this.ShowInfoBarAsync(infoBarModel);
            }
        }

        internal async Task<Result<SarifLog, string>> GetAnalysisResultsAsync(
            IHttpClientAdapter httpClientAdapter,
            string baseUrl,
            string analysisId,
            string accessToken)
        {
            if (analysisId == string.Empty)
            {
                return CreateEmptySarifLog();
            }
            else
            {
                HttpRequestMessage requestMessage = httpClientAdapter.BuildRequest(
                    HttpMethod.Get,
                    baseUrl + $"/{analysisId}",
                    bodyParameters: null,
                    "application/sarif+json",
                    accessToken);

                HttpResponseMessage responseMessage = await httpClientAdapter.SendAsync(requestMessage);
                if (responseMessage.IsSuccessStatusCode)
                {
                    using (Stream stream = await responseMessage.Content.ReadAsStreamAsync())
                    {
                        return SarifLog.Load(stream);
                    }
                }
                else
                {
                    return Result.Failure<SarifLog, string>(await responseMessage.Content.ReadAsStringAsync());
                }
            }
        }

        internal async Task<Result<string, ErrorType>> GetAnalysisIdAsync(
            IHttpClientAdapter httpClientAdapter,
            string baseUrl,
            string branch,
            string accessToken,
            string commitHash = null)
        {
            string lastAnalysisId = "0";
            string analysisId = null;
            int page = 1;
            int perPage = 100;

            while (analysisId == null)
            {
                string url = baseUrl + $"?ref=refs/heads/{branch}&page={page++}&per_page={perPage}";

                HttpRequestMessage requestMessage = httpClientAdapter.BuildRequest(
                    HttpMethod.Get,
                    url,
                    token: accessToken);

                HttpResponseMessage responseMessage = await httpClientAdapter.SendAsync(requestMessage);
                if (responseMessage.IsSuccessStatusCode)
                {
                    string content = await responseMessage.Content.ReadAsStringAsync();

                    JArray jArray = JsonConvert.DeserializeObject<JArray>(content);
                    if (jArray != null)
                    {
                        if (string.IsNullOrWhiteSpace(commitHash))
                        {
                            if (jArray.Count == 0)
                            {
                                if (lastAnalysisId != "0")
                                {
                                    // There were exactly {perPage} analyses in the previous response.
                                    analysisId = lastAnalysisId;
                                }

                                // Else, no results were returned.

                                break;
                            }

                            // Get the latest analysis.
                            string lastId = jArray.First["id"].Value<string>();
                            if (jArray.Count <= perPage)
                            {
                                analysisId = lastId;
                            }
                            else
                            {
                                // Remember the last analysis id, in case the next request returns zero analyses.
                                lastAnalysisId = lastId;
                            }
                        }
                        else
                        {
                            // Look for the analysis for a specific commit.
                            JToken scanResult = jArray
                                                    .Where(a => a["commit_sha"].Value<string>() == commitHash)
                                                    .FirstOrDefault();
                            if (scanResult != null)
                            {
                                // We found it, grab the analysis id.
                                analysisId = scanResult["id"].Value<string>();
                            }

                            break;
                        }
                    }
                    else
                    {
                        // Null JArray
                        break;
                    }
                }
                else
                {
                    // Unsuccessful request
                    break;
                }
            }

            return analysisId ?? Result.Failure<string, ErrorType>(ErrorType.AnalysesUnavailable);
        }

        internal async Task PollForUpdatedResultsAsync(bool showInfoBar = false)
        {
            Maybe<Secret> getAccessTokenResult = await GetCachedAccessTokenAsync();
            if (getAccessTokenResult.HasValue)
            {
                string accessToken = getAccessTokenResult.Value.Value;
                DateTime timeoutTime = DateTime.UtcNow.AddSeconds(ScanResultsPollTimeoutSeconds);

                if (showInfoBar)
                {
                    void CancelPollingCallback()
                    {
                        this.pollingCancellationTokenSource.Cancel();
                    }

                    Action cancelPollingCallbackMethod = CancelPollingCallback;

                    var infoBarModel = new InfoBarModel(
                        textSpans: new[]
                        {
                            new InfoBarTextSpan("The Microsoft SARIF Viewer is waiting for new static analysis results from GitHub"),
                        },
                        actionItems: new[]
                        {
                            new InfoBarButton("Cancel", cancelPollingCallbackMethod),
                        },
                        image: KnownMonikers.Activity,
                        isCloseButtonVisible: true);

                    await this.ShowInfoBarAsync(infoBarModel);
                }

                while (!this.pollingCancellationTokenSource.IsCancellationRequested && timeoutTime > DateTime.UtcNow)
                {
                    Result<string, ErrorType> getAnalysisIdResult = await GetAnalysisIdAsync(
                        httpClientAdapter,
                        codeScanningBaseApiUrl + GetAnalysesEndpoint,
                        this.scanDataRequestParameters.BranchName,
                        accessToken,
                        this.scanDataRequestParameters.CommitHash);

                    if (getAnalysisIdResult.IsSuccess)
                    {
                        Result<SarifLog, string> getResultsResult = await GetAnalysisResultsAsync(
                            httpClientAdapter,
                            codeScanningBaseApiUrl + GetAnalysesEndpoint,
                            getAnalysisIdResult.Value,
                            accessToken);

                        if (getResultsResult.IsSuccess)
                        {
                            var eventArgs = new GitRepoEventArgs
                            {
                                BranchName = this.scanDataRequestParameters.BranchName,
                                SarifLog = getResultsResult.Value,
                                LogFileName = "scan-results.sarif",
                                UseDotSarifDirectory = false,
                            };
                            RaiseServiceEvent(eventArgs);
                            break;
                        }
                    }

                    await Task.Delay(ScanResultsPollIntervalSeconds * 1000);
                }

                await CloseInfoBarAsync();
            }

            this.pollingCancellationTokenSource.Cancel();
            this.pollingCancellationTokenSource.Dispose();
            this.pollingCancellationTokenSource = new CancellationTokenSource();

            _ = this.statusBarService.ClearStatusTextAsync();
        }

        private static SarifLog CreateEmptySarifLog() => new SarifLog
            {
                Runs = new List<Run>
                {
                    new Run
                    {
                        Tool = new Tool(),
                        Results = new List<SarifResult>(),
                    },
                },
            };

        private async Task<ResultSourceServiceAction> DismissAlert_ExecuteAsync(MenuCommandInvokedEventArgs e)
        {
            if (e.SarifResults.Count == 1)
            {
                Maybe<Secret> getAccessTokenResult = await GetCachedAccessTokenAsync();
                if (getAccessTokenResult.HasValue)
                {
                    string accessToken = getAccessTokenResult.Value.Value;

                    Maybe<SarifResult> result = e.SarifResults.TryFirst();

                    if (result.HasValue)
                    {
                        SarifResult sarifResult = result.Value;

                        if (sarifResult.TryGetProperty<int>("github/alertNumber", out int alertNumber))
                        {
                            string url = this.codeScanningBaseApiUrl + string.Format(DismissAlertEndpointFormat, alertNumber) + $"?ref=refs/heads/{this.scanDataRequestParameters.BranchName}";

                            if (s_DismissAlertReasons.TryGetValue(e.MenuCommand.Text, out string reason))
                            {
                                HttpRequestMessage requestMessage = httpClientAdapter.BuildRequest(
                                    new HttpMethod("PATCH"),
                                    url,
                                    bodyParameters: new Dictionary<string, string>
                                    {
                                        { "state", "dismissed" },
                                        { "dismissed_reason", reason },
                                    },
                                    accept: "application/vnd.github+json",
                                    token: accessToken);

                                HttpResponseMessage responseMessage = await httpClientAdapter.SendAsync(requestMessage);

                                if (responseMessage.IsSuccessStatusCode)
                                {
                                    return ResultSourceServiceAction.DismissSelectedItem;
                                }
                                else
                                {
                                    string content = await responseMessage.Content.ReadAsStringAsync();
                                }
                            }
                        }
                    }
                }
            }

            return ResultSourceServiceAction.None;
        }

        private async Task<ResultSourceServiceAction> DismissAlerts_BeforeQueryStatusAsync(MenuCommandBeforeQueryStatusEventArgs e)
        {
            return e.SarifResults.Count == e.SelectedItemsCount
                ? await Task.FromResult(ResultSourceServiceAction.None)
                : await Task.FromResult(ResultSourceServiceAction.DisableMenuCommand);
        }

        private void RaiseServiceEvent(ServiceEventArgs eventArgs = null)
        {
            ServiceEvent?.Invoke(this, eventArgs);
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

        private async Task ShowInfoBarAsync(InfoBarModel infoBarModel)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            await this.CloseInfoBarAsync();
            this.infoBar = this.infoBarService.ShowInfoBar(infoBarModel);
        }

        private async Task CloseInfoBarAsync()
        {
            if (this.infoBar != null)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                _ = this.infoBarService.CloseInfoBar(this.infoBar);
                this.infoBar = null;
            }
        }
    }
}

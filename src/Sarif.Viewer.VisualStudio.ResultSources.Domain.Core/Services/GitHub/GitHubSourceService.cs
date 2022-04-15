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
using Microsoft.Sarif.Viewer.ResultSources.Domain.Abstractions;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Errors;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Models;
using Microsoft.Sarif.Viewer.Shell;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Octokit;

using Sarif.Viewer.VisualStudio.ResultSources.Domain.Core;
using Sarif.Viewer.VisualStudio.Shell.Core;

using Result = CSharpFunctionalExtensions.Result;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Services.GitHub
{
    /// <summary>
    /// GitHubSourceService class.
    /// </summary>
    public partial class GitHubSourceService : IGitHubSourceService
    {
        private const string SecretsNamespace = "microsoft-sarif-visualstudio-extension";
        private const string ClientId = "23c8243801d898f93624";
        private const string Scope = "security_events";
        private const string GitHubRepoUriPattern = @"^https://(www.)?github.com/(?<user>[a-z\d](?:[a-z\d]|-(?=[a-z\d])){0,38})/(?<repo>[a-z\d](?:[a-z\d]|-(?=[a-z\d])){0,38}).git$";
        private const string BaseUrl = "https://github.com";
        private const string DeviceCodeUrlFormat = "https://github.com/login/device/code?client_id={0}&scope={1}";
        private const string AccessTokenUrlFormat = "https://github.com/login/oauth/access_token?client_id={0}&device_code={1}&grant_type=urn:ietf:params:oauth:grant-type:device_code";
        private const string CodeScanningBaseApiUrlFormat = "https://api.github.com/repos/{0}/{1}/code-scanning/analyses";
        private const string GitLocalRefFileBaseRelativePath = @".git\refs\remotes\origin";
        private const int ScanResultsPollIntervalSeconds = 10;
        private const int ScanResultsPollTimeoutSeconds = 1200;

        private static readonly TargetUri s_baseTargetUri = new TargetUri(BaseUrl);

        private readonly IFileSystem fileSystem;
        private readonly IGitExe gitExe;

        private IServiceProvider serviceProvider;
        private IHttpClientAdapter httpClientAdapter;
        private ISecretStoreRepository secretStoreRepository;
        private IFileWatcher fileWatcherBranchChange;
        private IFileWatcher fileWatcherGitPush;
        private string repoPath;
        private string codeScanningBaseApiUrl;
        private InfoBarService infoBarService;
        private StatusBarService statusBarService;
        private IVsInfoBarUIElement infoBar;
        private CancellationTokenSource pollingCancellationTokenSource;
        private CancellationTokenSource statusBarCancellationTokenSource;
        private bool isPolling;

        private (string BranchName, string CommitHash) scanDataRequestParameters;

        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubSourceService"/> class.
        /// </summary>
        /// <param name="solutionRootPath">The full path of the solution directory.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="gitExe">The git.exe helper.</param>
        public GitHubSourceService(
            string solutionRootPath,
            IFileSystem fileSystem,
            IGitExe gitExe)
        {
            this.fileSystem = fileSystem;
            this.gitExe = gitExe;
            this.gitExe.RepoPath = solutionRootPath;
        }

        /// <inheritdoc cref="IResultSourceService.ResultsUpdated"/>
        public event EventHandler<ResultsUpdatedEventArgs> ResultsUpdated;

        /// <inheritdoc cref="IGitHubSourceService.InitializeAsync(IServiceProvider, IHttpClientAdapter, ISecretStoreRepository, IFileWatcher, IFileWatcher)"/>
        public async Task InitializeAsync(
            IServiceProvider serviceProvider,
            IHttpClientAdapter httpClientAdapter,
            ISecretStoreRepository secretStoreRepository,
            IFileWatcher fileWatcherBranchChange,
            IFileWatcher fileWatcherGitPush)
        {
            this.serviceProvider = serviceProvider;
            this.httpClientAdapter = httpClientAdapter;
            this.secretStoreRepository = secretStoreRepository;
            this.fileWatcherBranchChange = fileWatcherBranchChange;
            this.fileWatcherGitPush = fileWatcherGitPush;

            this.repoPath ??= await gitExe.GetRepoRootAsync();
            string repoUrl = await gitExe.GetRepoUriAsync();

            Match match = Regex.Match(repoUrl, GitHubRepoUriPattern);
            if (match.Success)
            {
                codeScanningBaseApiUrl = string.Format(CodeScanningBaseApiUrlFormat, match.Groups["user"], match.Groups["repo"]);
            }

            this.pollingCancellationTokenSource = new CancellationTokenSource();
            this.statusBarCancellationTokenSource = new CancellationTokenSource();

            this.infoBarService = new InfoBarService(this.serviceProvider);
            this.statusBarService = new StatusBarService(this.serviceProvider);
        }

        /// <inheritdoc cref="IGitHubSourceService.IsGitHubProject()"/>
        public async Task<bool> IsGitHubProjectAsync()
        {
            if (string.IsNullOrWhiteSpace(this.repoPath))
            {
                this.repoPath = await gitExe.GetRepoRootAsync();
            }

            return fileSystem.DirectoryExists(Path.Combine(this.repoPath, ".github"));
        }

        /// <inheritdoc cref="IGitHubSourceService.GetUserVerificationCodeAsync()"/>
        public async Task<Result<UserVerificationResponse, Error>> GetUserVerificationCodeAsync()
        {
            HttpRequestMessage requestMessage = this.httpClientAdapter.BuildRequest(
                HttpMethod.Post,
                string.Format(DeviceCodeUrlFormat, ClientId, Scope));

            HttpResponseMessage responseMessage = await this.httpClientAdapter.SendAsync(requestMessage);
            if (responseMessage.IsSuccessStatusCode)
            {
                UserVerificationResponse response = JsonConvert.DeserializeObject<UserVerificationResponse>(await responseMessage.Content.ReadAsStringAsync());
                return Result.Success<UserVerificationResponse, Error>(response);
            }
            else
            {
                return Result.Failure<UserVerificationResponse, Error>(new GitHubServiceError(await responseMessage.Content.ReadAsStringAsync()));
            }
        }

        /// <inheritdoc cref="IGitHubSourceService.GetCachedAccessTokenAsync(IGitHubClient)"/>
        public async Task<Maybe<Models.AccessToken>> GetCachedAccessTokenAsync(IGitHubClient gitHubClient = null)
        {
            Maybe<Entities.AccessToken> accessToken = secretStoreRepository.ReadAccessToken(s_baseTargetUri);
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
                    return new Models.AccessToken { Value = token };
                }
                catch (AuthorizationException)
                {
                    // Cached token is invalid, delete it.
                    _ = secretStoreRepository.DeleteAccessToken(s_baseTargetUri);
                }
            }

            return null;
        }

        /// <inheritdoc cref="IGitHubSourceService.GetRequestedAccessTokenAsync(UserVerificationResponse)"/>
        public async Task<Result<Models.AccessToken, Error>> GetRequestedAccessTokenAsync(UserVerificationResponse verificationResponse)
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
                this.secretStoreRepository.WriteAccessToken(s_baseTargetUri, new Entities.AccessToken() { Value = accessToken });
                return Result.Success<Models.AccessToken, Error>(new Models.AccessToken { Value = accessToken });
            }
            else
            {
                return Result.Failure<Models.AccessToken, Error>(new GitHubServiceError(jObject.Value<string>("error_description")));
            }
        }

        /// <inheritdoc cref="IResultSourceService.RequestCodeAnalysisScanResultsAsync()"/>
        public async Task<Result<bool, ErrorType>> RequestAnalysisScanResultsAsync(object data = null)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            Maybe<Models.AccessToken> getAccessTokenResult = await GetCachedAccessTokenAsync();
            if (getAccessTokenResult.HasValue)
            {
                await this.InitializeFileWatchersAsync();

                string branch = await gitExe.GetCurrentBranchAsync();
                string commitHash = await gitExe.GetCurrentCommitHashAsync();
                this.scanDataRequestParameters.BranchName = branch;
                this.scanDataRequestParameters.CommitHash = commitHash;

                CancellationToken token = this.statusBarCancellationTokenSource.Token;

                _ = this.statusBarService.AnimateStatusTextAsync(
                    "Retrieving static analysis results{0}",
                    new[] { string.Empty, ".", "..", "..." },
                    400,
                    token);

                bool showInfoBar = data is IConvertible d && d.ToBoolean(null);

                // Start polling but don't wait for task completion.
                _ = this.PollForUpdatedResultsAsync(showInfoBar);
                return Result.Success<bool, ErrorType>(true);
            }
            else
            {
                // Start the auth process.
                Result<bool, ErrorType> startAuthResult = await StartAuthSequenceAsync();
                return Result.Failure<bool, ErrorType>(startAuthResult.Error);
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

        private static SarifLog CreateEmptySarifLog()
        {
            return new SarifLog
            {
                Runs = new List<Run>
                {
                    new Run
                    {
                        Tool = new Tool(),
                        Results = new List<Microsoft.CodeAnalysis.Sarif.Result>(),
                    },
                },
            };
        }

        private async Task<Result<bool, ErrorType>> StartAuthSequenceAsync()
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
                async Task VerifyButtonCallback()
                {
                    var processStartInfo = new ProcessStartInfo()
                    {
                        FileName = userCodeResult.Value.VerificationUri,
                        UseShellExecute = true,
                    };
                    Process.Start(processStartInfo);

                    Result<Models.AccessToken, Error> requestedTokenResult = await GetRequestedAccessTokenAsync(userCodeResult.Value);
                    if (requestedTokenResult.IsSuccess)
                    {
                        await this.CloseInfoBarAsync();

                        _ = RequestAnalysisScanResultsAsync();
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

                return Result.Failure<bool, ErrorType>(ErrorType.WaitingForUserVerification);
            }

            return Result.Failure<bool, ErrorType>(ErrorType.MissingAccessToken);
        }

        private async Task<Result<SarifLog, string>> GetAnalysisResultsAsync(
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

        private async Task<Result<string, ErrorType>> GetAnalysisIdAsync(
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

        private async Task PollForUpdatedResultsAsync(bool showInfoBar = false)
        {
            Maybe<Models.AccessToken> getAccessTokenResult = await GetCachedAccessTokenAsync();
            if (getAccessTokenResult.HasValue)
            {
                string accessToken = getAccessTokenResult.Value.Value;
                DateTime timeoutTime = DateTime.UtcNow.AddSeconds(ScanResultsPollTimeoutSeconds);
                this.isPolling = true;

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
                        codeScanningBaseApiUrl,
                        this.scanDataRequestParameters.BranchName,
                        accessToken,
                        this.scanDataRequestParameters.CommitHash);

                    if (getAnalysisIdResult.IsSuccess)
                    {
                        Result<SarifLog, string> getResultsResult = await GetAnalysisResultsAsync(
                            httpClientAdapter,
                            codeScanningBaseApiUrl,
                            getAnalysisIdResult.Value,
                            accessToken);

                        if (getResultsResult.IsSuccess)
                        {
                            var eventArgs = new GitRepoEventArgs
                            {
                                BranchName = this.scanDataRequestParameters.BranchName,
                                SarifLog = getResultsResult.Value,
                            };
                            RaiseResultsUpdatedEvent(eventArgs);
                            break;
                        }
                    }

                    await Task.Delay(ScanResultsPollIntervalSeconds * 1000);
                }

                this.isPolling = false;
                await CloseInfoBarAsync();
            }

            this.pollingCancellationTokenSource.Cancel();
            this.pollingCancellationTokenSource.Dispose();
            this.pollingCancellationTokenSource = new CancellationTokenSource();

            this.statusBarCancellationTokenSource.Cancel();
            this.statusBarCancellationTokenSource.Dispose();
            this.statusBarCancellationTokenSource = new CancellationTokenSource();
        }

        private void RaiseResultsUpdatedEvent(GitRepoEventArgs eventArgs = null)
        {
            ResultsUpdated?.Invoke(this, eventArgs);
        }

        private async Task InitializeFileWatchersAsync()
        {
            string branch = await gitExe.GetCurrentBranchAsync();

            this.fileWatcherBranchChange.FilePath = Path.Combine(repoPath, ".git");
            this.fileWatcherBranchChange.Filter = "HEAD";
            this.fileWatcherBranchChange.FileRenamed += this.FileWatcherBranchChange_FileRenamed;
            this.fileWatcherBranchChange.Start();

            this.fileWatcherGitPush.FileRenamed += this.FileWatcherGitPush_Renamed;
            this.SetBranchRefFileWatcherPath(branch);
            this.fileWatcherGitPush.Start();
        }

        private void SetBranchRefFileWatcherPath(string branchName)
        {
            (string Path, string Name) parsedBranch = ParseBranchString(branchName);

            this.fileWatcherGitPush.DisableRaisingEvents();
            this.fileWatcherGitPush.FilePath = Path.Combine(repoPath, GitLocalRefFileBaseRelativePath, parsedBranch.Path);
            this.fileWatcherGitPush.Filter = parsedBranch.Name;
            this.fileWatcherGitPush.EnableRaisingEvents();
        }

        private void FileWatcherBranchChange_FileRenamed(object sender, FileSystemEventArgs e)
        {
            OnCurrentBranchChangedAsync().ConfigureAwait(false).GetAwaiter();
        }

        private void FileWatcherGitPush_Renamed(object sender, FileSystemEventArgs e)
        {
            OnBranchPushEventAsync().ConfigureAwait(false).GetAwaiter();
        }

        private async Task OnCurrentBranchChangedAsync()
        {
            string branchName = await gitExe.GetCurrentBranchAsync();
            this.SetBranchRefFileWatcherPath(branchName);

            this.scanDataRequestParameters.BranchName = branchName;
            this.scanDataRequestParameters.CommitHash = await this.gitExe.GetCurrentCommitHashAsync();

            if (!this.isPolling)
            {
                _ = RequestAnalysisScanResultsAsync();
            }
        }

        private async Task OnBranchPushEventAsync()
        {
            this.scanDataRequestParameters.BranchName = await this.gitExe.GetCurrentBranchAsync();
            this.scanDataRequestParameters.CommitHash = await this.gitExe.GetCurrentCommitHashAsync();

            if (!this.isPolling)
            {
                _ = RequestAnalysisScanResultsAsync(data: true);
            }
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

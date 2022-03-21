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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Octokit;

using Result = CSharpFunctionalExtensions.Result;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Services.GitHub
{
    public class GitHubSourceService : IGitHubSourceService
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

        private static readonly TargetUri baseTargetUri = new TargetUri(BaseUrl);

        private readonly string repoPath;
        private readonly IServiceProvider serviceProvider;
        private readonly AsyncLazy<string> vsInstallDir;
        private readonly IFileSystem fileSystem;
        private readonly GitHelper gitHelper;

        private string codeScanningBaseApiUrl;
        private ISecretStoreRepository secretStoreRepository;
        private FileSystemWatcher fileWatcherBranchChange;
        private FileSystemWatcher fileWatcherGitPush;
        private IVsInfoBarUIElement infoBar;

        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubSourceService"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="solutionPath">The path of the current solution.</param>
        public GitHubSourceService(
            IServiceProvider serviceProvider,
            string solutionPath)
        {
            this.serviceProvider = serviceProvider;
            this.vsInstallDir = new AsyncLazy<string>(this.GetVsInstallDirectoryAsync, ThreadHelper.JoinableTaskFactory);
            this.fileSystem = FileSystem.Instance;
            this.gitHelper = new GitHelper(fileSystem);
            this.repoPath = this.gitHelper.GetRepositoryRoot(solutionPath);
        }

        /// <inheritdoc cref="IResultSourceService.ResultsUpdated"/>
        public event EventHandler<ResultsUpdatedEventArgs> ResultsUpdated;

        /// <summary>
        /// Initializes the service instance.
        /// </summary>
        /// <param name="secretStoreRepository">The <see cref="ISecretStoreRepository"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task InitializeAsync(ISecretStoreRepository secretStoreRepository)
        {
            string branch = await this.GetCurrentBranchAsync();
            string repoUrl = await GetRepoUriAsync();
            Match match = Regex.Match(repoUrl, GitHubRepoUriPattern);

            if (match.Success)
            {
                codeScanningBaseApiUrl = string.Format(CodeScanningBaseApiUrlFormat, match.Groups["user"], match.Groups["repo"]);
            }

            this.secretStoreRepository = secretStoreRepository;
            InfoBarService.Initialize(this.serviceProvider);
            StatusBarService.Initialize(this.serviceProvider);

            this.fileWatcherBranchChange = new FileSystemWatcher(Path.Combine(repoPath, ".git"), "HEAD");
            this.fileWatcherBranchChange.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            this.fileWatcherBranchChange.Renamed += this.FileWatcher_BranchChange_Renamed;
            this.fileWatcherBranchChange.EnableRaisingEvents = true;

            this.fileWatcherGitPush = new FileSystemWatcher();
            this.SetBranchRefFileWatcherPath(branch);
            this.fileWatcherGitPush.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            this.fileWatcherGitPush.Renamed += this.FileWatcher_BranchFile_Changed;
            this.fileWatcherGitPush.EnableRaisingEvents = true;
        }

        /// <inheritdoc cref="IGitHubSourceService.IsGitHubProject()"/>
        public bool IsGitHubProject()
        {
            return fileSystem.DirectoryExists(Path.Combine(repoPath, ".github"));
        }

        /// <inheritdoc cref="IGitHubSourceService.GetUserVerificationCodeAsync()"/>
        public async Task<Result<UserVerificationResponse, Error>> GetUserVerificationCodeAsync()
        {
            HttpResponseMessage responseMessage = await HttpUtility.GetHttpResponseAsync(
                HttpMethod.Post,
                string.Format(DeviceCodeUrlFormat, ClientId, Scope));

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

        /// <inheritdoc cref="IGitHubSourceService.GetCachedAccessTokenAsync()"/>
        public async Task<Maybe<Models.AccessToken>> GetCachedAccessTokenAsync()
        {
            Maybe<Entities.AccessToken> accessToken = secretStoreRepository.ReadAccessToken(baseTargetUri);

            if (accessToken.HasValue)
            {
                string token = accessToken.Value.Value;
                var connection = new Connection(new ProductHeaderValue(SecretsNamespace))
                {
                    Credentials = new Credentials(token, AuthenticationType.Bearer),
                };
                var gitHubClient = new GitHubClient(connection);

                try
                {
                    // Validate the connection.
                    User user = await gitHubClient.User.Current();
                    return new Models.AccessToken { Value = token };
                }
                catch (AuthorizationException)
                {
                    // Cached token is invalid, delete it.
                    _ = secretStoreRepository.DeleteAccessToken(baseTargetUri);
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

            while (string.IsNullOrEmpty(accessToken) && DateTime.UtcNow < expireTime)
            {
                // Delay per the interval provided in the response.
                await Task.Delay(verificationResponse.PollingIntervalSeconds * 1000);

                HttpResponseMessage responseMessage = await HttpUtility.GetHttpResponseAsync(HttpMethod.Post, url);
                string responseContent = await responseMessage.Content.ReadAsStringAsync();

                jObject = JObject.Parse(responseContent);
                accessToken = jObject.Value<string>("access_token");
            }

            if (!string.IsNullOrEmpty(accessToken))
            {
                this.secretStoreRepository.WriteAccessToken(baseTargetUri, new Entities.AccessToken() { Value = accessToken });
                return Result.Success<Models.AccessToken, Error>(new Models.AccessToken { Value = accessToken });
            }
            else
            {
                return Result.Failure<Models.AccessToken, Error>(new GitHubServiceError(jObject.Value<string>("error_description")));
            }
        }

        /// <inheritdoc cref="IResultSourceService.GetCodeAnalysisScanResultsAsync()"/>
        public async Task<Result<SarifLog, ErrorType>> GetCodeAnalysisScanResultsAsync()
        {
            SarifLog sarifLog = null;
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken cancellationToken = source.Token;

            Maybe<Models.AccessToken> getAccessTokenResult = await GetCachedAccessTokenAsync();

            if (getAccessTokenResult.HasValue)
            {
                string accessToken = getAccessTokenResult.Value.Value;
                string branch = await this.GetCurrentBranchAsync();

                _ = Task.Run(async () => await StatusBarService.Instance.AnimateStatusTextAsync("Getting static analysis results{0}", new[] { string.Empty, ".", "..", "..." }, 400, cancellationToken), cancellationToken);

                Result<string, ErrorType> latestIdResult = await GetAnalysisIdAsync(codeScanningBaseApiUrl, branch, accessToken);

                if (latestIdResult.IsSuccess)
                {
                    Result<SarifLog, string> getLogResult = await GetAnalysisResultsAsync(codeScanningBaseApiUrl, latestIdResult.Value, accessToken);
                    StatusBarService.Instance.ClearStatusText();

                    if (getLogResult.IsSuccess)
                    {
                        sarifLog = getLogResult.Value;
                    }
                }
            }
            else
            {
                // Start the auth process.
                Result<bool, ErrorType> startAuthResult = await StartAuthSequenceAsync();
                return Result.Failure<SarifLog, ErrorType>(startAuthResult.Error);
            }

            source.Cancel();
            return sarifLog ?? Result.Failure<SarifLog, ErrorType>(ErrorType.AnalysesUnavailable);
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

                // The callback for the infobar button.
                async Task VerifyButtonCallback()
                {
                    Clipboard.SetText(userCode);
                    var processStartInfo = new ProcessStartInfo()
                    {
                        FileName = userCodeResult.Value.VerificationUri,
                        UseShellExecute = true,
                    };
                    Process.Start(processStartInfo);

                    Result<Models.AccessToken, Error> requestedTokenResult = await GetRequestedAccessTokenAsync(userCodeResult.Value);

                    if (requestedTokenResult.IsSuccess)
                    {
                        InfoBarService.Instance.CloseInfoBar(this.infoBar);

                        // Inform listeners that updated results are available.
                        this.RaiseResultsUpdatedEvent();
                    }
                }

                Func<Task> callbackMethod = VerifyButtonCallback;

                var infoBarModel = new InfoBarModel(
                    textSpans: new[]
                    {
                        new InfoBarTextSpan("The Microsoft SARIF Viewer needs you to login to your GitHub account. Click the button and enter verification code "),
                        new InfoBarTextSpan($"{userCode}", bold: true),
                    },
                    actionItems: new[]
                    {
                        new InfoBarButton("Verify on GitHub", callbackMethod),
                    },
                    image: GitHubInfoBarHelper.GetInfoBarImageMoniker(),
                    isCloseButtonVisible: true);

                await ShowInfoBarAsync(infoBarModel);

                return Result.Failure<bool, ErrorType>(ErrorType.WaitingForUserVerification);
            }

            return Result.Failure<bool, ErrorType>(ErrorType.MissingAccessToken);
        }

        private async Task<Result<SarifLog, string>> GetAnalysisResultsAsync(string baseUrl, string analysisId, string accessToken)
        {
            if (analysisId == string.Empty)
            {
                return CreateEmptySarifLog();
            }
            else
            {
                HttpResponseMessage responseMessage = await HttpUtility.GetHttpResponseAsync(HttpMethod.Get, baseUrl + $"/{analysisId}", "application/sarif+json", accessToken);

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
                // What does this endpoint do if we request past the end of the list?
                string url = baseUrl + $"?ref=refs/heads/{branch}&page={page++}&per_page={perPage}";
                HttpResponseMessage responseMessage = await HttpUtility.GetHttpResponseAsync(HttpMethod.Get, url, token: accessToken);

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

                            // Get the latest analysis
                            string lastId = jArray.Last["id"].Value<string>();
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
                            // Look for the analysis for a specific commit
                            JToken scanResult = jArray
                                                    .Where(a => a["commit_sha"].Value<string>() == commitHash)
                                                    .FirstOrDefault();

                            // We found it, grab the analysis id.
                            if (scanResult != null)
                            {
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

        private void SetBranchRefFileWatcherPath(string branchName)
        {
            (string Path, string Name) parsedBranch = ParseBranchString(branchName);

            this.fileWatcherGitPush.EnableRaisingEvents = false;
            this.fileWatcherGitPush.Path = Path.Combine(repoPath, GitLocalRefFileBaseRelativePath, parsedBranch.Path);
            this.fileWatcherGitPush.Filter = parsedBranch.Name;
            this.fileWatcherGitPush.EnableRaisingEvents = true;
        }

        private (string Path, string Name) ParseBranchString(string branch)
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

        private void FileWatcher_BranchChange_Renamed(object sender, FileSystemEventArgs e)
        {
            OnCurrentBranchChanged();
        }

        private void FileWatcher_BranchFile_Changed(object sender, FileSystemEventArgs e)
        {
            OnBranchPushEvent(e.FullPath);
        }

        private void OnCurrentBranchChanged()
        {
            string branchName = gitHelper.GetCurrentBranch(this.repoPath);
            this.SetBranchRefFileWatcherPath(branchName);
            this.RaiseResultsUpdatedEvent(new GitRepoEventArgs() { BranchName = branchName });
        }

        private void OnBranchPushEvent(string branchFilePath)
        {
            this.fileWatcherGitPush.EnableRaisingEvents = false;

            string commitHash = File.ReadAllText(branchFilePath)?.TrimEnd('\n');
            string currentBranch = gitHelper.GetCurrentBranch(this.repoPath);

            // Start polling for updated scan results.
            _ = Task.Run(() => PollForUpdatedResultsAsync(currentBranch, commitHash));
        }

        private async Task PollForUpdatedResultsAsync(string branchName, string commitHash)
        {
            Maybe<Models.AccessToken> getAccessTokenResult = await GetCachedAccessTokenAsync();

            if (getAccessTokenResult.HasValue)
            {
                string accessToken = getAccessTokenResult.Value.Value;
                DateTime timeoutTime = DateTime.UtcNow.AddSeconds(ScanResultsPollTimeoutSeconds);

                var infoBarModel = new InfoBarModel(
                    textSpans: new[]
                    {
                        new InfoBarTextSpan("The Microsoft SARIF Viewer is waiting for new static analysis results from GitHub"),
                    },
                    image: KnownMonikers.Activity,
                    isCloseButtonVisible: true);

                await ShowInfoBarAsync(infoBarModel);

                while (timeoutTime > DateTime.UtcNow)
                {
                    await Task.Delay(ScanResultsPollIntervalSeconds * 1000);
                    Result<string, ErrorType> getAnalysisIdResult = await GetAnalysisIdAsync(codeScanningBaseApiUrl, branchName, accessToken, commitHash);

                    if (getAnalysisIdResult.IsSuccess)
                    {
                        Result<SarifLog, string> getResultsResult = await GetAnalysisResultsAsync(codeScanningBaseApiUrl, getAnalysisIdResult.Value, accessToken);

                        if (getResultsResult.IsSuccess)
                        {
                            await CloseInfoBarAsync(this.infoBar);

                            var eventArgs = new GitRepoEventArgs
                            {
                                BranchName = branchName,
                                SarifLog = getResultsResult.Value,
                            };
                            RaiseResultsUpdatedEvent(eventArgs);
                            break;
                        }
                    }
                }
            }
        }

        private void RaiseResultsUpdatedEvent(GitRepoEventArgs eventArgs = null)
        {
            ResultsUpdated?.Invoke(this, eventArgs);
        }

        private async Task ShowInfoBarAsync(InfoBarModel infoBarModel)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            Result<IVsInfoBarUIElement> showInfoBarResult = InfoBarService.Instance.ShowInfoBar(infoBarModel);

            if (showInfoBarResult.IsSuccess)
            {
                this.infoBar = showInfoBarResult.Value;
            }
        }

        private async Task CloseInfoBarAsync(IVsInfoBarUIElement element)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            _ = InfoBarService.Instance.CloseInfoBar(element);
        }

        private async ValueTask<string> GetRepoUriAsync() // TODO: <string?>
        {
            return await ExecuteGitCommandAsync("config --get remote.origin.url");
        }

        private async ValueTask<string> GetCurrentBranchAsync() // TODO: <string?>
        {
            return await ExecuteGitCommandAsync("symbolic-ref --short HEAD");
        }

        private async ValueTask<string> ExecuteGitCommandAsync(string arguments)
        {
            // Get the trusted min Git executable path.
            string minGitPath = Path.Combine(await this.vsInstallDir.GetValueAsync(), @"CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\mingw32\bin\git.exe");

            await TaskScheduler.Default;
            try
            {
                var processInfo = new ProcessStartInfo()
                {
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    Arguments = arguments,
                    WorkingDirectory = repoPath,
                    FileName = minGitPath,
                };

                var process = Process.Start(processInfo);
                process.WaitForExit();
                string repoUri = await process.StandardOutput.ReadLineAsync();
                return repoUri;
            }
            catch
            {
                // Ignore all exceptions and return default value.
            }

            return null;
        }

        private async Task<string> GetVsInstallDirectoryAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IVsShell vsShell = this.serviceProvider.GetService<SVsShell, IVsShell>();
            Assumes.NotNull(vsShell);

            ErrorHandler.ThrowOnFailure(vsShell.GetProperty((int)__VSSPROPID.VSSPROPID_InstallDirectory, out object installDirObject));
            Assumes.True(installDirObject is string);
            return (string)installDirObject;
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

using CSharpFunctionalExtensions;

using Microsoft.Alm.Authentication;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Abstractions;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Errors;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Models;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Octokit;

using Result = CSharpFunctionalExtensions.Result;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Services.GitHub
{
    public class GitHubSourceService : IGitHubSourceService
    {
        private const string SecretsNamespace = "microsoft-sarif-visualstudio-extension";
        private const string ClientId = "c0c99f438d4b6279879e";
        private const string Scope = "security_events";
        private const string GitHubRepoUriPattern = @"^https://(www.)?github.com/(?<user>[a-z\d](?:[a-z\d]|-(?=[a-z\d])){0,38})/(?<repo>[a-z\d](?:[a-z\d]|-(?=[a-z\d])){0,38}).git$";
        private const string BaseUrl = "https://github.com";
        private const string DeviceCodeUrl = "https://github.com/login/device/code?client_id={0}&scope={1}";
        private const string AccessTokenUrl = "https://github.com/login/oauth/access_token?client_id={0}&device_code={1}&grant_type=urn:ietf:params:oauth:grant-type:device_code";
        private const string CodeScanningBaseApiUrl = "https://api.github.com/repos/{0}/{1}/code-scanning/analyses";

        private static readonly TargetUri baseTargetUri = new TargetUri(BaseUrl);

        private readonly string repoPath;
        private readonly IServiceProvider serviceProvider;
        private readonly AsyncLazy<string> vsInstallDir;
        private readonly IFileSystem fileSystem;
        private readonly GitHelper gitHelper;

        private ISecretStoreRepository secretStoreRepository;
        private FileSystemWatcher fileWatcher;
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

        /// <inheritdoc cref="IResultSourceService.ResultsUpdatedEvent"/>
        public event EventHandler ResultsUpdatedEvent;

        /// <summary>
        /// Initializes the service instance.
        /// </summary>
        /// <param name="secretStoreRepository">The <see cref="ISecretStoreRepository"/>.</param>
        public void Initialize(ISecretStoreRepository secretStoreRepository)
        {
            this.secretStoreRepository = secretStoreRepository;

            try
            {
                this.fileWatcher = new FileSystemWatcher(Path.Combine(repoPath, ".git"), "HEAD");
                this.fileWatcher.Created += this.FileWatcher_Created;
                this.fileWatcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
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
                string.Format(DeviceCodeUrl, ClientId, Scope));

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
                var connection = new Connection(new ProductHeaderValue(SecretsNamespace));
                connection.Credentials = new Credentials(token, AuthenticationType.Bearer);
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
            string url = string.Format(AccessTokenUrl, ClientId, verificationResponse.DeviceCode);
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

            Maybe<Models.AccessToken> result = await GetCachedAccessTokenAsync();

            if (result.HasValue)
            {
                Models.AccessToken accessToken = result.Value;
                string repoUrl = await GetRepoUriAsync();
                Match match = Regex.Match(repoUrl, GitHubRepoUriPattern);

                if (match.Success)
                {
                    string branch = await this.GetCurrentBranchAsync();
                    string url = string.Format(CodeScanningBaseApiUrl, match.Groups["user"], match.Groups["repo"]);

                    HttpResponseMessage responseMessage = await HttpUtility.GetHttpResponseAsync(HttpMethod.Get, url + $"?ref=refs/heads/{branch}&per_page=1", token: accessToken.Value);

                    if (responseMessage.IsSuccessStatusCode)
                    {
                        string content = await responseMessage.Content.ReadAsStringAsync();

                        JArray jArray = JsonConvert.DeserializeObject<JArray>(content);

                        if (jArray.Count > 0)
                        {
                            string firstId = jArray?[0]["id"].Value<string>();
                            responseMessage = await HttpUtility.GetHttpResponseAsync(HttpMethod.Get, url + $"/{firstId}", "application/sarif+json", accessToken.Value);

                            if (responseMessage.IsSuccessStatusCode)
                            {
                                using (Stream stream = await responseMessage.Content.ReadAsStreamAsync())
                                {
                                    sarifLog = SarifLog.Load(stream);
                                }
                            }
                        }
                        else
                        {
                            // No results were returned, so construct a valid, empty log.
                            sarifLog = new SarifLog
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
                    }
                }
                else
                {
                    return Result.Failure<SarifLog, ErrorType>(ErrorType.IncompatibleRepoUrl);
                }
            }
            else
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Start the auth process.
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

                    var infoBar = new InfoBarModel(
                        textSpans: new[]
                        {
                            new InfoBarTextSpan("The Microsoft SARIF Viewer needs you to login to your GitHub account. Click the button and enter verification code "),
                            new InfoBarTextSpan($"{userCode}", bold: true),
                        },
                        actionItems: new[]
                        {
                            new InfoBarButton("Verify on GitHub", callbackMethod),
                        },
                        image: KnownMonikers.GitHub,
                        isCloseButtonVisible: true);

                    InfoBarService.Initialize(this.serviceProvider);
                    Result<IVsInfoBarUIElement> showInfoBarResult = InfoBarService.Instance.ShowInfoBar(infoBar);

                    if (showInfoBarResult.IsSuccess)
                    {
                        this.infoBar = showInfoBarResult.Value;
                    }

                    return Result.Failure<SarifLog, ErrorType>(ErrorType.WaitingForUserVerification);
                }

                return Result.Failure<SarifLog, ErrorType>(ErrorType.MissingAccessToken);
            }

            return sarifLog;
        }

        private void FileWatcher_Created(object sender, FileSystemEventArgs e)
        {
            if (e.Name == "HEAD" && e.ChangeType == WatcherChangeTypes.Created)
            {
                this.RaiseResultsUpdatedEvent(new GitRepoEventArgs() { BranchName = gitHelper.GetCurrentBranch(repoPath) });
            }
        }

        private void RaiseResultsUpdatedEvent(GitRepoEventArgs eventArgs = null)
        {
            ResultsUpdatedEvent?.Invoke(this, eventArgs ?? EventArgs.Empty);
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

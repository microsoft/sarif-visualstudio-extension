// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TeamFoundation.Git.Extensibility;
using Microsoft.VisualStudio.Threading;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Sarif.Viewer.ResultSources.Services
{
    public class GitHubService : IGitHubService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IGitExt2 gitService;
        private readonly GitHubAuthenticationHelper authenticationHelper;
        private readonly AsyncLazy<string> vsInstallDir;

        private const string GitHubRepoUriPattern = @"^https://(www.)?github.com/(?<user>[a-z\d](?:[a-z\d]|-(?=[a-z\d])){0,38})/(?<repo>[a-z\d](?:[a-z\d]|-(?=[a-z\d])){0,38}).git$";
        private const string BaseApiUrl = "https://api.github.com/repos/{0}/{1}/code-scanning/analyses";

        public GitHubService(
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this.gitService = this.serviceProvider.GetService<IGitExt, IGitExt2>() as IGitExt2;
            this.authenticationHelper = new GitHubAuthenticationHelper();
            this.vsInstallDir = new AsyncLazy<string>(this.GetVsInstallDirectoryAsync, ThreadHelper.JoinableTaskFactory);
        }

        public async Task<SarifLog> GetCodeAnalysisScanResultsAsync(string path)
        {
            SarifLog sarifLog = null;

            await authenticationHelper.InitializeAsync();
            string token = await authenticationHelper.GetGitHubTokenAsync();

            IGitRepositoryInfo repoInfo = await this.GetRepoAsync(path);
            string repoUrl = await GetRepoUriAsync(path);
            Match match = Regex.Match(repoUrl, GitHubRepoUriPattern);

            if (match.Success)
            {
                string url = string.Format(BaseApiUrl, match.Groups["user"], match.Groups["repo"]);

                HttpResponseMessage responseMessage = await HttpUtility.GetHttpResponseAsync(HttpMethod.Get, url + $"?ref=refs/heads/{repoInfo.CurrentBranch.Name}&per_page=1", token: token);

                if (responseMessage.IsSuccessStatusCode)
                {
                    string content = await responseMessage.Content.ReadAsStringAsync();

                    JArray jArray = JsonConvert.DeserializeObject<JArray>(content);
                    string firstId = jArray?[0]["id"].Value<string>();
                    responseMessage = await HttpUtility.GetHttpResponseAsync(HttpMethod.Get, url + $"/{firstId}", "application/sarif+json", token);

                    if (responseMessage.IsSuccessStatusCode)
                    {
                        using (Stream stream = await responseMessage.Content.ReadAsStreamAsync())
                        {
                            sarifLog = SarifLog.Load(stream);
                        }
                    }
                }
            }

            return sarifLog;
        }

        /// <summary>
        /// Gets information about the active repo.
        /// </summary>
        /// <param name="path">The path of the open file, can be null</param>
        /// <returns>Repo uri for the given file</returns>
        public async ValueTask<string> GetRepoUriAsync(string path) // TODO: <string?>
        {
            // Get the trusted min git executable path.
            string minGitPath = Path.Combine(await this.vsInstallDir.GetValueAsync(), @"CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\mingw32\bin\git.exe");

            IGitRepositoryInfo repoInfo = await this.GetRepoAsync(path);
            if (repoInfo != null)
            {
                await TaskScheduler.Default;
                try
                {
                    string directoryName = Path.GetDirectoryName(path);
                    var processInfo = new ProcessStartInfo()
                    {
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        Arguments = "config --get remote.origin.url",
                        WorkingDirectory = directoryName,
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
            }

            return null;
        }

        private ValueTask<IGitRepositoryInfo> GetRepoAsync(string path)
        {
            foreach (IGitRepositoryInfo repo in this.gitService.ActiveRepositories)
            {
                if (PathUtil.IsDescendant(repo.RepositoryPath, path))
                {
                    return new ValueTask<IGitRepositoryInfo>(repo);
                }
            }

            return default;
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

        private ValueTask<IGitRepositoryInfo> GetRepoFromRelativePathAsync(string relativePath)
        {
            foreach (IGitRepositoryInfo repo in this.gitService.ActiveRepositories)
            {
                if (File.Exists(Path.Combine(repo.RepositoryPath, relativePath)))
                {
                    return new ValueTask<IGitRepositoryInfo>(repo);
                }
            }

            return default;
        }
    }
}

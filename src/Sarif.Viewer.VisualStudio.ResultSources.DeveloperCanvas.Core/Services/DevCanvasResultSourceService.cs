// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using CSharpFunctionalExtensions;

using EnvDTE;

using EnvDTE80;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer;
using Microsoft.Sarif.Viewer.ResultSources.Domain;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Abstractions;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Models;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Services;
using Microsoft.Sarif.Viewer.Shell;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Microsoft.Win32;

using Newtonsoft.Json;

using Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core.Models;

using static System.Net.Mime.MediaTypeNames;

using Result = CSharpFunctionalExtensions.Result;

namespace Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core.Services
{
    /// <summary>
    /// Is the class that handles the opening of projects and files and will start processes to properly query for the needed data from the DevCanvas web API.
    /// </summary>
    public class DevCanvasResultSourceService : IResultSourceService
    {
        public int FirstMenuId { get; set; }
        public int FirstCommandId { get; set; }
        public Func<string, object> GetOptionStateCallback { get; set; }
        public Action<string, object> SetOptionStateCallback { get; set; }

        public event EventHandler<ServiceEventArgs> ServiceEvent;
        public event EventHandler<SettingsEventArgs> SettingsEvent;

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
        private readonly MemoryCache filesQueriedCache;

        // Depending on the size of the solution/folder opened, we can attempt to query for insights for a lot of files at once.
        // To avoid hammering the thread pool, we allow a max number of outstanding queries and queue the rest.
        // Whenever a query completes, we process the next query on the queue until all queries are complete.
        // We use a priority queue to ensure we query for visible/opened files before files that are just loaded in the background.
        private int outstandingQueries = 0;

        /// <summary>
        /// Number of parallel queries we allow at once.
        /// </summary>
        private const int maxParallelQueries = 5;

        /// <summary>
        /// Max total number of entries into the cache that we allow.
        /// </summary>
        private const int MaxInsightCacheSize = 500;

        private static readonly string MaxInsightErrorMessage = $"Insights were not queried for one or more files because the limit of {MaxInsightCacheSize} has been reached. You can adjust this limit in the Options menu.";

        /// <summary>
        /// The number of minutes to wait before evicting an entry from the cache.
        /// </summary>
        private const int minutesBeforeRefresh = 60;

        private readonly Queue<string> filePathQueue;

        /// <summary>
        /// Handles the actual accessing of data 
        /// </summary>
        private readonly DevCanvasWebAPIAccessor accessor;

        private readonly object queryLock = new object();

        public DevCanvasResultSourceService(
            string solutionRootPath,
            Func<string, object> getOptionStateCallback,
            Action<string, object> setOptionStateCallback,
            IServiceProvider serviceProvider,
            IHttpClientAdapter httpClientAdapter,
            ISecretStoreRepository secretStoreRepository,
            IFileWatcher fileWatcherGitPush,
            IFileWatcher fileWatcherBranchChange,
            IFileSystem fileSystem,
            IGitExe gitExe,
            IInfoBarService infoBarService,
            IStatusBarService statusBarService)
        {
            this.GetOptionStateCallback = getOptionStateCallback;
            this.SetOptionStateCallback = setOptionStateCallback;
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

            this.browserService = new BrowserService();
            this.filesQueriedCache = new MemoryCache("FilesQueried");

            filePathQueue = new Queue<string>();

            Func<int> serverOptionAccess = () =>
            {
                return (int)getOptionStateCallback("DevCanvasServer");
            };

            AuthState.Initialize();
            AuthManager authManager = new AuthManager(SetOptionStateCallback);
            accessor = new DevCanvasWebAPIAccessor(serverOptionAccess, authManager);

        }

        /// <inheritdoc/>
        public System.Threading.Tasks.Task InitializeAsync()
        {
            // TODO: Remove this when merging into main.
            DevCanvasTracer.WriteLine($"Initializing {nameof(DevCanvasResultSourceService)}. Version 10/3");
            string userName = (string)Registry.GetValue("HKEY_CURRENT_USER\\Software\\Microsoft\\VSCommon\\ConnectedUser\\IdeUserV4\\Cache", "EmailAddress", null);

            if (string.IsNullOrWhiteSpace(userName) || !userName.EndsWith("@microsoft.com"))
            {
                DevCanvasTracer.WriteLine($"Failed to initialize {nameof(DevCanvasResultSourceService)}");
                return System.Threading.Tasks.Task.FromResult(Result.Failure("Not a MS user."));
            }
            else
            {
                DevCanvasTracer.WriteLine($"Initialized {nameof(DevCanvasResultSourceService)}");
                return System.Threading.Tasks.Task.FromResult(Result.Success());
            }
        }

        /// <inheritdoc/>
        public Task<Result> IsActiveAsync()
        {
            return System.Threading.Tasks.Task.FromResult(Result.Success());
        }

        /// <inheritdoc/>
        public Task<Result<bool, ErrorType>> OnDocumentEventAsync(string[] filePaths)
        {
            if (!AuthState.Instance.RefusedLogin)
            {
                foreach (string filePath in filePaths)
                {
                    lock (queryLock)
                    {
                        if (!filesQueriedCache.Contains(filePath))
                        {
                            filesQueriedCache.Add(filePath, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(minutesBeforeRefresh));
                            DownloadInsights(filePath);
                        }
                    }
                }
            }

            return System.Threading.Tasks.Task.FromResult(Result.Success<bool, ErrorType>(true));
        }

        /// <summary>
        /// Prepares to download insights for the given cache item. Note that actual download occurs in a separate thread and the
        /// request to download insights may be queued. The <see cref="ServiceEvent"/> event will be fired when
        /// insights have finally been downloaded.
        /// </summary>
        /// <param name="filePath">Cached information related to a file such as insights, download status, and any exceptions that ocurred relating to this file.</param>
        private void DownloadInsights(string filePath)
        {
            // If there aren't too many outstanding queries, or if the given file should bypass the queue, start a work item to query it now.
            // Otherwise, queue the query for later.
            if (filePathQueue.Count > MaxInsightCacheSize)
            {
                DevCanvasTracer.WriteLine(MaxInsightErrorMessage);
            }
            else if (outstandingQueries < maxParallelQueries)
            {
                outstandingQueries++;
                ThreadPool.QueueUserWorkItem(QueryInsights, filePath);
            }
            else
            {
                DevCanvasTracer.WriteLine($"Adding {filePath} to the queue.");
                filePathQueue.Enqueue(filePath);
            }
        }

        /// <summary>
        /// Builds the <see cref="DevCanvasVersionControlDetails"/> for a particular file path.
        /// </summary>
        /// <param name="absoluteFilePath">Absolute file path of the file on disk.</param>
        /// <returns>The information representing the repository if available, null otherwise.</returns>
        private async Task<(DevCanvasVersionControlDetails vcDetails, string repoRootedFilePath)> BuildVcDetailsAsync(string absoluteFilePath)
        {
            // TODO 
            // See if the file is part of a git repo / source depot base
            SourceControlType sourceControlType = SourceControlType.Unknown;
            string gitRepoRoot = await gitExe.GetRepoRootAsync(absoluteFilePath);
            string server = "";
            string project = "";
            string repo = "";
            string branch = "";

            if (gitRepoRoot != null)
            {
                sourceControlType = SourceControlType.Git;
                string repoUri = await gitExe.GetRepoUriAsync(absoluteFilePath);
                ParseGitUrl(repoUri, out server, out project, out repo);
                if (string.IsNullOrWhiteSpace(server) ||
                    string.IsNullOrWhiteSpace(project) ||
                    string.IsNullOrWhiteSpace(repo))
                {
                    sourceControlType = SourceControlType.Unknown;
                }
                else
                {
                    branch = await gitExe.GetCurrentBranchAsync(absoluteFilePath);
                }
            }
            else
            {
                bool sdExists = IsSourceDepot(absoluteFilePath, out gitRepoRoot);
                if (sdExists)
                {
                    sourceControlType = SourceControlType.SourceDepot;
                }
            }

            string repoRootedFilePath = GetRepoRootedFilePath(gitRepoRoot, absoluteFilePath);

            if (sourceControlType == SourceControlType.Unknown)
            {
                return (null, repoRootedFilePath);
            }

            // if it is part of a code base, construct a DevCanvasVersionControlDetails with appropriate details 
            return (new DevCanvasVersionControlDetails(sourceControlType, server, project, repo, branch), repoRootedFilePath);
        }

        /// <summary>
        /// Queries the web server for insights for a given file.
        /// This should be called from a worker thread.
        /// When the the insights have finished downloading for the given file, this method will
        /// process the query queue, scheduling another work item if necessary.
        /// </summary>
        /// <param name="state">A <see cref="RolledUpCacheItem"/> object.</param>
#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void QueryInsights(object state)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                // This will contain the message we'll display in the output window once the query is complete (and after we release the lock on cacheItem).
                StringBuilder resultMessage = new StringBuilder();

                string absoluteFilePath = (string)state;
                int insightsCount = 0;

                // if it is part of a code base, construct a DevCanvasVersionControlDetails with appropriate details 
                (DevCanvasVersionControlDetails vcDetails, string repoRootedFilePath) = await BuildVcDetailsAsync(absoluteFilePath);
                if (vcDetails == null || repoRootedFilePath == null)
                {
                    return;
                }

                // Get the generators (either from cache or web api) that we should query for this cache item.
                List<DevCanvasGeneratorInfo> allGenerators = await accessor.GetGeneratorsAsync();

                // Now query the server for insights.
                DevCanvasTracer.WriteLine($"Querying for insights for {absoluteFilePath}");

                List<SarifLog> logs = new List<SarifLog>();
                foreach (DevCanvasGeneratorInfo generator in allGenerators)
                {
                    DevCanvasRequestV1 requestObject = new DevCanvasRequestV1(generator.Name, repoRootedFilePath, vcDetails);

                    SarifLog log = await accessor.GetSarifLogV1Async(requestObject);
                    // filter out the xaml presentation since we cant render it here
                    log = RemoveUnneededContentFromLog(log);
                    logs.Add(log);
                }

                SarifLog masterLog = CombineLogs(logs);
                foreach (Run run in masterLog.Runs)
                {
                    insightsCount += run.Results.Count;
                }

                // Add the number of cached insights to the result message that we'll display in the output window (after releasing the lock).
                resultMessage.Append($"Cached {Util.S("insight", insightsCount)} for {absoluteFilePath}.");

                // Now that this query is complete, see if the number of outstanding queries is less than the max,
                // and if so, try to run the next query in the queue (if any).
                int queuedCount = 0;
                outstandingQueries--;
                if (outstandingQueries < maxParallelQueries)
                {
                    string nextFilePath = null;
                    if (filePathQueue.Count != 0)
                    {
                        nextFilePath = filePathQueue.Dequeue();

                        outstandingQueries++;
                        ThreadPool.QueueUserWorkItem(QueryInsights, nextFilePath);
                    }

                    // Periodically log a message to indicate how many files are still queued for processing.
                    queuedCount = filePathQueue.Count;
                    if (queuedCount > 0 && (queuedCount % 100 == 0))
                    {
                        resultMessage.Append($"\n{queuedCount} files remaining.");
                    }
                }

                // Display the result message (e.g. "Cached X insights for file Y")
                DevCanvasTracer.WriteLine(resultMessage.ToString());

                string repoRootedHash = string.Empty;
                using (var sha = new System.Security.Cryptography.SHA256Managed())
                {
                    byte[] textData = System.Text.Encoding.UTF8.GetBytes(repoRootedFilePath);
                    byte[] hash = sha.ComputeHash(textData);
                    repoRootedHash = BitConverter.ToString(hash).Replace("-", string.Empty);
                }

                RaiseServiceEvent(new ResultsUpdatedEventArgs()
                {
                    SarifLog = masterLog,
                    LogFileName = repoRootedHash,
                    UseDotSarifDirectory = false,
                    ShowBanner = false,
                    ClearPrevious = false,
                    ClearPreviousForFile = true
                });
            }
            catch (Exception)
            {
            }
        }

        /// <inheritdoc/>
        public async Task<Result<bool, ErrorType>> RequestAnalysisScanResultsAsync(object data = null)
        {
            // called from result source host's RequestAnalysisResultsAsync
            // called on a repo-basis, not a file/folder basis and we do not want to scan an entire repo at once for performance reasons and so we will leave it as-is.
            return Result.Success<bool, ErrorType>(true);
        }

        /// <summary>
        /// Gets the repo rooted version of the absolute file path.
        /// </summary>
        /// <param name="gitRepoRoot">Root of the code base.</param>
        /// <param name="absoluteFilePath">The file path we want a relative file path of</param>
        /// <returns>Repo rooted file path starting with the name of the directory.</returns>
        /// <exception cref="Exception">Thrown when the <paramref name="gitRepoRoot"/> is not part of <paramref name="absoluteFilePath"/>'s path.</exception>
        private string GetRepoRootedFilePath(string gitRepoRoot, string absoluteFilePath)
        {
            gitRepoRoot = gitRepoRoot.Replace("/", "\\");
            string correctedAbsPath = absoluteFilePath.Replace("/", "\\");
            if (correctedAbsPath.StartsWith(gitRepoRoot, StringComparison.OrdinalIgnoreCase))
            {
                string rootedFilePath = correctedAbsPath.Substring(gitRepoRoot.Length);
                if (rootedFilePath.StartsWith("\\"))
                {
                    rootedFilePath = rootedFilePath.Substring(1);
                }
                return rootedFilePath;
            }
            else
            {
                throw new Exception($"Repo root: {gitRepoRoot} is not a part of {correctedAbsPath}.");
            }
        }

        private const string XamlMessageKey = "Xaml";

        /// <summary>
        /// Trims the log to make a lighter log that can be processed easier.
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        private SarifLog RemoveUnneededContentFromLog(SarifLog log)
        {
            if (log.Runs != null)
            {
                foreach (Run run in log.Runs)
                {
                    foreach (Microsoft.CodeAnalysis.Sarif.Result result in run.Results)
                    {
                        if (result.Message.PropertyNames.Contains(XamlMessageKey))
                        {
                            result.Message.SetProperty<string>(XamlMessageKey, "");
                        }
                        if (result.Rule != null && result.Rule.ToolComponent != null)
                        {
                            result.Rule.ToolComponent = null;
                        }
                    }
                }
            }
            return log;
        }

        /// <summary>
        /// Combines logs together to create a single log representing the output for a file.
        /// </summary>
        /// <param name="logs">List of logs to combine.</param>
        /// <returns>One log that holds all the runs of the individual <see cref="SarifLog"/>s</returns>
        private SarifLog CombineLogs(List<SarifLog> logs)
        {
            SarifLog masterLog = new SarifLog();
            masterLog.Runs = logs.SelectMany(l => l.Runs ?? new List<Run>()).ToList();
            return masterLog;
        }

        private void RaiseServiceEvent(ServiceEventArgs eventArgs = null)
        {
            ServiceEvent?.Invoke(this, eventArgs);
        }

        private const string sdIniFileName = "sd.ini";

        /// <summary>
        /// Returns the code base root along with information about if it is a source depot code base.
        /// </summary>
        /// <param name="filePath">File path of a file in the code base</param>
        /// <param name="rootDir">The root of the code base</param>
        /// <returns>True if this file is part of a source depot code base.</returns>
        public static bool IsSourceDepot(string filePath, out string rootDir)
        {
            rootDir = null;

            try
            {
                rootDir = Path.GetDirectoryName(filePath);
                while (!string.IsNullOrEmpty(rootDir))
                {
                    if (File.Exists(Path.Combine(rootDir, sdIniFileName)))
                    {
                        return true;
                    }
                    rootDir = Path.GetDirectoryName(rootDir);
                }
            }
            catch (Exception)
            {
                // Swallow any exceptions that happen here. This is most likely due to the filePath being invalid.
            }
            return false;
        }

        /// <summary>
        /// Parses the server, project, and repository out of a full git-based repository URL.
        /// E.g. if <paramref name="gitUrl"/> = "https://dev.azure.com/serverName/projectName/_git/repoName" then
        /// <paramref name="gitServer"/> = "https://dev.azure.com/serverName",
        /// <paramref name="projectName"/> = "projectName", and
        /// <paramref name="gitRepositoryName"/> = "repoName".
        /// Note that some URLs, like GitHub, may not have a <paramref name="projectName"/>.
        /// It is the caller's responsibility to check the validity of each extracted part.
        /// </summary>
        /// <param name="gitUrl">The git url to be parsed. Ex: https://dev.azure.com/serverName/projectName/_git/repoName</param>
        /// <param name="gitServer">The server of the repo Ex: dev.azure.com/serverName</param>
        /// <param name="projectName">The project name of the repo Ex: OS.Fun</param>
        /// <param name="gitRepositoryName">The name of the repo Ex: devcanvas</param>
        public static void ParseGitUrl(string gitUrl, out string gitServer, out string projectName, out string gitRepositoryName)
        {
            projectName = null;
            gitServer = null;
            gitRepositoryName = null;

            if (gitUrl.Contains("ssh")) //example url: serverName@vs-ssh.visualstudio.com:v3/serverName/projectname/repoName , git@ssh.dev.azure.com:v3/serverName/projectName/repoName
            {
                if (gitUrl.Contains("vs-ssh.visualstudio.com"))
                {
                    Regex r = new Regex(@"(^[0-9,A-z]*@vs-ssh\.visualstudio\.com:[0-9,A-z]*/)([0-9,A-z]*)/([0-9,A-z]*)/([0-9,A-z]*)$");
                    Match match = r.Match(gitUrl);
                    if (match.Success && match.Groups.Count == 5)
                    {
                        string serverName = match.Groups[2].Value;
                        gitServer = $"{serverName}.visualstudio.com";
                        projectName = match.Groups[3].Value;
                        gitRepositoryName = match.Groups[4].Value;
                    }
                }
                else if (gitUrl.Contains("ssh.dev.azure.com"))
                {
                    Regex r = new Regex(@"(^git@ssh\.dev\.azure\.com:[0-9,A-z]*)/([0-9,A-z,%]*)/([0-9,A-z,%]*)/([0-9,A-z,%]*)$");
                    Match match = r.Match(gitUrl);
                    if (match.Success && match.Groups.Count == 5)
                    {
                        string serverName = match.Groups[2].Value;
                        gitServer = $"dev.azure.com/{serverName}";
                        projectName = match.Groups[3].Value;
                        gitRepositoryName = match.Groups[4].Value;
                    }
                }
                else
                {
                    return;
                }

            }
            else if (gitUrl.Contains("git")) //example url: https://serverName.visualstudio.com/DefaultCollection/projectName/_git/repoName
            {
                // handle URLs of format:
                //      https://github.com/Microsoft/Windows-universal-samples.git 
                //      git@github.com:Microsoft/ChakraCore-Debugger.git
                //      \\\\analogfs\\private\\AnalogSX\\GT\\ObjectLock\\ForNing\\Src\\MRTK.git
                if (gitUrl.EndsWith(".git"))
                {
                    // The URL contains project names and repository names which can alphabets, number and special characters, so regex matching is tricky
                    string[] parts = gitUrl.Split(new string[] { "https://", "/", ":", ".git" }, StringSplitOptions.RemoveEmptyEntries);

                    // Nothing to parse, return.
                    if (parts.Length == 0)
                    {
                        return;
                    }

                    // handle the case when git repo is hosted on a file share
                    if (gitUrl.StartsWith(@"\\"))
                    {
                        parts[0] = Regex.Unescape(parts[0]);
                        int indx = parts[0].LastIndexOf(@"\");
                        gitServer = parts[0].Substring(0, indx);
                        gitRepositoryName = parts[0].Substring(indx + 1);
                    }
                    else
                    {
                        gitServer = parts[0];
                        if (parts.Length == 2)
                        {
                            gitRepositoryName = parts[1];
                        }
                        else if (parts.Length == 3)
                        {
                            projectName = parts[1];
                            gitRepositoryName = parts[2];
                        }
                        else
                        {
                            // ideally we should not reach this code path, but in case there is an unexpected Git repo URL,
                            // set the repository name to null
                            gitRepositoryName = null;
                        }

                        // If the server looks like git@github.com, extract the github.com part.
                        if (gitServer.Contains("@"))
                        {
                            parts = gitServer.Split('@');
                            gitServer = parts.Length == 2 ? parts[1] : gitServer;
                        }
                    }
                }
                // handle the following URL formats to project to <"server", "project", "repo">
                //      https://dev.azure.com/serverName/_git/repoName to <"dev.azure.com/serverName", "projectName", "repoName">
                //      https://dev.azure.com/serverName/projectName/_git/repoName to <"dev.azure.com/serverName", "projectName", "repoName">
                //      https://llvm.org/git/llvm to <"llvm.org", "", "llvm">
                //      https://serverName.visualstudio.com/defaultcollection/projectName/_git/repoName to <"serverName.visualstudio.com", "projectName", "repoName">
                //      https://serverName.visualstudio.com/projectName/_git/repoName to <"serverName.visualstudio.com", "projectName", "repoName">
                else if (gitUrl.Contains(".com") || gitUrl.Contains(".org"))
                {
                    // only look at server/project/repo portion of URL
                    gitUrl = gitUrl.Contains("?") ? gitUrl.Substring(0, gitUrl.IndexOf("?")) : gitUrl;

                    // Remove "defaultcollection/" 
                    var regex = new Regex("defaultcollection/", RegexOptions.IgnoreCase);
                    gitUrl = regex.Replace(gitUrl, string.Empty);

                    // dev.azure.com uses "/" in server paths
                    // Use a replacer to change and change back for server, but could be used for other parts as needed
                    List<KeyValuePair<string, string>> delimiterReplacementsForServer = new List<KeyValuePair<string, string>>()
                    {
                        { new KeyValuePair<string, string>("dev.azure.com/", "dev.azure.com_") }
                    };
                    foreach (KeyValuePair<string, string> replacer in delimiterReplacementsForServer)
                    {
                        gitUrl = gitUrl.Replace(replacer.Key, replacer.Value);
                    }

                    // Some URLs have "_git" instead of "git", modify the set of delimiters accordingly.
                    List<string> delimiters = new List<string> { "https://", "/" };
                    if (gitUrl.Contains("_git"))
                    {
                        delimiters.Add("_git");
                    }
                    else
                    {
                        delimiters.Add("git");
                    }

                    string[] parts = gitUrl.Split(delimiters.ToArray(), StringSplitOptions.RemoveEmptyEntries);

                    // Nothing to parse, return.
                    if (parts.Length == 0)
                    {
                        return;
                    }

                    gitServer = parts[0];
                    foreach (KeyValuePair<string, string> replacer in delimiterReplacementsForServer)
                    {
                        gitServer = gitServer.Replace(replacer.Value, replacer.Key);
                    }
                    if (parts.Length == 3)
                    {
                        projectName = parts[1];
                        gitRepositoryName = parts[2];
                    }
                    else if (parts.Length == 2)
                    {
                        gitRepositoryName = parts[1];
                        // For Microsoft hosted repos - if there is no project in the url, it matches the repo name
                        if (gitServer.Contains("dev.azure.com") || gitServer.Contains(".visualstudio.com"))
                        {
                            projectName = parts[1];
                        }
                    }
                }
                else if (gitUrl.Contains("_git"))
                {
                    string[] parts = gitUrl.Split(new string[] { "https://", "/_git/" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        gitServer = parts[0];
                        gitRepositoryName = parts[1];
                    }
                }
            }
        }

        public void Settings_ServiceEvent(object sender, SettingsEventArgs e)
        {
            if (e.EventName == "DevCanvasLoginButtonClicked")
            {
                bool turnOn = bool.Parse(e.Value.ToString());
                if (turnOn)
                {
                     accessor.authManager.LogIntoDevCanvasAsync(0, string.Empty);
                }
                else
                {
                    filePathQueue.Clear();
                    accessor.authManager.LogOutOfDevCanvas();
                }
            }
        }
    }
}

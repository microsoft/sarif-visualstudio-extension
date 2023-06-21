// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;
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

using Newtonsoft.Json;

using Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core.Models;

using Result = CSharpFunctionalExtensions.Result;

namespace Sarif.Viewer.VisualStudio.ResultSources.DeveloperCanvas.Core.Services
{
    public class DevCanvasResultSourceService : IResultSourceService
    {
        public int FirstMenuId { get; set; }
        public int FirstCommandId { get; set; }
        public Func<string, bool> GetOptionStateCallback { get; set; }

#pragma warning disable CS0067 // The event 'DevCanvasResultSourceService.ServiceEvent' is never used
        public event EventHandler<ServiceEventArgs> ServiceEvent;
#pragma warning restore CS0067 // The event 'DevCanvasResultSourceService.ServiceEvent' is never used

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
        private readonly ReaderWriterLockSlim filePathQueueLock;

        /// <summary>
        /// Handles the actual accessing of data 
        /// </summary>
        private readonly DevCanvasAccessor accessor;

        public DevCanvasResultSourceService(
            string solutionRootPath,
            Func<string, bool> getOptionStateCallback,
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

            accessor = new DevCanvasAccessor();
        }

        /// <inheritdoc/>
        public async System.Threading.Tasks.Task InitializeAsync()
        {
            string repoPath = await gitExe.GetRepoRootAsync();
            string repoUrl = await gitExe.GetRepoUriAsync();
        }

        /// <inheritdoc/>
        public async Task<Result> IsActiveAsync()
        {
            return Result.Success();
        }

        /// <inheritdoc/>
        public async Task<Result<bool, ErrorType>> OnDocumentEventAsync(string[] filePaths)
        {
            foreach (string filePath in filePaths)
            {
                if (!filesQueriedCache.Contains(filePath))
                {
                    filesQueriedCache.Add(filePath, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(60));
                    DownloadInsights(filePath);
                    await this.statusBarService.SetStatusTextAsync($"Retrieving results from DevCanvas for {filePath}...");

                }
            }
            return Result.Success<bool, ErrorType>(true);
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
                Trace.WriteLine(MaxInsightErrorMessage);
            }
            else if (outstandingQueries < maxParallelQueries )
            {
                outstandingQueries++;
                ThreadPool.QueueUserWorkItem(QueryInsights, filePath);
            }
            else
            {
                filePathQueue.Enqueue(filePath);
            }
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
                string absoluteFilePath = (string)state;
                int insightsCount = 0;

                // This will contain the message we'll display in the output window once the query is complete (and after we release the lock on cacheItem).
                StringBuilder resultMessage = new StringBuilder();

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
                    Util.ParseGitUrl(repoUri, out server, out project, out repo);
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
                    bool sdExists = Util.IsSourceDepot(absoluteFilePath, out gitRepoRoot);
                    if (sdExists)
                    {
                        sourceControlType = SourceControlType.SourceDepot;
                    }
                }

                if (sourceControlType == SourceControlType.Unknown)
                {
                    return;
                }

                string repoRootedFilePath = GetRepoRootedFilePath(gitRepoRoot, absoluteFilePath);

                // if it is part of a code base, construct a DevCanvasVersionControlDetails with appropriate details 
                DevCanvasVersionControlDetails vcDetails = new DevCanvasVersionControlDetails(sourceControlType, server, project, repo, branch);

                // Get the generators that we should query for this cache item.
                List<DevCanvasGeneratorInfo> allGenerators = await accessor.GetGeneratorsAsync();

                // Now query the server for insights.
                Trace.WriteLine($"Querying for insights for {absoluteFilePath}");


                List<SarifLog> logs = new List<SarifLog>();
                foreach (DevCanvasGeneratorInfo generator in allGenerators)
                {
                    DevCanvasRequestV1 requestObject = new DevCanvasRequestV1(generator.Name, repoRootedFilePath, vcDetails);

                    SarifLog log = await accessor.GetSarifLogV1Async(requestObject);
                    // filter out the xaml presentation since we cant render it here
                    log = TrimLog(log);
                    logs.Add(log);
                }

                SarifLog masterLog = CombineLogs(logs);
                foreach (Run run in masterLog.Runs)
                {
                    insightsCount += run.Results.Count;
                }

                // Add the number of cached insights to the result message that we'll display in the output window (after releasing the lock).
                resultMessage.Append($"Cached {Util.S("insight", insightsCount)} for {absoluteFilePath}");

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
                Trace.WriteLine(resultMessage.ToString());

                RaiseServiceEvent(new ResultsUpdatedEventArgs()
                {
                    SarifLog = masterLog,
                    LogFileName = "",
                    UseDotSarifDirectory = false
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
            if (correctedAbsPath.StartsWith(gitRepoRoot))
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
        private SarifLog TrimLog(SarifLog log)
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
                        //result.Rule = null;
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
    }
}

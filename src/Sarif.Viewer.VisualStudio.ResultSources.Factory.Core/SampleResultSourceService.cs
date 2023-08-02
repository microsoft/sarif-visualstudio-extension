// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using CSharpFunctionalExtensions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ResultSources.Domain;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Abstractions;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Models;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Services;
using Microsoft.Sarif.Viewer.Shell;

using Result = CSharpFunctionalExtensions.Result;

namespace Microsoft.Sarif.Viewer.ResultSources.Factory
{
    /// <summary>
    /// A sample result source service for testing purposes.
    /// </summary>
    internal class SampleResultSourceService : IResultSourceService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SampleResultSourceService"/> class.
        /// </summary>
        /// <param name="solutionRootPath">The full path of the solution directory.</param>
        /// <param name="getOptionStateCallback">Callback <see cref="Func{T, TResult}"/> to retrieve option state.</param>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="httpClientAdapter">The <see cref="IHttpClientAdapter"/>.</param>
        /// <param name="secretStoreRepository">The <see cref="ISecretStoreRepository"/>.</param>
        /// <param name="fileWatcherBranchChange">The file watcher for Git branch changes.</param>
        /// <param name="fileWatcherGitPush">The file watcher for Git pushes.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="gitExe">The git.exe helper.</param>
        /// <param name="infoBarService">The <see cref="IInfoBarService"/>.</param>
        /// <param name="statusBarService">The <see cref="IStatusBarService"/>.</param>
        public SampleResultSourceService(
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable SA1114 // Parameter list should follow declaration
            string solutionRootPath,
#pragma warning restore SA1114 // Parameter list should follow declaration
            Func<string, bool> getOptionStateCallback,
            IServiceProvider serviceProvider,
            IHttpClientAdapter httpClientAdapter,
            ISecretStoreRepository secretStoreRepository,
            IFileWatcher fileWatcherBranchChange,
            IFileWatcher fileWatcherGitPush,
            IFileSystem fileSystem,
            IGitExe gitExe,
            IInfoBarService infoBarService,
            IStatusBarService statusBarService)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            Console.Write(ServiceEvent);
        }

        public SampleResultSourceService() { }

        public event EventHandler<ServiceEventArgs> ServiceEvent;

        public int FirstMenuId { get; set; }

        public int FirstCommandId { get; set; }

        public Func<string, object> GetOptionStateCallback { get; set; }

        public Task InitializeAsync()
        {
            return Task.Run(() => { });
        }

        public Task<Result> IsActiveAsync()
        {
            return Task.Run(() => { return Result.Success(); });
        }

        public Task<Result<bool, ErrorType>> RequestAnalysisScanResultsAsync(object data = null)
        {
            return Task.Run(() => { return Result.Success<bool, ErrorType>(true); });
        }

        public Task<Result<bool, ErrorType>> OnDocumentEventAsync(string[] filePaths)
        {
            return Task.Run(() => { return Result.Success<bool, ErrorType>(true); });
        }
    }
}

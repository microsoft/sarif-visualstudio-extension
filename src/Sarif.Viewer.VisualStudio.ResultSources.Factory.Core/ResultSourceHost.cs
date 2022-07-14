// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using CSharpFunctionalExtensions;

using Microsoft.Sarif.Viewer.ResultSources.Domain.Models;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Services;
using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.Sarif.Viewer.ResultSources.Factory
{
    /// <summary>
    /// Hosts the active result source for the specified solution.
    /// </summary>
    public class ResultSourceHost
    {
        private readonly string solutionPath;
        private readonly IServiceProvider serviceProvider;

        private IResultSourceService resultSourceService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultSourceHost"/> class.
        /// </summary>
        /// <param name="solutionPath">The absolute path of the folder that contains the solution file.</param>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
        public ResultSourceHost(string solutionPath, IServiceProvider serviceProvider)
        {
            this.solutionPath = solutionPath;
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// The event raised when new scan results are available.
        /// </summary>
        public event EventHandler<ResultsUpdatedEventArgs> ResultsUpdated;

        /// <summary>
        /// Requests analysis results from the active source service, if any.
        /// </summary>
        /// <returns>An asynchronous <see cref="Task"/>.</returns>
        public async Task RequestAnalysisResultsAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Currently this service only supports one result source.
            if (this.resultSourceService == null)
            {
                var resultSourceFactory = new ResultSourceFactory(solutionPath, serviceProvider);
                Result<IResultSourceService, ErrorType> result = await resultSourceFactory.GetResultSourceServiceAsync();

                if (result.IsSuccess)
                {
                    this.resultSourceService = result.Value;
                    this.resultSourceService.ResultsUpdated += this.ResultSourceService_ResultsUpdated;
                }
            }

            if (this.resultSourceService != null)
            {
                await this.resultSourceService?.RequestAnalysisScanResultsAsync();
            }
        }

        private void ResultSourceService_ResultsUpdated(object sender, ResultsUpdatedEventArgs e)
        {
            ResultsUpdated?.Invoke(this, e);
        }
    }
}

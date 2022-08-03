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
        private IResultSourceService resultSourceService;

        /// <summary>
        /// The event raised when new scan results are available.
        /// </summary>
        public event EventHandler<ResultsUpdatedEventArgs> ResultsUpdated;

        /// <summary>
        /// Requests analysis results from the active source service, if any.
        /// </summary>
        /// <param name="resultSourceFactory">The <see cref="IResultSourceFactory"/>.</param>
        /// <returns>An asynchronous <see cref="Task"/>.</returns>
        public async Task RequestAnalysisResultsAsync(IResultSourceFactory resultSourceFactory)
        {
            if (!ResultSourceFactory.IsUnitTesting)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            }

            // Currently this service only supports one result source.
            if (this.resultSourceService == null)
            {
                Result<IResultSourceService, ErrorType> result = await resultSourceFactory.GetResultSourceServiceAsync();

                if (result.IsSuccess)
                {
                    this.resultSourceService = result.Value;
                    this.resultSourceService.ResultsUpdated += this.ResultSourceService_ResultsUpdated;
                }
            }

            if (this.resultSourceService != null)
            {
                try
                {
                    await this.resultSourceService?.RequestAnalysisResultsAsync();
                }
                catch (Exception) { }
            }
        }

        private void ResultSourceService_ResultsUpdated(object sender, ResultsUpdatedEventArgs e)
        {
            ResultsUpdated?.Invoke(this, e);
        }
    }
}

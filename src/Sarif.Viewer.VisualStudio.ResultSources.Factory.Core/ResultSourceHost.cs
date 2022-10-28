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
        private readonly IResultSourceFactory resultSourceFactory;
        private IResultSourceService resultSourceService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultSourceHost"/> class.
        /// </summary>
        /// <param name="solutionRootPath">The local root path of the current project/solution.</param>
        /// <param name="serviceProvider">The service provider.</param>
        public ResultSourceHost(
            string solutionRootPath,
            IServiceProvider serviceProvider)
        {
            this.resultSourceFactory = new ResultSourceFactory(solutionRootPath, serviceProvider);
        }

        public ResultSourceHost(ResultSourceFactory resultSourceFactory)
        {
            this.resultSourceFactory = resultSourceFactory;
        }

        /// <summary>
        /// The event raised when new scan results are available.
        /// </summary>
        public event EventHandler<ServiceEventArgs> ServiceEvent;

        /// <summary>
        /// Gets the maximum number of child menus per flyout in the Error List context menu.
        /// </summary>
        public static int ErrorListContextdMenuChildFlyoutsPerFlyout => 3;

        /// <summary>
        /// Gets the maximum number of menu commands per flyout in the Error List context menu.
        /// </summary>
        public static int ErrorListContextdMenuCommandsPerFlyout => 10;

        /// <summary>
        /// Requests analysis results from the active source service, if any.
        /// </summary>
        /// <param name="resultSourceFactory">The <see cref="IResultSourceFactory"/>.</param>
        /// <returns>An asynchronous <see cref="Task"/>.</returns>
        public async Task RequestAnalysisResultsAsync()
        {
            if (!ResultSourceFactory.IsUnitTesting)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            }

            // Currently this service only supports one result source at a time.
            if (this.resultSourceService == null)
            {
                Result<IResultSourceService, ErrorType> result = await resultSourceFactory.GetResultSourceServiceAsync();

                if (result.IsSuccess)
                {
                    // We have an active results service.
                    this.resultSourceService = result.Value;

                    // Hook up the service event handler.
                    this.resultSourceService.ServiceEvent += this.ResultSourceService_ServiceEvent;

                    await this.resultSourceService.InitializeAsync();
                }
            }

            if (this.resultSourceService != null)
            {
                try
                {
                    await this.resultSourceService?.RequestAnalysisScanResultsAsync();
                }
                catch (Exception) { }
            }
        }

        private void ResultSourceService_ServiceEvent(object sender, ServiceEventArgs e)
        {
            ServiceEvent?.Invoke(this, e);
        }
    }
}

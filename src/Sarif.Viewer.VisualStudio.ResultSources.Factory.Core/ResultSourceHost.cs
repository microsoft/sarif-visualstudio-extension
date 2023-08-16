// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

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
        private List<IResultSourceService> resultSourceServices;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultSourceHost"/> class.
        /// </summary>
        /// <param name="solutionRootPath">The local root path of the current project/solution.</param>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="getOptionStateCallback">Callback <see cref="Func{T, TResult}"/> to retrieve option state.</param>
        /// <param name="setOptionStateCallback">Callback to set option state.</param>
        public ResultSourceHost(
            string solutionRootPath,
            IServiceProvider serviceProvider,
            Func<string, object> getOptionStateCallback,
            Action<string, object> setOptionStateCallback)
        {
            this.resultSourceFactory = new ResultSourceFactory(solutionRootPath, serviceProvider, getOptionStateCallback, setOptionStateCallback);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultSourceHost"/> class.
        /// </summary>
        /// <param name="resultSourceFactory">The <see cref="IResultSourceFactory"/>.</param>
        public ResultSourceHost(IResultSourceFactory resultSourceFactory)
        {
            this.resultSourceFactory = resultSourceFactory;
        }

        /// <summary>
        /// The event raised when new scan results are available.
        /// </summary>
        public event EventHandler<ServiceEventArgs> ServiceEvent;

        /// <summary>
        /// Fired when an event is fired by the settings ui.
        /// Some examples of this are button clicks or other listeners.
        /// </summary>
        public event EventHandler<SettingsEventArgs> SettingsEvent;

        /// <summary>
        /// Gets the maximum number of child menus per flyout in the Error List context menu.
        /// </summary>
        public static int ErrorListContextdMenuChildFlyoutsPerFlyout => 3;

        /// <summary>
        /// Gets the maximum number of menu commands per flyout in the Error List context menu.
        /// </summary>
        public static int ErrorListContextdMenuCommandsPerFlyout => 10;

        /// <summary>
        /// Gets the number of services in <see cref="resultSourceServices"/>.
        /// </summary>
        public int ServiceCount => resultSourceServices.Count;

        /// <summary>
        /// Requests analysis results from the active source service, if any.
        /// </summary>
        /// <param name="resultSourceFactory">The <see cref="IResultSourceFactory"/>.</param>
        /// <returns>An asynchronous <see cref="Task"/>.</returns>
        public async Task RequestAnalysisResultsAsync()
        {
            Trace.WriteLine("Start of RequestAnalysisResultsAsync");
            if (!ResultSourceFactory.IsUnitTesting)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            }

            if (this.resultSourceServices == null)
            {
                Result<List<IResultSourceService>, ErrorType> result = await resultSourceFactory.GetResultSourceServicesAsync();

                if (result.IsSuccess)
                {
                    // We have an active results service.
                    this.resultSourceServices = result.Value;

                    // Hook up the service event handler.
                    foreach (IResultSourceService service in this.resultSourceServices)
                    {
                        service.ServiceEvent += this.ResultSourceService_ServiceEvent;
                        await service.InitializeAsync();
                    }
                }
            }

            if (this.resultSourceServices != null)
            {
                try
                {
                    foreach (IResultSourceService service in this.resultSourceServices)
                    {
                        await service.RequestAnalysisScanResultsAsync();
                    }
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// Fires the <see cref="IResultSourceService.OnDocumentEventAsync(string[])"/> event when a file is opened.
        /// </summary>
        /// <param name="filePaths">Aboslute path of files that were opened.</param>
        /// <returns>An async task that tracks progress.</returns>
        public async Task RequestAnalysisResultsForFileAsync(string[] filePaths)
        {
            if (this.resultSourceServices != null)
            {
                foreach (IResultSourceService service in this.resultSourceServices)
                {
                    try
                    {
                        await service.OnDocumentEventAsync(filePaths);
                    }
                    catch (Exception) { }
                }
            }
        }

        /// <summary>
        /// Fires the service event of this class.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">Payload fired.</param>
        private void ResultSourceService_ServiceEvent(object sender, ServiceEventArgs e)
        {
            ServiceEvent?.Invoke(this, e);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using CSharpFunctionalExtensions;

using Microsoft.Sarif.Viewer.ResultSources.Domain.Models;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Services
{
    /// <summary>
    /// Represents a service that provides analysis results from an external source.
    /// </summary>
    public interface IResultSourceService
    {
        /// <summary>
        /// The event raised for all events that can be fired by the result service.
        /// </summary>
        event EventHandler<ServiceEventArgs> ServiceEvent;

        /// <summary>
        /// Gets or sets the first menu ID of the range that is available to the result source service.
        /// </summary>
        int FirstMenuId { get; set; }

        /// <summary>
        /// Gets or sets the first command ID of the range that is available to the result source service.
        /// </summary>
        int FirstCommandId { get; set; }

        /// <summary>
        /// Gets or sets the callback method to get the option state for the specified key.
        /// </summary>
        Func<string, object> GetOptionStateCallback { get; set; }

        /// <summary>
        /// Gets or sets the callback method to set the option state for the specified key.
        /// </summary>
        Action<string, object> SetOptionStateCallback { get; set; }

        /// <summary>
        /// Initializes the service instance.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task InitializeAsync();

        /// <summary>
        /// Gets a value indicating whether this service is active in the project.
        /// </summary>
        /// <returns><see cref="Result"/>.</returns>
        Task<Result> IsActiveAsync();

        /// <summary>
        /// Gets the latest code scan results for the current branch.
        /// </summary>
        /// <param name="data">A data object.</param>
        /// <returns>True if the request succeeded; otherwise, an error.</returns>
        Task<Result<bool, ErrorType>> RequestAnalysisScanResultsAsync(object data = null);

        /// <summary>
        /// Fired when a document or a set of documents are loaded.
        /// </summary>
        /// <param name="filePaths">List of files that were loaded.</param>
        /// <returns>True if succeeded, otherwise an error.</returns>
        Task<Result<bool, ErrorType>> OnDocumentEventAsync(string[] filePaths);

        /// <summary>
        /// Listens to when a setting event is fired.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">Payload fired.</param>
        public void Settings_ServiceEvent(object sender, SettingsEventArgs e);
    }
}

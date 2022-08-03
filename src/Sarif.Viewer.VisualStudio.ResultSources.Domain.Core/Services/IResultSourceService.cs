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
        /// The event raised when new scan results are available.
        /// </summary>
        event EventHandler<ResultsUpdatedEventArgs> ResultsUpdated;

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
        /// Gets the latest analysis results for the current branch.
        /// </summary>
        /// <param name="data">A data object.</param>
        /// <returns>True if the request succeeded; otherwise, an error.</returns>
        Task<Result<bool, ErrorType>> RequestAnalysisResultsAsync(object data = null);
    }
}

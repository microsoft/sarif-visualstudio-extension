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
        /// Gets a value indicating whether this service is active in the project.
        /// </summary>
        /// <returns>True if the service is active; otherwise, false.</returns>
        Task<bool> IsActiveAsync();

        /// <summary>
        /// Gets the latest code scan results for the current branch.
        /// </summary>
        /// <param name="data">A data object.</param>
        /// <returns>True if the request succeeded; otherwise, an error.</returns>
        Task<Result<bool, ErrorType>> RequestAnalysisScanResultsAsync(object data = null);
    }
}

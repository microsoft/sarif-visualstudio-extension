// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using CSharpFunctionalExtensions;

using Microsoft.Sarif.Viewer.ResultSources.Domain.Models;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Services
{
    public interface IResultSourceService
    {
        /// <summary>
        /// The event raised when new scan results are available.
        /// </summary>
        event EventHandler<ResultsUpdatedEventArgs> ResultsUpdated;

        /// <summary>
        /// Gets the latest code scan results for the current branch.
        /// </summary>
        /// <param name="data">A data object.</param>
        /// <returns>True if the request succeeded; otherwise, an error.</returns>
        Task<Result<bool, ErrorType>> RequestAnalysisScanResultsAsync(object data = null);
    }
}

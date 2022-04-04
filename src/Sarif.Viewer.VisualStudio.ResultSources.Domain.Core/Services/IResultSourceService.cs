// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using CSharpFunctionalExtensions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Models;

using Sarif.Viewer.VisualStudio.ResultSources.Domain.Core;

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
        /// <param name="httpClientAdapter">The <see cref="IHttpClientAdapter"/>.</param>
        /// <returns>The SARIF log received if successful; otherwise, an error.</returns>
        Task<Result<SarifLog, ErrorType>> GetCodeAnalysisScanResultsAsync(IHttpClientAdapter httpClientAdapter);
    }
}

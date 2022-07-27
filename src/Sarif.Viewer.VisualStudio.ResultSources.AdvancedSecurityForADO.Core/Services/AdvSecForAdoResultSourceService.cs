// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;

using CSharpFunctionalExtensions;

using Microsoft.Sarif.Viewer.ResultSources.Domain;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Models;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Services;

namespace Microsoft.Sarif.Viewer.ResultSources.AdvancedSecurityForAdo.Services
{
    public class AdvSecForAdoResultSourceService : IResultSourceService
    {
        private const string AzureDevOpsOrganizationUrl = "https://dev.azure.com/advsec";
        private const string ListBuildsApiUrlFormat = "https://dev.azure.com/advsec/Dogfood/_apis/build/builds?api-version=6.0";
        private const string GetBuildArtifactApiUrlFormat = "https://dev.azure.com/advsec/Dogfood/_apis/build/builds/177/artifacts?artifactName=CodeAnalysisLogs&api-version=6.0";

        private readonly string solutionRootPath;
        private readonly IServiceProvider serviceProvider;
        private readonly IHttpClientAdapter httpClientAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdvSecForAdoResultSourceService"/> class.
        /// </summary>
        /// <param name="solutionRootPath">The full path of the solution directory.</param>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="httpClientAdapter">The <see cref="IHttpClientAdapter"/>.</param>
        public AdvSecForAdoResultSourceService(
            string solutionRootPath,
            IServiceProvider serviceProvider,
            IHttpClientAdapter httpClientAdapter)
        {
            this.solutionRootPath = solutionRootPath;
            this.serviceProvider = serviceProvider;
            this.httpClientAdapter = httpClientAdapter;
        }

        public event EventHandler<ResultsUpdatedEventArgs> ResultsUpdated;

        public Task InitializeAsync()
        {
            throw new NotImplementedException();
        }

        public Task<Result> IsActiveAsync()
        {
            throw new NotImplementedException();
        }

        public Task<Result<bool, ErrorType>> RequestAnalysisScanResultsAsync(object data = null)
        {
            throw new NotImplementedException();
        }

        private async Task<string> GetLatestBuildIdAsync()
        {
            HttpRequestMessage requestMessage = httpClientAdapter.BuildRequest(
                HttpMethod.Get,
                ListBuildsApiUrlFormat,
                token: "token-here");

            return await Task.FromResult(string.Empty);
        }

        private async Task<string> GetArtifactDownloadUrlAsync()
        {
            return await Task.FromResult(string.Empty);
        }

        private void RaiseResultsUpdatedEvent(ResultsUpdatedEventArgs eventArgs = null)
        {
            ResultsUpdated?.Invoke(this, eventArgs);
        }
    }
}

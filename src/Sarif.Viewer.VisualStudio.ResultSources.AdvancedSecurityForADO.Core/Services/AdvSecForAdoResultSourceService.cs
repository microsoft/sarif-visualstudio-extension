// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using CSharpFunctionalExtensions;

using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Sarif.Viewer.ResultSources.AdvancedSecurityForAdo.Models;
using Microsoft.Sarif.Viewer.ResultSources.Domain;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Models;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Services;

using Newtonsoft.Json;

using Result = CSharpFunctionalExtensions.Result;

namespace Microsoft.Sarif.Viewer.ResultSources.AdvancedSecurityForAdo.Services
{
    public class AdvSecForAdoResultSourceService : IResultSourceService, IAdvSecForAdoResultSourceService
    {
        private const string SettingsFilePath = "AdvSecADO.json";
        private const string AzureDevOpsUrlFormat = "https://dev.azure.com/{0}/{1}";
        private const string ListBuildsApiQueryString = "/_apis/build/builds?api-version=6.0";
        private const string GetBuildArtifactApiQueryStringFormat = "/_apis/build/builds/{0}/artifacts?artifactName=CodeAnalysisLogs&api-version=6.0";

        private readonly string solutionRootPath;
        private readonly IServiceProvider serviceProvider;
        private readonly IHttpClientAdapter httpClientAdapter;
        private readonly IFileSystem fileSystem;

        private Settings settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdvSecForAdoResultSourceService"/> class.
        /// </summary>
        /// <param name="solutionRootPath">The full path of the solution directory.</param>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="httpClientAdapter">The <see cref="IHttpClientAdapter"/>.</param>
        /// <param name="fileSystem">The file system.</param>
        public AdvSecForAdoResultSourceService(
            string solutionRootPath,
            IServiceProvider serviceProvider,
            IHttpClientAdapter httpClientAdapter,
            IFileSystem fileSystem)
        {
            this.solutionRootPath = solutionRootPath;
            this.serviceProvider = serviceProvider;
            this.httpClientAdapter = httpClientAdapter;
            this.fileSystem = fileSystem;
        }

        /// <inheritdoc cref="IResultSourceService.ResultsUpdated"/>
        public event EventHandler<ResultsUpdatedEventArgs> ResultsUpdated;

        /// <inheritdoc cref="IResultSourceService.InitializeAsync()"/>
        public Task InitializeAsync()
        {
            if (!string.IsNullOrWhiteSpace(this.solutionRootPath))
            {
                string path = Path.Combine(this.solutionRootPath, SettingsFilePath);
                if (fileSystem.FileExists(path))
                {
                    string settingsText = File.ReadAllText(path);
                    this.settings = JsonConvert.DeserializeObject<Settings>(settingsText);
                }
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc cref="IResultSourceService.IsActiveAsync()"/>
        public Task<Result> IsActiveAsync()
        {
            Result result = !string.IsNullOrWhiteSpace(this.solutionRootPath) && fileSystem.FileExists(Path.Combine(this.solutionRootPath, SettingsFilePath)) ?
                Result.Success() :
                Result.Failure(nameof(AdvSecForAdoResultSourceService));
            return Task.FromResult(result);
        }

        /// <inheritdoc cref="IResultSourceService.RequestAnalysisScanResultsAsync(object)"/>
        public Task<Result<bool, ErrorType>> RequestAnalysisScanResultsAsync(object data = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IAdvSecForAdoResultSourceService.GetLatestBuildIdAsync()"/>
        public async Task<string> GetLatestBuildIdAsync()
        {
            HttpRequestMessage requestMessage = httpClientAdapter.BuildRequest(
                HttpMethod.Get,
                ListBuildsApiQueryString,
                token: "token-here");

            return await Task.FromResult(string.Empty);
        }

        /// <inheritdoc cref="IAdvSecForAdoResultSourceService.GetArtifactDownloadUrlAsync(string)"/>
        public async Task<string> GetArtifactDownloadUrlAsync(string buildId)
        {
            return await Task.FromResult(buildId);
        }

        private void RaiseResultsUpdatedEvent(ResultsUpdatedEventArgs eventArgs = null)
        {
            ResultsUpdated?.Invoke(this, eventArgs);
        }
    }
}

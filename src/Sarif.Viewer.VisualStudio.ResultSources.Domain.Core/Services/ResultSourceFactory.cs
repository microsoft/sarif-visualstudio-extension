// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using CSharpFunctionalExtensions;

using Microsoft.Sarif.Viewer.ResultSources.Domain.Abstractions;
using Microsoft.Sarif.Viewer.ResultSources.Domain.Services.GitHub;

using Result = CSharpFunctionalExtensions.Result;

namespace Microsoft.Sarif.Viewer.ResultSources.Domain.Services
{
    public class ResultSourceFactory
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ISecretStoreRepository secretStoreRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultSourceFactory"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="secretStoreRepository">The <see cref="ISecretStoreRepository"/>.</param>
        public ResultSourceFactory(
            IServiceProvider serviceProvider,
            ISecretStoreRepository secretStoreRepository)
        {
            this.serviceProvider = serviceProvider;
            this.secretStoreRepository = secretStoreRepository;
        }

        /// <summary>
        /// Gets a result source service.
        /// </summary>
        /// <param name="projectRootPath">The local root path for the current project.</param>
        /// <returns>A result source service instance if the project platform is supported; otherwise, null.</returns>
        public async Task<Result<IResultSourceService, ErrorType>> GetResultSourceServiceAsync(string projectRootPath)
        {
            // Check for GitHub project
            var gitHubSourceService = new GitHubSourceService(this.serviceProvider, projectRootPath);

            if (gitHubSourceService.IsGitHubProject())
            {
                await gitHubSourceService.InitializeAsync(this.secretStoreRepository);
                return gitHubSourceService;
            }

            return Result.Failure<IResultSourceService, ErrorType>(ErrorType.PlatformNotSupported);
        }
    }
}
